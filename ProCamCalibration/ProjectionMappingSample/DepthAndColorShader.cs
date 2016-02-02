using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using Device = SharpDX.Direct3D11.Device;

namespace RoomAliveToolkit
{
    public class DepthAndColorShader
    {
        public DepthAndColorShader(Device device)
        {
            shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorFloatVS.cso"));
            depthAndColorVS = new VertexShader(device, shaderByteCode);
            depthAndColorGS = new GeometryShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorGS.cso")));
            depthAndColorPS = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorPS.cso")));

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
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                //BorderColor = new SharpDX.Color4(0.5f, 0.5f, 0.5f, 1.0f),
                BorderColor = new SharpDX.Color4(0, 0, 0, 1.0f),
            };
            colorSamplerState = new SamplerState(device, colorSamplerStateDesc);

            //// Kinect depth image
            //var depthImageTextureDesc = new Texture2DDescription()
            //{
            //    Width = depthImageWidth,
            //    Height = depthImageHeight,
            //    MipLevels = 1,
            //    ArraySize = 1,
            //    Format = SharpDX.DXGI.Format.R16_UInt, // R32_Float
            //    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
            //    Usage = ResourceUsage.Dynamic,
            //    BindFlags = BindFlags.ShaderResource,
            //    CpuAccessFlags = CpuAccessFlags.Write,
            //};
            //depthImageTexture = new Texture2D(device, depthImageTextureDesc);
            //depthImageTextureRV = new ShaderResourceView(device, depthImageTexture);

            // filtered depth image
            var filteredDepthImageTextureDesc = new Texture2DDescription()
            {
                Width = Kinect2Calibration.depthImageWidth * 3,
                Height = Kinect2Calibration.depthImageHeight * 3,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R32G32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            filteredDepthImageTexture = new Texture2D(device, filteredDepthImageTextureDesc);
            filteredRenderTargetView = new RenderTargetView(device, filteredDepthImageTexture);
            filteredDepthImageSRV = new ShaderResourceView(device, filteredDepthImageTexture);

            filteredDepthImageTexture2 = new Texture2D(device, filteredDepthImageTextureDesc);
            filteredRenderTargetView2 = new RenderTargetView(device, filteredDepthImageTexture2);
            filteredDepthImageSRV2 = new ShaderResourceView(device, filteredDepthImageTexture2);



            //// Kinect color image
            //var colorImageStagingTextureDesc = new Texture2DDescription()
            //{
            //    Width = colorImageWidth,
            //    Height = colorImageHeight,
            //    MipLevels = 1,
            //    ArraySize = 1,
            //    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
            //    //Format = SharpDX.DXGI.Format.YUY2
            //    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
            //    Usage = ResourceUsage.Dynamic,
            //    BindFlags = BindFlags.ShaderResource,
            //    CpuAccessFlags = CpuAccessFlags.Write
            //};
            //colorImageStagingTexture = new Texture2D(device, colorImageStagingTextureDesc);

            //var colorImageTextureDesc = new Texture2DDescription()
            //{
            //    Width = colorImageWidth,
            //    Height = colorImageHeight,
            //    MipLevels = 0,
            //    ArraySize = 1,
            //    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
            //    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
            //    Usage = ResourceUsage.Default,
            //    BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
            //    CpuAccessFlags = CpuAccessFlags.None,
            //    OptionFlags = ResourceOptionFlags.GenerateMipMaps
            //};
            //colorImageTexture = new Texture2D(device, colorImageTextureDesc);
            //colorImageTextureRV = new ShaderResourceView(device, colorImageTexture);

            // constant buffer
            var constantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = ConstantBuffer.size,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            constantBuffer = new SharpDX.Direct3D11.Buffer(device, constantBufferDesc);

            bilateralFilter = new BilateralFilter(device, Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);

            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("SV_POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            });

        }

        public static SharpDX.Direct3D11.Buffer CreateVertexBuffer(Device device, RoomAliveToolkit.Kinect2Calibration kinect2Calibration)
        {
            // generate depthFrameToCameraSpace table
            var depthFrameToCameraSpaceTable = kinect2Calibration.ComputeDepthFrameToCameraSpaceTable(Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);


            int numVertices = 6 * (Kinect2Calibration.depthImageWidth - 1) * (Kinect2Calibration.depthImageHeight - 1);
            var vertices = new VertexPosition[numVertices];

            Int3[] quadOffsets = new Int3[]
            {
                new Int3(0, 0, 0),  
                new Int3(1, 0, 0),  
                new Int3(0, 1, 0),  
                new Int3(1, 0, 0),  
                new Int3(1, 1, 0),  
                new Int3(0, 1, 0),  
            };

            int vertexIndex = 0;
            for (int y = 0; y < Kinect2Calibration.depthImageHeight - 1; y++)
                for (int x = 0; x < Kinect2Calibration.depthImageWidth - 1; x++)
                    for (int i = 0; i < 6; i++)
                    {
                        int vertexX = x + quadOffsets[i].X;
                        int vertexY = y + quadOffsets[i].Y;

                        var point = depthFrameToCameraSpaceTable[Kinect2Calibration.depthImageWidth * vertexY + vertexX];

                        var vertex = new VertexPosition();
                        vertex.position = new SharpDX.Vector4(point.X, point.Y, vertexX, vertexY);
                        vertices[vertexIndex++] = vertex;
                    }

            var stream = new DataStream(numVertices * VertexPosition.SizeInBytes, true, true);
            stream.WriteRange(vertices);
            stream.Position = 0;

            var vertexBufferDesc = new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                Usage = ResourceUsage.Default,
                SizeInBytes = numVertices * VertexPosition.SizeInBytes,
            };
            var vertexBuffer = new SharpDX.Direct3D11.Buffer(device, stream, vertexBufferDesc);

            stream.Dispose();

            return vertexBuffer;
        }



