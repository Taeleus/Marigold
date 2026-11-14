using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using HarmonyLib;

namespace Marigold
{
    internal sealed class GpuShipResult
    {
        public readonly GpuShipFrame Frame;
        public readonly string TextureName, Error;
        public readonly double RenderMilliseconds;
        public readonly int Generation;
        public readonly bool TexturesPending;

        public GpuShipResult(GpuShipFrame frame, string textureName, string error, double renderMilliseconds,
            int generation, bool texturesPending)
        {
            Frame = frame;
            TextureName = textureName;
            Error = error;
            RenderMilliseconds = renderMilliseconds;
            Generation = generation;
            TexturesPending = texturesPending;
        }
    }

    internal sealed partial class GpuShipRenderer : IDisposable
    {
        private const string PatchId = "marigold.gpu-preview";
        private static readonly string TexturePrefix = "marigoldgpu" + Guid.NewGuid().ToString("N");
        private static GpuShipRenderer active;
        private readonly GameRenderer game;
        private readonly Harmony harmony;
        private Request pending;
        private GpuShipResult latest;
        private Pipeline pipeline;
        private int generation, renderGeneration, disposed;

        private sealed class Request
        {
            public readonly GpuShipFrame Frame;
            public readonly int Generation;

            public Request(GpuShipFrame frame, int generation)
            {
                Frame = frame;
                Generation = generation;
            }
        }

        public GpuShipRenderer()
        {
            game = new GameRenderer();
            harmony = new Harmony(PatchId);
            if (Interlocked.CompareExchange(ref active, this, null) != null)
                throw new InvalidOperationException("A Marigold GPU preview is already active.");

            try
            {
                harmony.Patch(game.SpritePass, prefix: new HarmonyMethod(typeof(GpuShipRenderer), nameof(BeforeSprites)));
            }
            catch
            {
                Interlocked.CompareExchange(ref active, null, this);
                throw;
            }
        }

        public GpuShipResult Latest
        {
            get
            {
                var result = Volatile.Read(ref latest);
                return result != null && result.Generation == Volatile.Read(ref generation) ? result : null;
            }
        }

        public void Submit(GpuShipFrame frame)
        {
            if (Volatile.Read(ref disposed) == 0)
                Interlocked.Exchange(ref pending, new Request(frame, Volatile.Read(ref generation)));
        }

        public void Clear()
        {
            Interlocked.Increment(ref generation);
            Interlocked.Exchange(ref pending, null);
            Volatile.Write(ref latest, null);
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref disposed, 1);
            Clear();
        }

        private static void BeforeSprites()
        {
            Volatile.Read(ref active)?.DrawOnRenderThread();
        }

        private void DrawOnRenderThread()
        {
            if (Volatile.Read(ref disposed) != 0)
            {
                ReleasePipeline();
                try { harmony.Unpatch(game.SpritePass, HarmonyPatchType.Prefix, PatchId); }
                catch { /* Leave the disabled hook inert if the game is shutting down. */ }
                Interlocked.CompareExchange(ref active, null, this);
                return;
            }

            var request = Interlocked.Exchange(ref pending, null);
            int currentGeneration = Volatile.Read(ref generation);
            if (request != null && request.Generation != currentGeneration)
                request = null;
            bool sceneChanged = renderGeneration != currentGeneration;
            if (request == null && !sceneChanged)
                return;

            renderGeneration = currentGeneration;
            long start = Stopwatch.GetTimestamp();
            try
            {
                var device = game.Device;
                bool deviceAvailable = device != null && device.NativePointer != IntPtr.Zero;
                if (pipeline != null && (!deviceAvailable || !ReferenceEquals(pipeline.Device, device)))
                    ReleasePipeline();

                if (sceneChanged)
                    pipeline?.ClearScene();
                if (request == null)
                    return;

                if (!deviceAvailable)
                    throw new InvalidOperationException("The game Direct3D device is unavailable.");
                if (pipeline == null)
                    pipeline = new Pipeline(game, device, TexturePrefix);
                string texture = pipeline.Draw(request.Frame);
                var result = new GpuShipResult(request.Frame, texture, null,
                    (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency, request.Generation,
                    pipeline.TexturesPending);
                Volatile.Write(ref latest, result);
            }
            catch (Exception error)
            {
                ReleasePipeline();
                if (request == null)
                    return;
                if (error is TargetInvocationException reflectionError && reflectionError.InnerException != null)
                    error = reflectionError.InnerException;
                Volatile.Write(ref latest, new GpuShipResult(request.Frame, null,
                    "GPU Textured Ship unavailable: " + error.Message, 0, request.Generation, false));
            }
        }

        private void ReleasePipeline()
        {
            try { pipeline?.Dispose(); }
            catch { /* Graphics resources can already have been released during device shutdown. */ }
            pipeline = null;
        }
    }
}
