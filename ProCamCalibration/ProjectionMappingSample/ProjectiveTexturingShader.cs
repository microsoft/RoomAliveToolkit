using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.IO;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;

namespace RoomAliveToolkit
{
    public class ProjectiveTexturingShader
    {
        public ProjectiveTexturingShader(Device device)
        {
            var shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/DepthAndProjectiveTextureVS.cso"));
            vertexShader = new VertexShader(device, shaderByteCode);
            geometryShader = new GeometryShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorGS.cso")));
            pixelShader = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorPS.cso")));

            // depth stencil state
            var depthStencilStateDesc = new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.LessEqual,
                IsStencilEnabled = false,
            };
            depthStencilState = new DepthStencilState(device, depthStencilStateDesc);

            // rasterizer states
            var rasterizerStateDesc = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = true,
                IsFrontCounterClockwise = true,
                IsMultisampleEnabled = true,
            };
            rasterizerState = new RasterizerState(device, rasterizerStateDesc);

            // constant buffer
            var constantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = Constants.size,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            constantBuffer = new SharpDX.Direct3D11.Buffer(device, constantBufferDesc);

            // user view sampler state
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

            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("SV_POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            });

        }

        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = Constants.size)]
        public unsafe struct Constants
        {
            public const int size = 128;
            [FieldOffset(0)]
            public fixed float userWorldViewProjection[16];
            [FieldOffset(64)]
            public fixed float projectorWorldViewProjection[16];
        };

        public unsafe void SetConstants(DeviceContext deviceContext, SharpDX.Matrix userWorldViewProjection, SharpDX.Matrix projectorWorldViewProjection)
        {
            Constants constants = new Constants();

            for (int i = 0, col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                {
                    constants.userWorldViewProjection[i] = userWorldViewProjection[row, col];
                    constants.projectorWorldViewProjection[i] = projectorWorldViewProjection[row, col];
                    i++;
                }

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<Constants>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        public void Render(DeviceContext deviceContext, ShaderResourceView depthImageTextureRV, ShaderResourceView colorImageTextureRV, SharpDX.Direct3D11.Buffer vertexBuffer, RenderTargetView renderTargetView, DepthStencilView depthStencilView, Viewport viewport)
        {
            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, VertexPosition.SizeInBytes, 0)); // bytes per vertex
            deviceContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
            deviceContext.OutputMerger.DepthStencilState = depthStencilState;
            deviceContext.Rasterizer.State = rasterizerState;
            deviceContext.Rasterizer.SetViewport(viewport);
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.VertexShader.SetShaderResource(0, depthImageTextureRV);
            deviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            deviceContext.GeometryShader.Set(geometryShader);
            deviceContext.PixelShader.Set(pixelShader);
            deviceContext.PixelShader.SetShaderResource(0, colorImageTextureRV);
            deviceContext.PixelShader.SetSampler(0, colorSamplerState);
            deviceContext.Draw((Kinect2Calibration.depthImageWidth - 1) * (Kinect2Calibration.depthImageHeight - 1) * 6, 0);

            deviceContext.VertexShader.SetShaderResource(0, null); // to avoid warnings when these are later set as render targets
            deviceContext.PixelShader.SetShaderResource(0, null);
        }

        struct VertexPosition
        {
            public SharpDX.Vector4 position;
            static public int SizeInBytes { get { return 4 * 4; } }
        }

        VertexShader vertexShader;
        GeometryShader geometryShader;
        PixelShader pixelShader;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SharpDX.Direct3D11.Buffer constantBuffer;
        SamplerState colorSamplerState;
        InputLayout vertexInputLayout;
    }
}
