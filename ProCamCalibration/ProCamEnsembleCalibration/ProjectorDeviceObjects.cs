using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;

namespace RoomAliveToolkit
{
    public class ProjectorDeviceObjects
    {
        public ProjectorDeviceObjects(SharpDX.Direct3D11.Device device, ProjectorCameraEnsemble.Projector projector)
        {
            var blendedProjectionDesc = new Texture2DDescription()
            {
                Width = projector.width,
                Height = projector.height,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
            };
            blendedProjection = new Texture2D(device, blendedProjectionDesc);
            blendedProjectionRV = new ShaderResourceView(device, blendedProjection);
            blendedProjectionRTV = new RenderTargetView(device, blendedProjection);

            // each gets its own depth buffer, since not all projectors may have the same dimensions
            var depthBufferDesc = new Texture2DDescription()
            {
                Width = projector.width,
                Height = projector.height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D32_Float, // necessary?
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None
            };
            depthStencil = new Texture2D(device, depthBufferDesc);
            depthStencilView = new DepthStencilView(device, depthStencil);

            // viewport
            viewport = new Viewport(0, 0, projector.width, projector.height, 0f, 1f);
        }

        public Texture2D blendedProjection, depthStencil;
        public ShaderResourceView blendedProjectionRV;
        public RenderTargetView blendedProjectionRTV;
        public Viewport viewport;
        public DepthStencilView depthStencilView;
    }
}
