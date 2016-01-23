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
    public class DepthMapShader : IDisposable
    {
        public DepthMapShader(Device device, int numProjectors)
        {
            var shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/DepthMapVS.cso"));
            vertexShader = new VertexShader(device, shaderByteCode);
            pixelShader = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthMapPS.cso")));

            // depth buffer
            var depthBufferDesc = new Texture2DDescription()
            {
                Width = depthMapWidth,
                Height = depthMapHeight,
                MipLevels = 1,
                ArraySize = numProjectors,
                Format = Format.R32_Typeless,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None
            };
            depthStencil = new Texture2D(device, depthBufferDesc);

            depthStencilViews = new DepthStencilView[numProjectors];
            for (int i = 0; i < numProjectors; i++)
            {
                // when using a typeless texture format, we have to set the format by view description
                var depthStencilViewDesc = new DepthStencilViewDescription()
                {
                    Format = Format.D32_Float,
                    Dimension = DepthStencilViewDimension.Texture2DArray,
                    Texture2DArray = new DepthStencilViewDescription.Texture2DArrayResource()
                    {
                        ArraySize = 1,
                        FirstArraySlice = i,
                        MipSlice = 0,
                    }
                };
                depthStencilViews[i] = new DepthStencilView(device, depthStencil, depthStencilViewDesc);
            }

            var shaderResourceViewDesc = new ShaderResourceViewDescription()
            {
                Format = Format.R32_Float,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DArray,
                Texture2DArray = new ShaderResourceViewDescription.Texture2DArrayResource()
                {
                    ArraySize = numProjectors,
                    FirstArraySlice = 0,
                    MipLevels = -1,
                    MostDetailedMip = 0,
                }
            };
            depthStencilView2 = new ShaderResourceView(device, depthStencil, shaderResourceViewDesc);

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
                SizeInBytes = ConstantBuffer.size,
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
                ArraySize = numProjectors,
                Format = SharpDX.DXGI.Format.R32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            depthMapsTexture = new Texture2D(device, depthMapsTextureDesc);

            depthMapRTVs = new RenderTargetView[numProjectors];
            for (int i = 0; i < numProjectors; i++)
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
        DepthStencilView[] depthStencilViews;
        Viewport viewport;

        public ShaderResourceView depthMapsSRV, depthStencilView2;

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

            // TOOD: dispose depthstencil views
        }

        [StructLayout(LayoutKind.Explicit, Size = ConstantBuffer.size)]
        unsafe struct ConstantBuffer
        {
            public const int size = 64;

            [FieldOffset(0)]
            public fixed float projection[16];
        };

        public unsafe void SetConstants(DeviceContext deviceContext, SharpDX.Matrix projection)
        {
            // hlsl matrices are default column order
            var constants = new ConstantBuffer();
            for (int i = 0, col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                    constants.projection[i++] = projection[row, col];

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<ConstantBuffer>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        public void Render(DeviceContext deviceContext,
            List<ProjectorCameraEnsemble.Projector> projectors,
            int numCameras,
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
            foreach (var projector in projectors)
            {
                var depthMapRTV = depthMapRTVs[renderTargeti++];

                int projectori = projectors.IndexOf(projector);

                deviceContext.OutputMerger.SetTargets(depthStencilViews[projectori], depthMapRTV);
                deviceContext.ClearRenderTargetView(depthMapRTV, Color.Black);
                deviceContext.ClearDepthStencilView(depthStencilViews[projectori], DepthStencilClearFlags.Depth, 1, 0);

                var view = new SharpDX.Matrix();
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 4; j++)
                        view[i, j] = (float)projector.pose[i, j];
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

                SetConstants(deviceContext, viewProjection);

                // draws all transformed vertices for all cameras
                deviceContext.DrawIndexed(Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6 * numCameras, 0, 0);
            }

            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            deviceContext.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
        }
    }
}
