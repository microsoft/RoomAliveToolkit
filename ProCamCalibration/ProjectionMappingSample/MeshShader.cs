using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.WIC;
using Device = SharpDX.Direct3D11.Device;

namespace RoomAliveToolkit
{

    public class PointLight
    {
        public PointLight()
        {
            Ia = Vector3.One;
            Id = Vector3.One;
            Is = Vector3.One;
        }
        public Vector3 position;
        public Vector3 Ia, Id, Is;
    }

    public class MeshDeviceResources
    {
        public MeshDeviceResources(Device device, SharpDX.WIC.ImagingFactory2 imagingFactory, Mesh mesh)
        {
            this.mesh = mesh;

            // create single vertex buffer
            var stream = new DataStream(mesh.vertices.Count * Mesh.VertexPositionNormalTexture.sizeInBytes, true, true);
            stream.WriteRange(mesh.vertices.ToArray());
            stream.Position = 0;

            var vertexBufferDesc = new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                Usage = ResourceUsage.Default,
                SizeInBytes = mesh.vertices.Count * Mesh.VertexPositionNormalTexture.sizeInBytes,
            };
            vertexBuffer = new SharpDX.Direct3D11.Buffer(device, stream, vertexBufferDesc);

            stream.Dispose();

            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, Mesh.VertexPositionNormalTexture.sizeInBytes, 0);

            foreach (var subset in mesh.subsets)
            {
                if (subset.material.textureFilename != null)
                {
                    var decoder = new SharpDX.WIC.BitmapDecoder(imagingFactory, subset.material.textureFilename, SharpDX.WIC.DecodeOptions.CacheOnLoad);
                    var bitmapFrameDecode = decoder.GetFrame(0);

                    var stagingTextureDesc = new Texture2DDescription()
                    {
                        Width = bitmapFrameDecode.Size.Width,
                        Height = bitmapFrameDecode.Size.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                        Usage = ResourceUsage.Dynamic,
                        BindFlags = BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.Write
                    };
                    var stagingTexture = new Texture2D(device, stagingTextureDesc);

                    var textureDesc = new Texture2DDescription()
                    {
                        Width = bitmapFrameDecode.Size.Width,
                        Height = bitmapFrameDecode.Size.Height,
                        MipLevels = 0,
                        ArraySize = 1,
                        Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.GenerateMipMaps
                    };
                    var texture = new Texture2D(device, textureDesc);

                    // convert to 32 bpp
                    var formatConverter = new FormatConverter(imagingFactory);
                    formatConverter.Initialize(bitmapFrameDecode, SharpDX.WIC.PixelFormat.Format32bppBGR);
                    var dataBox = device.ImmediateContext.MapSubresource(stagingTexture, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                    formatConverter.CopyPixels(dataBox.RowPitch, dataBox.DataPointer, dataBox.RowPitch * bitmapFrameDecode.Size.Height);
                    device.ImmediateContext.UnmapSubresource(stagingTexture, 0);

                    var resourceRegion = new ResourceRegion()
                    {
                        Left = 0,
                        Top = 0,
                        Right = bitmapFrameDecode.Size.Width,
                        Bottom = bitmapFrameDecode.Size.Height,
                        Front = 0,
                        Back = 1,
                    };
                    device.ImmediateContext.CopySubresourceRegion(stagingTexture, 0, resourceRegion, texture, 0);
                    var textureRV = new ShaderResourceView(device, texture);
                    device.ImmediateContext.GenerateMips(textureRV);

                    decoder.Dispose();
                    formatConverter.Dispose();
                    bitmapFrameDecode.Dispose();

                    textureRVs[subset] = textureRV;
                }
            }
        }


