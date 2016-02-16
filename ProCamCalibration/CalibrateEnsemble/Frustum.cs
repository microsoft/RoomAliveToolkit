using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomAliveToolkit
{
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

    public class FrustumShader
    {
        struct VertexPosition
        {
            public SharpDX.Vector4 position;
            static public int SizeInBytes { get { return 4 * 4; } }
        }


        public SharpDX.Direct3D11.Buffer vertexBuffer;
        public VertexBufferBinding vertexBufferBinding;

        public FrustumShader(Device device)
        {
            // create single vertex buffer
            var stream = new DataStream(6 * VertexPosition.SizeInBytes, true, true);
            stream.Write(new Vector4(0, 0, 0, 1));
            stream.Write(new Vector4(0, 0, 2, 1));
            stream.Write(new Vector4(0, -0.1f, 0, 1));
            stream.Write(new Vector4(0, 0.1f, 0, 1));
            stream.Write(new Vector4(-0.1f, 0, 0, 1));
            stream.Write(new Vector4(0.1f, 0, 0, 1));

            stream.Position = 0;

            var vertexBufferDesc = new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                Usage = ResourceUsage.Default,
                SizeInBytes = 6 * VertexPosition.SizeInBytes,
            };
            vertexBuffer = new SharpDX.Direct3D11.Buffer(device, stream, vertexBufferDesc);

            stream.Dispose();

            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, VertexPosition.SizeInBytes, 0);


            shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/frustumVS.cso"));
            frustumVS = new VertexShader(device, shaderByteCode);
            frustumPS = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/frustumPS.cso")));

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
                FillMode = FillMode.Wireframe,
                IsDepthClipEnabled = true,
                IsFrontCounterClockwise = true,
                IsMultisampleEnabled = true,
            };
            rasterizerState = new RasterizerState(device, rasterizerStateDesc);

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

            // Pixel shader constant buffer
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
            });
        }

        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = VSConstantBuffer.size)]
        unsafe struct VSConstantBuffer
        {
            public const int size = 64 + 64 + 64;

            [FieldOffset(0)]
            public fixed float world[16]; 
            [FieldOffset(64)]
            public fixed float viewProjection[16]; 
            [FieldOffset(128)]
            public fixed float frustumProjection[16];
        };


        [StructLayout(LayoutKind.Explicit, Size = PSConstantBuffer.size)]
        unsafe struct PSConstantBuffer
        {
            public const int size = 16;

            [FieldOffset(0)]
            public fixed float color[3];
        };

        public unsafe void SetVertexShaderConstants(DeviceContext deviceContext, SharpDX.Matrix world, SharpDX.Matrix viewProjection, SharpDX.Matrix frustumProjection)
        {
            // hlsl matrices are default column order
            var constants = new VSConstantBuffer();
            for (int i = 0, col = 0; col < 4; col++)
            {
                for (int row = 0; row < 4; row++)
                {
                    constants.world[i] = world[row, col];
                    constants.viewProjection[i] = viewProjection[row, col];
                    constants.frustumProjection[i] = frustumProjection[row, col];
                    i++;
                }
            }

            DataStream dataStream;
            deviceContext.MapSubresource(vertexShaderConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<VSConstantBuffer>(constants);
            deviceContext.UnmapSubresource(vertexShaderConstantBuffer, 0);
        }

        public unsafe void SetPixelShaderConstants(DeviceContext deviceContext, SharpDX.Color3 color)
        {
            var constants = new PSConstantBuffer();
            for (int i = 0; i < 3; i++)
            {
                constants.color[i] = color[i];
            }

            DataStream dataStream;
            deviceContext.MapSubresource(pixelShaderConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<PSConstantBuffer>(constants);
            deviceContext.UnmapSubresource(pixelShaderConstantBuffer, 0);
        }

        public void Render(DeviceContext deviceContext)
        {
            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;
            deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            deviceContext.Rasterizer.State = rasterizerState;

            deviceContext.VertexShader.Set(frustumVS);
            deviceContext.VertexShader.SetConstantBuffer(0, vertexShaderConstantBuffer);

            deviceContext.GeometryShader.Set(null);

            deviceContext.PixelShader.SetConstantBuffer(0, pixelShaderConstantBuffer);
            deviceContext.PixelShader.Set(frustumPS);

            deviceContext.Draw(6, 0);
        }

        VertexShader frustumVS;
        PixelShader frustumPS;
        ShaderBytecode shaderByteCode;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SharpDX.Direct3D11.Buffer vertexShaderConstantBuffer;
        SharpDX.Direct3D11.Buffer pixelShaderConstantBuffer;
        InputLayout vertexInputLayout;
    }
}
