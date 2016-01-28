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
            toFloatComputeShader = new ComputeShader(device, new ShaderBytecode(File.ReadAllBytes("Content/Depth.ToFloatCS.cso")));
            bilateralFilterComputeShader = new ComputeShader(device, new ShaderBytecode(File.ReadAllBytes("Content/Depth.BilateralFilterCS.cso")));
            computeNormalsComputeShader = new ComputeShader(device, new ShaderBytecode(File.ReadAllBytes("Content/Depth.ComputeNormalsCS.cso")));
            intializeNormalsComputeShader = new ComputeShader(device, new ShaderBytecode(File.ReadAllBytes("Content/Depth.InitializeNormals.cso")));

            // constant buffer
            var constantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = 16,
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
                SizeInBytes = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6 * 4 * numCameras,
            };
            vertexBuffer = new SharpDX.Direct3D11.Buffer(device, vertexBufferDesc);
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer, 6 * 4, 0);

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

            // float depth buffer
            var floatDepthImageTextureDesc = new Texture2DDescription()
            {
                Width = Kinect2Calibration.depthImageWidth,
                Height = Kinect2Calibration.depthImageHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            floatDepthImageTexture = new Texture2D(device, floatDepthImageTextureDesc);
            floatDepthImageUAV = new UnorderedAccessView(device, floatDepthImageTexture);
            floatDepthImageSRV = new ShaderResourceView(device, floatDepthImageTexture);


            floatDepthImageTexture2 = new Texture2D(device, floatDepthImageTextureDesc);
            floatDepthImageUAV2 = new UnorderedAccessView(device, floatDepthImageTexture2);
            floatDepthImageSRV2 = new ShaderResourceView(device, floatDepthImageTexture2);

            // constant buffer
            var bilateralFilterConstantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = 80,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            bilateralFilterConstantBuffer = new SharpDX.Direct3D11.Buffer(device, bilateralFilterConstantBufferDesc);

            // upper and lower normals
            var quadInfoStructuredBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                StructureByteStride = 24,
                SizeInBytes = 512 * 484 * 24,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                OptionFlags = ResourceOptionFlags.BufferStructured,
            };
            quadInfoBuffer = new SharpDX.Direct3D11.Buffer(device, quadInfoStructuredBufferDesc);
            quadInfoBufferUAV = new UnorderedAccessView(device, quadInfoBuffer);

        }

        int numCameras;
        ComputeShader computeShader, toFloatComputeShader, bilateralFilterComputeShader, computeNormalsComputeShader, intializeNormalsComputeShader;
        public SharpDX.Direct3D11.Buffer constantBuffer, vertexBuffer, indexBuffer, bilateralFilterConstantBuffer;
        public VertexBufferBinding vertexBufferBinding;
        public Texture2D floatDepthImageTexture, floatDepthImageTexture2;
        public UnorderedAccessView floatDepthImageUAV, floatDepthImageUAV2;
        public ShaderResourceView floatDepthImageSRV, floatDepthImageSRV2;
        public SharpDX.Direct3D11.Buffer quadInfoBuffer;
        public UnorderedAccessView quadInfoBufferUAV;

        public void Dispose()
        {
            computeShader.Dispose();
            constantBuffer.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        public unsafe void SetConstants(DeviceContext deviceContext, Matrix world, int indexOffset)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);

            //dataStream.PackedWrite(world.ToSharp4x4());
            dataStream.PackedWrite((uint)indexOffset);

            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        public unsafe void SetBilateralFilterConstants(DeviceContext deviceContext, Matrix world, float spatialSigma, float intensitySigma)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(bilateralFilterConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);

            dataStream.PackedWrite(world.ToSharp4x4());

            dataStream.PackedWrite((1.0f / spatialSigma)*(1.0f / spatialSigma));
            dataStream.PackedWrite((1.0f / intensitySigma)*(1.0f / intensitySigma));

            deviceContext.UnmapSubresource(bilateralFilterConstantBuffer, 0);
        }

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        public void UpdateCamera(DeviceContext deviceContext, ProjectorCameraEnsemble.Camera camera, CameraDeviceResource cameraDeviceResource)
        {
            //stopwatch.Restart();

            // bilateral filter
            SetBilateralFilterConstants(deviceContext, camera.pose, 3.0f, 40.0f);
            deviceContext.ComputeShader.SetConstantBuffer(0, bilateralFilterConstantBuffer);
            deviceContext.ComputeShader.Set(bilateralFilterComputeShader);
            deviceContext.ComputeShader.SetShaderResource(0, cameraDeviceResource.depthImageTextureRV);
            deviceContext.ComputeShader.SetShaderResource(1, cameraDeviceResource.depthFrameToCameraSpaceTableTextureRV);
            deviceContext.ComputeShader.SetUnorderedAccessView(0, floatDepthImageUAV2);
            deviceContext.ComputeShader.SetUnorderedAccessView(1, cameraDeviceResource.vertexBufferUAV);
            deviceContext.Dispatch(16, 22, 1);
            deviceContext.ComputeShader.SetShaderResource(0, null);
            deviceContext.ComputeShader.SetShaderResource(1, null);
            deviceContext.ComputeShader.SetUnorderedAccessView(0, null);
            deviceContext.ComputeShader.SetUnorderedAccessView(1, null);

            //Console.Write("\t" + stopwatch.ElapsedTicks);
            //stopwatch.Restart();


            // world coordinates and index buffer
            int offset = cameraDeviceResource.cameraIndex * Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight;
            SetConstants(deviceContext, camera.pose, offset);

            deviceContext.ComputeShader.Set(computeShader);
            deviceContext.ComputeShader.SetConstantBuffer(0, constantBuffer);
            deviceContext.ComputeShader.SetShaderResource(0, floatDepthImageSRV2);
            deviceContext.ComputeShader.SetShaderResource(1, cameraDeviceResource.depthFrameToCameraSpaceTableTextureRV);
            deviceContext.ComputeShader.SetUnorderedAccessView(0, cameraDeviceResource.vertexBufferUAV);
            deviceContext.ComputeShader.SetUnorderedAccessView(1, cameraDeviceResource.indexBufferUAV);
            deviceContext.ComputeShader.SetUnorderedAccessView(2, quadInfoBufferUAV);
            deviceContext.Dispatch(16, 22, 1);
            deviceContext.ComputeShader.SetShaderResource(0, null);
            deviceContext.ComputeShader.SetShaderResource(1, null);
            deviceContext.ComputeShader.SetUnorderedAccessView(0, null);
            deviceContext.ComputeShader.SetUnorderedAccessView(1, null);
            deviceContext.ComputeShader.SetUnorderedAccessView(2, null);

            ////Console.Write("\t" + stopwatch.ElapsedTicks);
            ////stopwatch.Restart();


            // compute normals
            deviceContext.ComputeShader.Set(computeNormalsComputeShader);
            deviceContext.ComputeShader.SetUnorderedAccessView(0, cameraDeviceResource.vertexBufferUAV);
            deviceContext.ComputeShader.SetUnorderedAccessView(1, quadInfoBufferUAV);
            deviceContext.Dispatch(16, 22, 1);
            deviceContext.ComputeShader.SetUnorderedAccessView(0, null);


            //Console.Write("\t" + stopwatch.ElapsedTicks);

            //Console.WriteLine();


        }
    }
}
