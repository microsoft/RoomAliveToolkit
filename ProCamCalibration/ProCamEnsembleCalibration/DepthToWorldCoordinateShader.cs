using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;

namespace RoomAliveToolkit
{
    public class DepthToWorldCoordinateShader : IDisposable
    {
        public DepthToWorldCoordinateShader(Device device)
        {
            //this.numCameras = numCameras;

            computeShader = new ComputeShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthCS.cso")));

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

        }

        int numCameras;
        public ComputeShader computeShader;
        public SharpDX.Direct3D11.Buffer constantBuffer;
        public SharpDX.Direct3D11.Buffer vertexBuffer, indexBuffer;
        public VertexBufferBinding vertexBufferBinding;

        public void Dispose()
        {
            // TODO
        }

        [StructLayout(LayoutKind.Explicit, Size = ConstantBuffer.size)]
        unsafe struct ConstantBuffer
        {
            public const int size = 80; // multiple of 16

            [FieldOffset(0)]
            public fixed float world[16]; // 4-component padding
            [FieldOffset(64)]
            public uint indexOffset;
        };

        public unsafe void SetConstants(DeviceContext deviceContext, SharpDX.Matrix world, uint indexOffset)
        {
            // hlsl matrices are default column order
            var constants = new ConstantBuffer();
            for (int i = 0, col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                {
                    constants.world[i] = world[row, col];
                    i++;
                }
            constants.indexOffset = indexOffset;

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<ConstantBuffer>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }
    }
}
