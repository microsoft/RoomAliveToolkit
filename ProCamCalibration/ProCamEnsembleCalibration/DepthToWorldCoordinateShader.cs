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
        public DepthToWorldCoordinateShader(Device device, int numCameras)
        {
            this.numCameras = numCameras;

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

            // vertex buffer
            var vertexBufferDesc = new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.VertexBuffer,
                OptionFlags = ResourceOptionFlags.BufferAllowRawViews,
                CpuAccessFlags = CpuAccessFlags.None,
                Usage = ResourceUsage.Default,
                SizeInBytes = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 3 * 4 * numCameras,
            };
            vertexBuffer = new SharpDX.Direct3D11.Buffer(device, vertexBufferDesc);
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, 3 * 4, 0);

            // index buffer
            var indexBufferDesc = new BufferDescription()
            {
                BindFlags = BindFlags.UnorderedAccess | BindFlags.IndexBuffer,
                OptionFlags = ResourceOptionFlags.BufferAllowRawViews,
                CpuAccessFlags = CpuAccessFlags.None,
                Usage = ResourceUsage.Default,
                SizeInBytes = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6 * 4 * numCameras,
            };
            indexBuffer = new SharpDX.Direct3D11.Buffer(device, indexBufferDesc);
        }

        int numCameras;
        ComputeShader computeShader;
        public SharpDX.Direct3D11.Buffer constantBuffer, vertexBuffer, indexBuffer;
        public VertexBufferBinding vertexBufferBinding;

        public void Dispose()
        {
            computeShader.Dispose();
            constantBuffer.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
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

        public unsafe void SetConstants(DeviceContext deviceContext, Matrix world, int indexOffset)
        {
            // hlsl matrices are default column order
            var constants = new ConstantBuffer();
            for (int i = 0, col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                    constants.world[i++] = (float) world[row, col];
            constants.indexOffset = (uint) indexOffset;

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<ConstantBuffer>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        public void UpdateCamera(DeviceContext deviceContext, ProjectorCameraEnsemble.Camera camera, CameraDeviceResource cameraDeviceResource)
        {
            int offset = cameraDeviceResource.cameraIndex * Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight;
            SetConstants(deviceContext, camera.pose, offset);

            deviceContext.ComputeShader.Set(computeShader);
            deviceContext.ComputeShader.SetConstantBuffer(0, constantBuffer);
            deviceContext.ComputeShader.SetShaderResource(0, cameraDeviceResource.depthImageTextureRV);
            deviceContext.ComputeShader.SetShaderResource(1, cameraDeviceResource.depthFrameToCameraSpaceTableTextureRV);
            deviceContext.ComputeShader.SetUnorderedAccessView(0, cameraDeviceResource.vertexBufferUAV);
            deviceContext.ComputeShader.SetUnorderedAccessView(1, cameraDeviceResource.indexBufferUAV);

            deviceContext.Dispatch(16, 22, 1);

            deviceContext.ComputeShader.SetShaderResource(0, null);
            deviceContext.ComputeShader.SetShaderResource(1, null);
            deviceContext.ComputeShader.SetUnorderedAccessView(0, null);
            deviceContext.ComputeShader.SetUnorderedAccessView(1, null);
            deviceContext.ComputeShader.Set(null);
        }
    }
}
