using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;

namespace RoomAliveToolkit
{
    public class BlendShader : IDisposable
    {
        public BlendShader(Device device)
        {
            var shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/BlendVS.cso"));
            vertexShader = new VertexShader(device, shaderByteCode);
            pixelShader = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/BlendPS.cso")));

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
                IsMultisampleEnabled = false,
            };
            rasterizerState = new RasterizerState(device, rasterizerStateDesc);

            // color sampler state
            var colorSamplerStateDesc = new SamplerStateDescription()
            {
                Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                BorderColor = new SharpDX.Color4(0, 0, 0, 0),
                MaximumAnisotropy = 4,
            };
            colorSamplerState = new SamplerState(device, colorSamplerStateDesc);

            // constant buffer
            var vertexShaderConstantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = 64,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            vertexShaderConstantBuffer = new SharpDX.Direct3D11.Buffer(device, vertexShaderConstantBufferDesc);

            var pixelShaderConstantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = 784,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            pixelShaderConstantBuffer = new SharpDX.Direct3D11.Buffer(device, pixelShaderConstantBufferDesc);

            // vertex layout
            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("pos", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            });
        }

        VertexShader vertexShader;
        PixelShader pixelShader;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SamplerState colorSamplerState;
        SharpDX.Direct3D11.Buffer vertexShaderConstantBuffer, pixelShaderConstantBuffer;
        InputLayout vertexInputLayout;

        public void Dispose()
        {
            vertexShader.Dispose();
            pixelShader.Dispose();
            depthStencilState.Dispose();
            rasterizerState.Dispose();
            colorSamplerState.Dispose();
            vertexShaderConstantBuffer.Dispose();
            vertexInputLayout.Dispose();
        }

        public void SetVertexShaderConstants(DeviceContext deviceContext, ProjectorCameraEnsemble.Projector projector)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(vertexShaderConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);

            var view = projector.pose.ToSharp4x4();
            view.Invert();
            view.Transpose();

            var cameraMatrix = projector.cameraMatrix;
            float fx = (float)cameraMatrix[0, 0];
            float fy = (float)cameraMatrix[1, 1];
            float cx = (float)cameraMatrix[0, 2];
            float cy = (float)cameraMatrix[1, 2];

            float near = 0.1f;
            float far = 10.0f;

            float w = projector.width;
            float h = projector.height;

            var projection = GraphicsTransforms.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
            projection.Transpose();

            var viewProjection = view * projection;

            dataStream.PackedWrite(viewProjection);

            deviceContext.UnmapSubresource(vertexShaderConstantBuffer, 0);
        }

        public void SetPixelShaderConstants(DeviceContext deviceContext, 
            ProjectorCameraEnsemble.Projector thisProjector,
            List<ProjectorCameraEnsemble.Projector> projectors)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(pixelShaderConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);

            // numProjectors
            dataStream.PackedWrite((uint)projectors.Count);
            dataStream.PackedWrite((uint)projectors.IndexOf(thisProjector));

            foreach (var projector in projectors)
            {
                var view = projector.pose.ToSharp4x4();
                view.Invert();
                view.Transpose();

                var cameraMatrix = projector.cameraMatrix;
                float fx = (float)cameraMatrix[0, 0];
                float fy = (float)cameraMatrix[1, 1];
                float cx = (float)cameraMatrix[0, 2];
                float cy = (float)cameraMatrix[1, 2];

                float near = 0.1f;
                float far = 10.0f;

                float w = projector.width;
                float h = projector.height;

                var projection = GraphicsTransforms.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
                projection.Transpose();

                var viewProjection = view * projection;

                // every element of an array starts on a new 4-component vector
                dataStream.NextVector();

                // invertedFocalLength
                dataStream.PackedWrite(fx);

                // viewProjection
                dataStream.PackedWrite(viewProjection);

                // position
                var pose = projector.pose.ToSharp4x4();
                pose.Transpose();
                var position = pose.TranslationVector;
                dataStream.PackedWrite(position);
            }

            deviceContext.UnmapSubresource(pixelShaderConstantBuffer, 0);
        }


        public void Render(DeviceContext deviceContext,
            List<ProjectorCameraEnsemble.Camera> cameras,
            List<ProjectorCameraEnsemble.Projector> projectors,
            ProjectorCameraEnsemble.Projector projector,
            ProjectorDeviceObjects projectorDeviceObjects,
            VertexBufferBinding vertexBufferBinding,
            SharpDX.Direct3D11.Buffer indexBuffer,
            ShaderResourceView depthMapsSRV)
        {

            deviceContext.OutputMerger.SetTargets(projectorDeviceObjects.depthStencilView, projectorDeviceObjects.blendedProjectionRTV);
            deviceContext.ClearRenderTargetView(projectorDeviceObjects.blendedProjectionRTV, Color4.Black);
            deviceContext.ClearDepthStencilView(projectorDeviceObjects.depthStencilView, DepthStencilClearFlags.Depth, 1, 0);
            deviceContext.Rasterizer.SetViewport(projectorDeviceObjects.viewport);


            deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            deviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.VertexShader.SetConstantBuffer(0, vertexShaderConstantBuffer);
            deviceContext.PixelShader.Set(pixelShader);
            deviceContext.PixelShader.SetSampler(0, colorSamplerState);
            deviceContext.PixelShader.SetConstantBuffer(0, pixelShaderConstantBuffer);
            deviceContext.OutputMerger.DepthStencilState = depthStencilState;
            deviceContext.Rasterizer.State = rasterizerState;

            SetVertexShaderConstants(deviceContext, projector);

            // TODO: these do not need to be set every frame:
            SetPixelShaderConstants(deviceContext, projector, projectors);

            deviceContext.PixelShader.SetShaderResource(0, depthMapsSRV);
            


            int numVertices = cameras.Count * Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6;
            deviceContext.DrawIndexed(numVertices, 0, 0);

            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            deviceContext.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
            deviceContext.PixelShader.SetShaderResource(0, null);

        }


    }


}
