using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using VRageMath;
using Buffer = SharpDX.Direct3D11.Buffer;
using D3DComparison = SharpDX.Direct3D11.Comparison;
using Device = SharpDX.Direct3D11.Device;

namespace Marigold
{
    internal sealed partial class GpuShipRenderer
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct CameraConstants
        {
            private const float DepthScale = .125f;
            public Vector4 Right, Up, Near, CenterInverseRadius, Settings;

            public CameraConstants(GpuShipFrame frame)
            {
                var min = frame.Bounds.Min;
                var max = frame.Bounds.Max;
                double dx = max.X - min.X, dy = max.Y - min.Y, dz = max.Z - min.Z;
                double radius = Math.Max(.5, Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2);
                Right = new Vector4((float)frame.View.X.X, (float)frame.View.Y.X, (float)frame.View.Z.X, 0);
                Up = new Vector4((float)frame.View.X.Y, (float)frame.View.Y.Y, (float)frame.View.Z.Y, 0);
                Near = new Vector4((float)frame.View.X.Z, (float)frame.View.Y.Z, (float)frame.View.Z.Z, 0);
                CenterInverseRadius = new Vector4((float)((min.X + max.X) / 2), (float)((min.Y + max.Y) / 2),
                    (float)((min.Z + max.Z) / 2), (float)(1 / radius));
                Settings = new Vector4((float)frame.View.Zoom, frame.ShipOpacity, DepthScale * frame.DepthTolerance, DepthScale);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct InstanceData
        {
            private static readonly float[] Srgb = LinearColors();
            public Vector4 X, Y, Z, Uv, Paint;

            public InstanceData(TexturedShipGpuInstance instance, byte alpha)
            {
                var matrix = instance.Transform;
                X = new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41);
                Y = new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42);
                Z = new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43);
                Uv = new Vector4(instance.UvOffset.X, instance.UvOffset.Y, instance.UvScale, 0);
                uint color = instance.PaintRgb;
                Paint = new Vector4(Srgb[(color >> 16) & 255], Srgb[(color >> 8) & 255], Srgb[color & 255], alpha / 255f);
            }

