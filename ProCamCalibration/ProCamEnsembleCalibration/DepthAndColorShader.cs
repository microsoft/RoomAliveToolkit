using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;

namespace RoomAliveToolkit
{
    public class DepthAndColorCameraShader : IDisposable
    {
        public DepthAndColorCameraShader(Device device)
        {
            var shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorVS.cso"));
            vertexShader = new VertexShader(device, shaderByteCode);
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
                CullMode = CullMode.None, // beware what this does to both shaders
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
                BorderColor = new SharpDX.Color4(0.5f, 0.5f, 0.5f, 1.0f),
                //BorderColor = new SharpDX.Color4(0, 0, 0, 1.0f),
            };
            colorSamplerState = new SamplerState(device, colorSamplerStateDesc);

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

            // vertex layout; vertex buffer is created on demand
            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("pos", 0, Format.R32G32B32_Float, 0, 0),
            });
        }

        VertexShader vertexShader;
        PixelShader pixelShader;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SamplerState colorSamplerState;
        SharpDX.Direct3D11.Buffer constantBuffer;
        InputLayout vertexInputLayout;

        public void Dispose()
        {
            vertexShader.Dispose();
            pixelShader.Dispose();
            depthStencilState.Dispose();
            rasterizerState.Dispose();
            colorSamplerState.Dispose();
            constantBuffer.Dispose();
            vertexInputLayout.Dispose();
        }

        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = ConstantBuffer.size)]
        unsafe struct ConstantBuffer
        {
            public const int size = 160;

            [FieldOffset(0)]
            public fixed float worldToColor[16]; // 4-component padding
            [FieldOffset(64)]
            public fixed float projection[16];
            [FieldOffset(128)]
            public fixed float f[2];
            [FieldOffset(136)]
            public fixed float c[2];
            [FieldOffset(144)]
            public float k1;
            [FieldOffset(148)]
            public float k2;
        };

        public unsafe void SetConstants(DeviceContext deviceContext, Kinect2Calibration kinect2Calibration, SharpDX.Matrix colorCameraPose, SharpDX.Matrix projection)
        {
            // hlsl matrices are default column order
            var constants = new ConstantBuffer();
            for (int i = 0, col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                {
                    constants.projection[i] = projection[row, col];
                    constants.worldToColor[i] = (float)colorCameraPose[row, col];
                    i++;
                }
            constants.f[0] = (float)kinect2Calibration.colorCameraMatrix[0, 0] / (float)Kinect2Calibration.colorImageWidth;
            constants.f[1] = -(float)kinect2Calibration.colorCameraMatrix[1, 1] / (float)Kinect2Calibration.colorImageHeight;
            constants.c[0] = (float)kinect2Calibration.colorCameraMatrix[0, 2] / (float)Kinect2Calibration.colorImageWidth;
            constants.c[1] = 1f - (float)kinect2Calibration.colorCameraMatrix[1, 2] / (float)Kinect2Calibration.colorImageHeight;
            constants.k1 = (float)kinect2Calibration.colorLensDistortion[0];
            constants.k2 = (float)kinect2Calibration.colorLensDistortion[1];

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<ConstantBuffer>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        public void RenderCamera(DeviceContext deviceContext, 
            ProjectorCameraEnsemble.Camera camera, 
            CameraDeviceResource cameraDeviceResource, 
            VertexBufferBinding vertexBufferBinding, 
            SharpDX.Direct3D11.Buffer indexBuffer,
            SharpDX.Matrix worldViewProjection)
        {
            deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            deviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            deviceContext.PixelShader.Set(pixelShader);
            deviceContext.PixelShader.SetSampler(0, colorSamplerState);
            deviceContext.OutputMerger.DepthStencilState = depthStencilState;
            deviceContext.Rasterizer.State = rasterizerState;

            var depthToWorld = new SharpDX.Matrix();
            var depthToColor = new SharpDX.Matrix();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    depthToWorld[i, j] = (float)camera.pose[i, j];
                    depthToColor[i, j] = (float)camera.calibration.depthToColorTransform[i, j];
                }
            depthToWorld.Transpose();
            depthToColor.Transpose();
            var worldToDepth = depthToWorld;
            worldToDepth.Invert();
            var worldToColor = worldToDepth * depthToColor;

            SetConstants(deviceContext, camera.calibration, worldToColor, worldViewProjection);

            deviceContext.PixelShader.SetShaderResource(0, cameraDeviceResource.colorImageTextureRV);
            int numVertices = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6;
            deviceContext.DrawIndexed(numVertices, numVertices * cameraDeviceResource.cameraIndex, 0);


            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            deviceContext.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
        }

    }
}
