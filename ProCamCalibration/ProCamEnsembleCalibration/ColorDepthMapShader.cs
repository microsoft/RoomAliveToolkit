using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Device = SharpDX.Direct3D11.Device;

namespace RoomAliveToolkit
{
    public class ColorDepthMapShader : IDisposable
    {
        public ColorDepthMapShader(Device device, int numCameras)
        {
            var shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/ColorDepthMapVS.cso"));
            vertexShader = new VertexShader(device, shaderByteCode);
            pixelShader = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/ColorDepthMapPS.cso")));

            // depth buffer
            var depthBufferDesc = new Texture2DDescription()
            {
                Width = depthMapWidth,
                Height = depthMapHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None
            };
            depthStencil = new Texture2D(device, depthBufferDesc);





            depthStencilView = new DepthStencilView(device, depthStencil);

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
                SizeInBytes = 96,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            constantBuffer = new SharpDX.Direct3D11.Buffer(device, constantBufferDesc);

            // vertex layout
            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("pos", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            });

            // depth maps
            var depthMapsTextureDesc = new Texture2DDescription()
            {
                Width = depthMapWidth,
                Height = depthMapHeight,
                MipLevels = 1,
                ArraySize = numCameras,
                Format = SharpDX.DXGI.Format.R32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            depthMapsTexture = new Texture2D(device, depthMapsTextureDesc);

            depthMapRTVs = new RenderTargetView[numCameras];
            for (int i = 0; i < numCameras; i++)
            {
                var renderTargetViewDesc = new RenderTargetViewDescription()
                {
                    Format = Format.R32_Float,
                    Dimension = RenderTargetViewDimension.Texture2DArray,
                    Texture2DArray = new RenderTargetViewDescription.Texture2DArrayResource()
                    {
                        ArraySize = 1,
                        FirstArraySlice = i,
                        MipSlice = 0,
                    }
                };
                depthMapRTVs[i] = new RenderTargetView(device, depthMapsTexture, renderTargetViewDesc);
            }

            depthMapsSRV = new ShaderResourceView(device, depthMapsTexture);

            // viewport
            viewport = new Viewport(0, 0, depthMapWidth, depthMapHeight, 0f, 1f);
        }

        const int depthMapWidth = 1024;
        const int depthMapHeight = 768;

        VertexShader vertexShader;
        PixelShader pixelShader;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SharpDX.Direct3D11.Buffer constantBuffer;
        InputLayout vertexInputLayout;
        Texture2D depthMapsTexture, depthStencil;
        RenderTargetView[] depthMapRTVs;
        DepthStencilView depthStencilView;
        Viewport viewport;

        public ShaderResourceView depthMapsSRV;

        public void Dispose()
        {
            vertexShader.Dispose();
            pixelShader.Dispose();
            depthStencilState.Dispose();
            rasterizerState.Dispose();
            constantBuffer.Dispose();
            vertexInputLayout.Dispose();
            depthMapsTexture.Dispose();
            depthStencil.Dispose();
            foreach (var renderTargetView in depthMapRTVs)
                renderTargetView.Dispose();
        }

        public unsafe void SetConstants(DeviceContext deviceContext, ProjectorCameraEnsemble.Camera camera)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);

            var depthToWorld = camera.pose.ToSharp4x4();
            var depthToColor = camera.calibration.depthToColorTransform.ToSharp4x4();
            depthToWorld.Transpose();
            depthToColor.Transpose();
            var worldToDepth = depthToWorld;
            worldToDepth.Invert();
            var worldToColor = worldToDepth * depthToColor;

            // worldToColor
            dataStream.PackedWrite(worldToColor);

            // f
            dataStream.PackedWrite(new Vector2((float)camera.calibration.colorCameraMatrix[0, 0] / (float)Kinect2Calibration.colorImageWidth,
-(float)camera.calibration.colorCameraMatrix[1, 1] / (float)Kinect2Calibration.colorImageHeight));
            // c
            dataStream.PackedWrite(new Vector2((float)camera.calibration.colorCameraMatrix[0, 2] / (float)Kinect2Calibration.colorImageWidth,
                1f - (float)camera.calibration.colorCameraMatrix[1, 2] / (float)Kinect2Calibration.colorImageHeight));
            // k1
            dataStream.PackedWrite((float)camera.calibration.colorLensDistortion[0]);
            // k2
            dataStream.PackedWrite((float)camera.calibration.colorLensDistortion[1]);

            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        public void Render(DeviceContext deviceContext,
            List<ProjectorCameraEnsemble.Camera> cameras,
            VertexBufferBinding vertexBufferBinding,
            SharpDX.Direct3D11.Buffer indexBuffer)
        {
            deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            deviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            deviceContext.PixelShader.Set(pixelShader);
            deviceContext.OutputMerger.DepthStencilState = depthStencilState;
            deviceContext.Rasterizer.State = rasterizerState;
            deviceContext.Rasterizer.SetViewport(viewport);

            int renderTargeti = 0;
            foreach (var camera in cameras)
            {
                var depthMapRTV = depthMapRTVs[renderTargeti++];

                deviceContext.OutputMerger.SetTargets(depthStencilView, depthMapRTV);
                deviceContext.ClearRenderTargetView(depthMapRTV, Color.Black);
                deviceContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1, 0);



                SetConstants(deviceContext, camera);

                // draws all transformed vertices for all cameras
                deviceContext.DrawIndexed(Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6 * cameras.Count, 0, 0);
            }

            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            deviceContext.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
        }
    }
}