            private static float[] LinearColors()
            {
                var colors = new float[256];
                for (int i = 0; i < colors.Length; i++)
                {
                    double srgb = i / 255.0;
                    colors[i] = (float)(srgb <= .04045 ? srgb / 12.92 : Math.Pow((srgb + .055) / 1.055, 2.4));
                }
                return colors;
            }
        }

        private sealed class Pipeline : IDisposable
        {
            private const Format ColorFormat = Format.R8G8B8A8_UNorm_SRgb;
            private static readonly int InstanceStride = Utilities.SizeOf<InstanceData>();
            private static readonly int ModelStride = Utilities.SizeOf<TexturedShipVertex>();
            private static readonly int OverlayStride = Utilities.SizeOf<GpuShipOverlayVertex>();
            private const int MaximumMaterialTextures = 4096;
            private readonly GameRenderer game;
            private readonly string texturePrefix;
            private readonly DeviceContext commands;
            private readonly VertexShader modelVertex, overlayVertex;
            private readonly PixelShader paintPixel, texturePixel, fillPixel, linePixel;
            private readonly InputLayout modelLayout, overlayLayout;
            private readonly RasterizerState rasterizer;
            private readonly BlendState overlayBlend;
            private readonly DepthStencilState solidDepth, dimDepth, noDepth;
            private readonly SamplerState sampler;
            private readonly Buffer cameraBuffer;
            private readonly List<RenderBatch> batches = new List<RenderBatch>();
            private readonly Dictionary<string, MaterialTexture> materials =
                new Dictionary<string, MaterialTexture>(StringComparer.OrdinalIgnoreCase);
            private TexturedShipMesh mesh;
            private byte[] alphas;
            private Texture2D color, depth;
            private RenderTargetView renderTarget;
            private DepthStencilView depthTarget;
            private ShaderResourceView depthView;
            private Buffer overlayFills, overlayLines;
            private GpuShipOverlayVertex[] uploadedFills, uploadedLines;
            private int size, renderNumber;

            public Device Device { get; }
            public bool TexturesPending { get; private set; }

            private sealed class MaterialTexture
            {
                public object Handle;
                public ShaderResourceView View;
                public int RenderNumber;
                public bool Loading;
            }

            public Pipeline(GameRenderer game, Device device, string texturePrefix)
            {
                this.game = game;
                this.texturePrefix = texturePrefix;
                Device = device;
                try
                {
                    commands = new DeviceContext(device);
                    using (var bytecode = Compile("ModelVertex", "vs_4_0"))
                    {
                        modelVertex = new VertexShader(device, bytecode);
                        modelLayout = new InputLayout(device, bytecode, ModelElements());
                    }
                    using (var bytecode = Compile("OverlayVertex", "vs_4_0"))
                    {
                        overlayVertex = new VertexShader(device, bytecode);
                        overlayLayout = new InputLayout(device, bytecode, new[]
                        {
                            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                            new InputElement("COLOR", 0, Format.R8G8B8A8_UNorm, 12, 0),
                            new InputElement("TEXCOORD", 0, Format.R32_Float, 16, 0)
                        });
                    }
                    paintPixel = Pixel("PaintPixel");
                    texturePixel = Pixel("TexturedPixel");
                    fillPixel = Pixel("FillPixel");
                    linePixel = Pixel("LinePixel");
                    cameraBuffer = new Buffer(device, Utilities.SizeOf<CameraConstants>(), ResourceUsage.Default,
                        BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                    solidDepth = new DepthStencilState(device, HullDepthState(true));
                    dimDepth = new DepthStencilState(device, HullDepthState(false));
                    var noDepthState = DepthStencilStateDescription.Default();
                    noDepthState.IsDepthEnabled = false;
                    noDepthState.DepthWriteMask = DepthWriteMask.Zero;
                    noDepth = new DepthStencilState(device, noDepthState);
                    rasterizer = new RasterizerState(device, new RasterizerStateDescription
                    {
                        CullMode = CullMode.None, FillMode = FillMode.Solid, IsDepthClipEnabled = true,
                        IsAntialiasedLineEnabled = true
                    });
                    var blending = new BlendStateDescription();
                    blending.RenderTarget[0] = new RenderTargetBlendDescription
                    {
                        IsBlendEnabled = true, SourceBlend = BlendOption.One,
                        DestinationBlend = BlendOption.InverseSourceAlpha, BlendOperation = BlendOperation.Add,
                        SourceAlphaBlend = BlendOption.One, DestinationAlphaBlend = BlendOption.InverseSourceAlpha,
                        AlphaBlendOperation = BlendOperation.Add, RenderTargetWriteMask = ColorWriteMaskFlags.All
                    };
                    overlayBlend = new BlendState(device, blending);
                    sampler = new SamplerState(device, new SamplerStateDescription
                    {
                        Filter = Filter.Anisotropic, MaximumAnisotropy = 4, ComparisonFunction = D3DComparison.Never,
                        AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Wrap,
                        AddressW = TextureAddressMode.Wrap, MaximumLod = float.MaxValue
                    });
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public string Draw(GpuShipFrame frame)
            {
                if (frame.Size != 384 && frame.Size != 768 && frame.Size != 1024 && frame.Size != 1536)
                    throw new ArgumentOutOfRangeException(nameof(frame), "Unsupported GPU preview size.");
                renderNumber++;
                TexturesPending = false;
                EnsureTargets(frame.Size);
                string outputName = texturePrefix + "_" + frame.Size.ToString(CultureInfo.InvariantCulture);
                var outputResource = game.GetOrCreateTexture(outputName, frame.Size);
                if (frame.ShipOpacity > 0)
                    PrepareMesh(frame.Mesh, frame.PartAlphas);

                var camera = new CameraConstants(frame);
                commands.UpdateSubresource(ref camera, cameraBuffer);
                commands.VertexShader.SetConstantBuffer(0, cameraBuffer);
                commands.PixelShader.SetConstantBuffer(0, cameraBuffer);
                commands.Rasterizer.State = rasterizer;
                commands.Rasterizer.SetViewport(0, 0, size, size, 0, 1);
                commands.ClearRenderTargetView(renderTarget, default(RawColor4));
                commands.ClearDepthStencilView(depthTarget, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
                if (frame.ShipOpacity > 0)
                    DrawModel();
                DrawOverlays(frame);
                commands.OutputMerger.SetTargets((RenderTargetView)null);
                commands.CopyResource(color, outputResource);
                using (var list = commands.FinishCommandList(false))
                    Device.ImmediateContext.ExecuteCommandList(list, true);
                return outputName;
            }

            private void PrepareMesh(TexturedShipMesh next, byte[] nextAlphas)
            {
                if (!ReferenceEquals(mesh, next))
                {
                    ReleaseBatches();
                    foreach (var batch in next.GpuBatches)
                        if (batch.Vertices.Length != 0 && batch.Indices.Length != 0 && batch.Instances.Length != 0)
                            batches.Add(new RenderBatch(Device, batch));
                    mesh = next;
                }
                else if (ReferenceEquals(alphas, nextAlphas))
                    return;

                foreach (var batch in batches)
                    batch.SetInstances(Device, nextAlphas);
                alphas = nextAlphas;
            }

            private void DrawModel()
            {
                commands.OutputMerger.SetTargets(depthTarget, renderTarget);
                commands.InputAssembler.InputLayout = modelLayout;
                commands.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                commands.VertexShader.Set(modelVertex);
                commands.PixelShader.SetSampler(0, sampler);
                DrawModelPass(solidDepth, 1, false);
                DrawModelPass(dimDepth, 0, true);
                commands.PixelShader.SetShaderResource(0, null);
            }

            private void DrawModelPass(DepthStencilState state, int reference, bool dim)
            {
                commands.OutputMerger.SetDepthStencilState(state, reference);
                PixelShader shader = null;
                ShaderResourceView bound = null;
                foreach (var batch in batches)
                {
                    int count = dim ? batch.DimCount : batch.SolidCount;
                    if (count == 0)
                        continue;

                    var view = Material(batch.Source.ColorTexture);
                    var nextShader = view != null ? texturePixel : paintPixel;
                    if (shader != nextShader)
                    {
                        commands.PixelShader.Set(nextShader);
                        shader = nextShader;
                    }
                    if (!ReferenceEquals(bound, view))
                    {
                        commands.PixelShader.SetShaderResource(0, view);
                        bound = view;
                    }
                    commands.InputAssembler.SetIndexBuffer(batch.Indices, Format.R32_UInt, 0);
                    commands.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(batch.Vertices, ModelStride, 0));
                    commands.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(dim ? batch.Dim : batch.Solid, InstanceStride, 0));
                    commands.DrawIndexedInstanced(batch.Source.Indices.Length, count, 0, 0, 0);
                }
            }

            private ShaderResourceView Material(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return null;
                if (!materials.TryGetValue(path, out var material))
                {
                    if (materials.Count >= MaximumMaterialTextures)
                        return null;
                    material = new MaterialTexture { Handle = game.Material(path) };
                    materials.Add(path, material);
                }
                if (material.RenderNumber != renderNumber)
                {
                    material.View = game.View(material.Handle, out material.Loading);
                    material.RenderNumber = renderNumber;
                }
                TexturesPending |= material.Loading;
                return material.View != null && material.View.NativePointer != IntPtr.Zero ? material.View : null;
            }

            private void DrawOverlays(GpuShipFrame frame)
            {
                commands.OutputMerger.SetTargets(renderTarget);
                commands.OutputMerger.SetDepthStencilState(noDepth);
                commands.OutputMerger.SetBlendState(overlayBlend);
                commands.VertexShader.Set(overlayVertex);
                commands.PixelShader.SetShaderResource(1, depthView);
                commands.InputAssembler.InputLayout = overlayLayout;
                DrawOverlay(ref overlayFills, ref uploadedFills, frame.Fills, fillPixel, PrimitiveTopology.TriangleList);
                DrawOverlay(ref overlayLines, ref uploadedLines, frame.Lines, linePixel, PrimitiveTopology.LineList);
                commands.PixelShader.SetShaderResource(1, null);
            }

            private void DrawOverlay(ref Buffer buffer, ref GpuShipOverlayVertex[] uploaded,
                GpuShipOverlayVertex[] vertices, PixelShader shader, PrimitiveTopology topology)
            {
                if (vertices == null || vertices.Length == 0)
                    return;
                if (!ReferenceEquals(uploaded, vertices))
                {
                    int required = checked(vertices.Length * OverlayStride);
                    if (buffer == null || buffer.Description.SizeInBytes < required)
                    {
                        buffer?.Dispose();
                        buffer = new Buffer(Device, (required + 4095) & ~4095, ResourceUsage.Dynamic,
                            BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
                    }
                    commands.MapSubresource(buffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out var mapped);
                    try
                    {
                        mapped.WriteRange(vertices);
                    }
                    finally
                    {
                        mapped.Dispose();
                        commands.UnmapSubresource(buffer, 0);
                    }
                    uploaded = vertices;
                }
                commands.InputAssembler.PrimitiveTopology = topology;
                commands.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(buffer, OverlayStride, 0));
                commands.PixelShader.Set(shader);
                commands.Draw(vertices.Length, 0);
            }

            private void EnsureTargets(int requestedSize)
            {
                if (size == requestedSize)
                    return;
                ReleaseTargets();
                var description = new Texture2DDescription
                {
                    Width = requestedSize, Height = requestedSize, ArraySize = 1, MipLevels = 1,
                    SampleDescription = new SampleDescription(1, 0), Format = ColorFormat,
                    Usage = ResourceUsage.Default, BindFlags = BindFlags.RenderTarget
                };
                color = new Texture2D(Device, description);
                renderTarget = new RenderTargetView(Device, color);
                description.Format = Format.R24G8_Typeless;
                description.BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource;
                depth = new Texture2D(Device, description);
                depthTarget = new DepthStencilView(Device, depth, new DepthStencilViewDescription
                {
                    Format = Format.D24_UNorm_S8_UInt, Dimension = DepthStencilViewDimension.Texture2D
                });
                depthView = new ShaderResourceView(Device, depth, new ShaderResourceViewDescription
                {
                    Format = Format.R24_UNorm_X8_Typeless, Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource { MipLevels = 1 }
                });
                size = requestedSize;
            }

            private static DepthStencilStateDescription HullDepthState(bool solid)
            {
                var operation = new DepthStencilOperationDescription
                {
                    Comparison = solid ? D3DComparison.Always : D3DComparison.Equal,
                    DepthFailOperation = StencilOperation.Keep, FailOperation = StencilOperation.Keep,
                    PassOperation = solid ? StencilOperation.Replace : StencilOperation.Keep
                };
                return new DepthStencilStateDescription
                {
                    IsDepthEnabled = true, DepthComparison = D3DComparison.Less, DepthWriteMask = DepthWriteMask.All,
                    IsStencilEnabled = true, StencilReadMask = 255, StencilWriteMask = solid ? (byte)255 : (byte)0,
                    FrontFace = operation, BackFace = operation
                };
            }

            private static InputElement[] ModelElements()
            {
                var elements = new InputElement[7];
                elements[0] = new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0);
                elements[1] = new InputElement("TEXCOORD", 0, Format.R32G32_Float, 12, 0);
                for (int i = 0; i < 5; i++)
                    elements[i + 2] = new InputElement("INSTANCE", i, Format.R32G32B32A32_Float,
                        i * 16, 1, InputClassification.PerInstanceData, 1);
                return elements;
            }

            private static CompilationResult Compile(string entryPoint, string profile)
            {
                return ShaderBytecode.Compile(ShaderSource, entryPoint, profile, ShaderFlags.OptimizationLevel3);
            }

            private PixelShader Pixel(string entryPoint)
            {
                using (var bytecode = Compile(entryPoint, "ps_4_0"))
                    return new PixelShader(Device, bytecode);
            }

            private void ReleaseBatches()
            {
                foreach (var batch in batches)
                    batch.Dispose();
                batches.Clear();
                materials.Clear();
            }

            private void ReleaseTargets()
            {
                depthView?.Dispose();
                depthTarget?.Dispose();
                depth?.Dispose();
                renderTarget?.Dispose();
                color?.Dispose();
                depthView = null;
                depthTarget = null;
                depth = color = null;
                renderTarget = null;
                size = 0;
            }

            public void ClearScene()
            {
                // Keep device-specific shaders and state for the next ship.
                ReleaseBatches();
                mesh = null;
                alphas = null;
                TexturesPending = false;
                ReleaseTargets();
                overlayFills?.Dispose();
                overlayLines?.Dispose();
                overlayFills = overlayLines = null;
                uploadedFills = uploadedLines = null;
            }

            public void Dispose()
            {
                ClearScene();
                cameraBuffer?.Dispose();
                sampler?.Dispose();
                noDepth?.Dispose();
                dimDepth?.Dispose();
                solidDepth?.Dispose();
                overlayBlend?.Dispose();
                rasterizer?.Dispose();
                overlayLayout?.Dispose();
                modelLayout?.Dispose();
                linePixel?.Dispose();
                fillPixel?.Dispose();
                texturePixel?.Dispose();
                paintPixel?.Dispose();
                overlayVertex?.Dispose();
                modelVertex?.Dispose();
                commands?.Dispose();
            }
        }

        private sealed class RenderBatch : IDisposable
        {
            public readonly TexturedShipGpuBatch Source;
            public readonly Buffer Vertices, Indices;
            public Buffer Solid, Dim;
            public int SolidCount, DimCount;

            public RenderBatch(Device device, TexturedShipGpuBatch source)
            {
                Source = source;
                Vertices = Buffer.Create(device, BindFlags.VertexBuffer, source.Vertices);
                try
                {
                    Indices = Buffer.Create(device, BindFlags.IndexBuffer, source.Indices);
                }
                catch
                {
                    Vertices.Dispose();
                    throw;
                }
            }

            public void SetInstances(Device device, byte[] alphas)
            {
                Solid?.Dispose();
                Dim?.Dispose();
                Solid = Dim = null;
                SolidCount = DimCount = 0;
                var solid = new List<InstanceData>();
                var dim = new List<InstanceData>();
                foreach (var instance in Source.Instances)
                {
                    byte alpha = alphas == null ? (byte)255
                        : (uint)instance.Part < (uint)alphas.Length ? alphas[instance.Part] : (byte)0;
                    if (alpha == 255)
                        solid.Add(new InstanceData(instance, alpha));
                    else if (alpha != 0)
                        dim.Add(new InstanceData(instance, alpha));
                }
                SolidCount = solid.Count;
                DimCount = dim.Count;
                if (SolidCount != 0)
                    Solid = Buffer.Create(device, BindFlags.VertexBuffer, solid.ToArray());
                if (DimCount != 0)
                    Dim = Buffer.Create(device, BindFlags.VertexBuffer, dim.ToArray());
            }

            public void Dispose()
            {
                Solid?.Dispose();
                Dim?.Dispose();
                Indices.Dispose();
                Vertices.Dispose();
            }
        }
    }
}
