using System;
using System.Reflection;
using HarmonyLib;
using SharpDX;
using SharpDX.Direct3D11;
using VRageRender.Messages;
using Resource = SharpDX.Direct3D11.Resource;

namespace Marigold
{
    internal sealed partial class GpuShipRenderer
    {
        private sealed class GameRenderer
        {
            public readonly MethodInfo SpritePass;
            private readonly PropertyInfo device, resource, streamedTexture, shaderView, loadFailed, loaded;
            private readonly FieldInfo fileTextures, textureStreamer;
            private readonly MethodInfo createTexture, initializeTexture, getTexture, touch;
            private readonly object colorQuery;

            public GameRenderer()
            {
                var render = RequireType("VRageRender.MyRender11");
                var managers = RequireType("VRage.Render11.Common.MyManagers");
                var generated = RequireType("VRage.Render11.Resources.Internal.MyGeneratedTexture");
                var streamer = RequireType("VRage.Render11.Resources.Textures.MyTextureStreamingManager");
                var streamBase = RequireType("VRage.Render11.Resources.Textures.IMyStreamedTextureBase");
                var streamTexture = RequireType("VRage.Render11.Resources.Textures.IMyStreamedTexture");
                var texture = RequireType("VRage.Render11.Resources.Internal.MyFileTexture");
                var textureType = RequireType("VRage.Render11.Resources.MyFileTextureEnum");
                var query = AccessTools.Inner(streamer, "QueryArgs");
                if (query == null)
                    throw new MissingMemberException(streamer.FullName, "QueryArgs");

                SpritePass = RequireMethod(render, "RenderMainSprites");
                device = RequireProperty(render, "DeviceInstance");
                fileTextures = RequireField(managers, "FileTextures");
                textureStreamer = RequireField(managers, "Textures");
                createTexture = RequireMethod(fileTextures.FieldType, "CreateGeneratedTexture",
                    typeof(string), typeof(int), typeof(int), typeof(MyGeneratedTextureType), typeof(bool),
                    typeof(byte[]), typeof(bool));
                initializeTexture = RequireMethod(generated, "Reset", typeof(DataBox[]));
                resource = RequireProperty(generated, "Resource");
                getTexture = RequireMethod(streamer, "GetTexture", typeof(string), query);
                touch = RequireMethod(streamBase, "Touch", typeof(ushort));
                streamedTexture = RequireProperty(streamTexture, "Texture");
                shaderView = RequireProperty(RequireType("VRage.Render11.Resources.ISrvBindable"), "Srv");
                loadFailed = RequireProperty(texture, "LoadFailed");
                loaded = RequireProperty(texture, "IsLoaded");
                colorQuery = Activator.CreateInstance(query);
                RequireField(query, "TextureType").SetValue(colorQuery, Enum.Parse(textureType, "COLOR_METAL"));
            }

            public Device Device => device.GetValue(null) as Device;

            public Resource GetOrCreateTexture(string name, int size)
            {
                var texture = createTexture.Invoke(fileTextures.GetValue(null), new object[]
                {
                    name, size, size, MyGeneratedTextureType.RGBA, false, null, true
                });
                if (texture == null)
                    throw new InvalidOperationException("Space Engineers could not register the GPU preview texture.");

                var output = resource.GetValue(texture) as Resource;
                if (output != null && output.NativePointer != IntPtr.Zero)
                    return output;

                // The render manager owns the stable sprite names through device teardown.
                // A new generated texture without initial pixels has no D3D resource yet.
                initializeTexture.Invoke(texture, new object[] { null });
                output = resource.GetValue(texture) as Resource;
                if (output == null || output.NativePointer == IntPtr.Zero)
                    throw new InvalidOperationException("Space Engineers could not allocate the GPU preview texture.");
                return output;
            }

            public object Material(string path)
            {
                return getTexture.Invoke(textureStreamer.GetValue(null), new[] { (object)path, colorQuery });
            }

            public ShaderResourceView View(object material, out bool loading)
            {
                loading = false;
                if (material == null)
                    return null;
                touch.Invoke(material, new object[] { (ushort)100 });
                var texture = streamedTexture.GetValue(material);
                if (texture == null)
                {
                    loading = true;
                    return null;
                }
                if (loadFailed.DeclaringType.IsInstanceOfType(texture))
                {
                    if ((bool)loadFailed.GetValue(texture))
                        return null;
                    loading = !(bool)loaded.GetValue(texture);
                }
                return shaderView.GetValue(texture) as ShaderResourceView;
            }

            private static Type RequireType(string name)
            {
                return AccessTools.TypeByName(name) ?? throw new TypeLoadException(name);
            }

            private static MethodInfo RequireMethod(Type type, string name, params Type[] arguments)
            {
                return AccessTools.Method(type, name, arguments) ?? throw new MissingMethodException(type.FullName, name);
            }

            private static PropertyInfo RequireProperty(Type type, string name)
            {
                return AccessTools.Property(type, name) ?? throw new MissingMemberException(type.FullName, name);
            }

            private static FieldInfo RequireField(Type type, string name)
            {
                return AccessTools.Field(type, name) ?? throw new MissingFieldException(type.FullName, name);
            }
        }
    }
}