        public Mesh mesh;
        public SharpDX.Direct3D11.Buffer vertexBuffer;
        public VertexBufferBinding vertexBufferBinding;
        public Dictionary<Mesh.Subset, ShaderResourceView> textureRVs = new Dictionary<Mesh.Subset, ShaderResourceView>();
    }

    public class MeshShader
    {
        public MeshShader(Device device)
        {
            shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/MeshVS.cso"));
            meshVS = new VertexShader(device, shaderByteCode);
            meshPS = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/MeshPS.cso")));
            meshWithTexturePS = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/MeshWithTexturePS.cso")));

            // depth stencil state
            var depthStencilStateDesc = new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.LessEqual,
                IsStencilEnabled = false,
            };
            depthStencilState = new DepthStencilState(device, depthStencilStateDesc);

            // rasterizer state
            var rasterizerStateDesc = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = true,
                IsFrontCounterClockwise = true,
                IsMultisampleEnabled = true,
            };
            rasterizerState = new RasterizerState(device, rasterizerStateDesc);

            // color sampler state
            var colorSamplerStateDesc = new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                //BorderColor = new SharpDX.Color4(0.5f, 0.5f, 0.5f, 1.0f),
                //BorderColor = new SharpDX.Color4(0, 0, 0, 1.0f),
            };
            colorSamplerState = new SamplerState(device, colorSamplerStateDesc);

            // constant buffer
            var VSConstantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = VSConstantBuffer.size,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            vertexShaderConstantBuffer = new SharpDX.Direct3D11.Buffer(device, VSConstantBufferDesc);

            var PSConstantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = PSConstantBuffer.size,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            pixelShaderConstantBuffer = new SharpDX.Direct3D11.Buffer(device, PSConstantBufferDesc);

            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("SV_POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 16, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 24, 0),
            });

        }

        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = VSConstantBuffer.size)]
        unsafe struct VSConstantBuffer
        {
            public const int size = 128 + 16;

            [FieldOffset(0)]
            public fixed float world[16]; // 4-component padding
            [FieldOffset(64)]
            public fixed float viewProjection[16]; // 4-component padding
            [FieldOffset(128)]
            public fixed float lightPosition[3];
        };

        public unsafe void SetVertexShaderConstants(DeviceContext deviceContext, SharpDX.Matrix world, SharpDX.Matrix viewProjection, Vector3 lightPosition)
        {
            // hlsl matrices are default column order
            var constants = new VSConstantBuffer();
            for (int i = 0, col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                {
                    constants.world[i] = world[row, col];
                    constants.viewProjection[i] = viewProjection[row, col];
                    i++;
                }

            for (int i = 0; i < 3; i++)
                constants.lightPosition[i] = lightPosition[i];

            DataStream dataStream;
            deviceContext.MapSubresource(vertexShaderConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<VSConstantBuffer>(constants);
            deviceContext.UnmapSubresource(vertexShaderConstantBuffer, 0);
        }

        [StructLayout(LayoutKind.Explicit, Size = VSConstantBuffer.size)]
        unsafe struct PSConstantBuffer
        {
            public const int size = 108 + 4;

            [FieldOffset(0)]
            public fixed float cameraPosition[3];
            [FieldOffset(16)]
            public fixed float Ia[3];
            [FieldOffset(32)]
            public fixed float Id[3];
            [FieldOffset(48)]
            public fixed float Is[3];
            [FieldOffset(64)]
            public fixed float Ka[3];
            [FieldOffset(80)]
            public fixed float Kd[3];
            [FieldOffset(96)]
            public fixed float Ks[3];
            [FieldOffset(108)]
            public float Ns;
        };

        public unsafe void SetPixelShaderConstants(DeviceContext deviceContext, Mesh.Material material, PointLight light)
        {
            // hlsl matrices are default column order
            var constants = new PSConstantBuffer();
            for (int i = 0; i < 3; i++)
            {
                constants.Ka[i] = material.ambientColor[i];
                constants.Kd[i] = material.diffuseColor[i];
                constants.Ks[i] = material.specularColor[i];

                constants.Ia[i] = light.Ia[i];
                constants.Id[i] = light.Id[i];
                constants.Is[i] = light.Is[i];
            }
            constants.Ns = material.shininess;

            // TODO: add camera position

            DataStream dataStream;
            deviceContext.MapSubresource(pixelShaderConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<PSConstantBuffer>(constants);
            deviceContext.UnmapSubresource(pixelShaderConstantBuffer, 0);
        }

        public void Render(DeviceContext deviceContext, MeshDeviceResources meshDeviceResources, PointLight pointLight, RenderTargetView renderTargetView, DepthStencilView depthStencilView, Viewport viewport)
        {
            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            deviceContext.InputAssembler.SetVertexBuffers(0, meshDeviceResources.vertexBufferBinding);
            deviceContext.Rasterizer.State = rasterizerState;
            deviceContext.Rasterizer.SetViewport(viewport);
            deviceContext.VertexShader.Set(meshVS);
            deviceContext.VertexShader.SetConstantBuffer(0, vertexShaderConstantBuffer);
            deviceContext.GeometryShader.Set(null);
            deviceContext.PixelShader.Set(meshPS);
            deviceContext.PixelShader.SetSampler(0, colorSamplerState);
            deviceContext.PixelShader.SetConstantBuffer(0, pixelShaderConstantBuffer);
            deviceContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
            deviceContext.OutputMerger.DepthStencilState = depthStencilState;

            foreach (var subset in meshDeviceResources.mesh.subsets)
            //var subset = meshDeviceResources.mesh.subsets[meshDeviceResources.mesh.subsets.Count - 2];
            {
                if (subset.material.textureFilename != null)
                {
                    deviceContext.PixelShader.Set(meshWithTexturePS);
                    deviceContext.PixelShader.SetShaderResource(0, meshDeviceResources.textureRVs[subset]);
                }
                else
                    deviceContext.PixelShader.Set(meshPS);

                SetPixelShaderConstants(deviceContext, subset.material, pointLight);
                deviceContext.Draw(subset.length, subset.start);
            }

        }


        VertexShader meshVS;
        PixelShader meshPS, meshWithTexturePS;
        ShaderBytecode shaderByteCode;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SamplerState colorSamplerState;
        SharpDX.Direct3D11.Buffer vertexShaderConstantBuffer, pixelShaderConstantBuffer;
        InputLayout vertexInputLayout;
    }
}