        struct VertexPosition
        {
            public SharpDX.Vector4 position;
            static public int SizeInBytes { get { return 4 * 4;  } }
        }

        InputLayout vertexInputLayout;

        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = ConstantBuffer.size)]
        unsafe struct ConstantBuffer
        {
            public const int size = 160;

            [FieldOffset(0)]
            public fixed float depthToColorTransform[16]; // 4-component padding
            [FieldOffset(64)]
            public fixed float f[2];
            [FieldOffset(72)]
            public fixed float c[2];
            [FieldOffset(80)]
            public float k1;
            [FieldOffset(84)]
            public float k2;
            [FieldOffset(96)]
            public fixed float projection[16];
        };


        public unsafe void SetConstants(DeviceContext deviceContext, RoomAliveToolkit.Kinect2Calibration kinect2Calibration, SharpDX.Matrix projection)
        {
            // hlsl matrices are default column order
            var constants = new ConstantBuffer();
            for (int i = 0, col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                {
                    constants.projection[i] = projection[row, col];
                    constants.depthToColorTransform[i] = (float)kinect2Calibration.depthToColorTransform[row, col];
                    i++;
                }
            constants.f[0] = (float)kinect2Calibration.colorCameraMatrix[0, 0];
            constants.f[1] = (float)kinect2Calibration.colorCameraMatrix[1, 1];
            constants.c[0] = (float)kinect2Calibration.colorCameraMatrix[0, 2];
            constants.c[1] = (float)kinect2Calibration.colorCameraMatrix[1, 2];
            constants.k1 = (float)kinect2Calibration.colorLensDistortion[0];
            constants.k2 = (float)kinect2Calibration.colorLensDistortion[1];

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<ConstantBuffer>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);

        }


        public void Render(DeviceContext deviceContext, ShaderResourceView depthImageTextureRV, ShaderResourceView colorImageTextureRV, SharpDX.Direct3D11.Buffer vertexBuffer, RenderTargetView renderTargetView, DepthStencilView depthStencilView, Viewport viewport)
        {
            //bilateralFilter.Render(deviceContext, depthImageTextureRV, filteredRenderTargetView2);
            //bilateralFilter.Render(deviceContext, filteredDepthImageSRV2, filteredRenderTargetView);
            
            //bilateralFilter.Render(deviceContext, filteredDepthImageSRV, filteredRenderTargetView2);
            //bilateralFilter.Render(deviceContext, filteredDepthImageSRV2, filteredRenderTargetView);



            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VertexPosition.SizeInBytes, 0)); // bytes per vertex
            deviceContext.Rasterizer.State = rasterizerState;
            deviceContext.Rasterizer.SetViewport(viewport);
            deviceContext.VertexShader.Set(depthAndColorVS);
            deviceContext.VertexShader.SetShaderResource(0, depthImageTextureRV);
            //deviceContext.VertexShader.SetShaderResource(0, depthAndMaskRV);
            //deviceContext.VertexShader.SetShaderResource(0, filteredDepthImageSRV);
            deviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            deviceContext.GeometryShader.Set(depthAndColorGS);
            deviceContext.PixelShader.Set(depthAndColorPS);
            deviceContext.PixelShader.SetShaderResource(0, colorImageTextureRV);
            deviceContext.PixelShader.SetSampler(0, colorSamplerState);
            deviceContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
            deviceContext.OutputMerger.DepthStencilState = depthStencilState;
            deviceContext.Draw((Kinect2Calibration.depthImageWidth - 1) * (Kinect2Calibration.depthImageHeight - 1) * 6, 0);
        }

        VertexShader depthAndColorVS;
        GeometryShader depthAndColorGS;
        PixelShader depthAndColorPS;
        ShaderBytecode shaderByteCode;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SamplerState colorSamplerState;
        SharpDX.Direct3D11.Buffer constantBuffer; 
        BilateralFilter bilateralFilter;
        FromUIntPS fromUIntPS;
        Texture2D filteredDepthImageTexture, filteredDepthImageTexture2;
        RenderTargetView filteredRenderTargetView, filteredRenderTargetView2;
        ShaderResourceView filteredDepthImageSRV, filteredDepthImageSRV2;
    }

   

}
