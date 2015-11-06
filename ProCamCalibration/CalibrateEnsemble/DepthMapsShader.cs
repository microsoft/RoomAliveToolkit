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
    public class DepthMapsShader
    {
        public DepthMapsShader(Device device)
        {
            shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/DepthMapsVS.cso"));
            vertexShader = new VertexShader(device, shaderByteCode);
            geometryShader = new GeometryShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthMapsGS.cso")));
            pixelShader = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthMapsPS.cso")));

            var depthMapsTextureDesc = new Texture2DDescription()
            {
                Width = 1024,
                Height = 768,
                MipLevels = 1,
                ArraySize = 3,
                //Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Format = Format.R32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            var depthMapsTexture = new Texture2D(device, depthMapsTextureDesc);
            depthMapsRenderTargetView = new RenderTargetView(device, depthMapsTexture);

            // user view depth buffer
            var depthMapsDepthBufferDesc = new Texture2DDescription()
            {
                Width = 1024,
                Height = 768,
                MipLevels = 1,
                ArraySize = 3,
                Format = Format.D32_Float, // necessary?
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None
            };
            var depthMapsDepthStencil = new Texture2D(device, depthMapsDepthBufferDesc);
            depthMapsDepthStencilVuew = new DepthStencilView(device, depthMapsDepthStencil);

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

            //// Kinect depth image
            //var depthImageTextureDesc = new Texture2DDescription()
            //{
            //    Width = Kinect2Calibration.depthImageWidth,
            //    Height = Kinect2Calibration.depthImageHeight,
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

            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("SV_POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            });

        }


        InputLayout vertexInputLayout;

        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = ConstantBuffer.size)]
        unsafe struct ConstantBuffer
        {
            public const int size = 16*16*4; // TODO: check this

            [FieldOffset(0)]
            public fixed float projection[16*16];
        };


        public unsafe void SetConstants(DeviceContext deviceContext, RoomAliveToolkit.Kinect2Calibration kinect2Calibration, SharpDX.Matrix projection)
        {
            // hlsl matrices are default column order
            var constants = new ConstantBuffer();

            for (int i = 0, r = 0; r < 3; r++)
                for (int col = 0; col < 4; col++)
                    for (int row = 0; row < 4; row++)
                    {
                        constants.projection[i] = projection[row, col];
                        i++;
                    }

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<ConstantBuffer>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        //public void Render(DeviceContext deviceContext, ShaderResourceView depthImageTextureRV, SharpDX.Direct3D11.Buffer vertexBuffer, RenderTargetView renderTargetView, DepthStencilView depthStencilView, Viewport viewport)
        public void Render(DeviceContext deviceContext, ShaderResourceView depthImageTextureRV, SharpDX.Direct3D11.Buffer vertexBuffer, Viewport viewport)
        {
            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, 16, 0)); // bytes per vertex
            deviceContext.Rasterizer.State = rasterizerState;
            deviceContext.Rasterizer.SetViewport(viewport);
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.VertexShader.SetShaderResource(0, depthImageTextureRV);
            deviceContext.GeometryShader.Set(geometryShader);
            deviceContext.GeometryShader.SetConstantBuffer(0, constantBuffer);
            deviceContext.PixelShader.Set(pixelShader);
            deviceContext.OutputMerger.SetTargets(depthMapsDepthStencilVuew, depthMapsRenderTargetView);
            deviceContext.OutputMerger.DepthStencilState = depthStencilState;
            deviceContext.Draw((Kinect2Calibration.depthImageWidth - 1) * (Kinect2Calibration.depthImageHeight - 1) * 6, 0);
        }

        RenderTargetView depthMapsRenderTargetView;
        DepthStencilView depthMapsDepthStencilVuew;
        ShaderBytecode shaderByteCode;
        VertexShader vertexShader;
        GeometryShader geometryShader;
        PixelShader pixelShader;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SharpDX.Direct3D11.Buffer constantBuffer;
    }
}
