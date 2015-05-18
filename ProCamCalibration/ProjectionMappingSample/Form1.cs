using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace RoomAliveToolkit
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public Form1(Factory factory, SharpDX.Direct3D11.Device device, Object renderLock)
        {
            InitializeComponent();
            this.factory = factory;
            this.device = device;
            this.renderLock = renderLock;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode)
                return;

            // create swap chain, rendertarget
            var swapChainDesc = new SwapChainDescription()
            {
                BufferCount = 1,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = videoPanel1.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard,
                SampleDescription = new SampleDescription(1, 0),
            };

            swapChain = new SwapChain(factory, device, swapChainDesc);

            // render target
            renderTarget = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderTargetView = new RenderTargetView(device, renderTarget);

            // depth buffer
            var depthBufferDesc = new Texture2DDescription()
            {
                Width = videoPanel1.Width,
                Height = videoPanel1.Height,
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
            viewport = new Viewport(0, 0, videoPanel1.Width, videoPanel1.Height, 0f, 1f);
        }

        SharpDX.Direct3D11.Device device;
        Factory factory;
        Texture2D renderTarget, depthStencil;
        public RenderTargetView renderTargetView;
        public DepthStencilView depthStencilView;
        public Viewport viewport;
        public SwapChain swapChain;
        Object renderLock;

        private void videoPanel1_SizeChanged(object sender, EventArgs e)
        {
            // TODO: look into using this as initial creation
            if (renderTargetView != null)
                lock (renderLock)
                {
                    renderTargetView.Dispose();
                    renderTarget.Dispose();

                    depthStencilView.Dispose();
                    depthStencil.Dispose();

                    swapChain.ResizeBuffers(1, videoPanel1.Width, videoPanel1.Height, Format.Unknown, SwapChainFlags.AllowModeSwitch);

                    renderTarget = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
                    renderTargetView = new RenderTargetView(device, renderTarget);

                    // depth buffer
                    var depthBufferDesc = new Texture2DDescription()
                    {
                        Width = videoPanel1.Width,
                        Height = videoPanel1.Height,
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
                    viewport = new Viewport(0, 0, videoPanel1.Width, videoPanel1.Height, 0f, 1f);
                }
        }
    }

    public class ProjectorForm : Form1
    {
        public ProjectorForm(Factory factory, SharpDX.Direct3D11.Device device, Object renderLock, ProjectorCameraEnsemble.Projector projector) : base(factory, device, renderLock)
        {
            this.projector = projector;
            Text = "Projector " + projector.name;
        }

        public bool FullScreen
        {
            get { return fullScreen; }
            set 
            {
                if (value)
                {
                    // switch to fullscreen
                    ShowInTaskbar = false;
                    FormBorderStyle = FormBorderStyle.None;
                    var bounds = Screen.AllScreens[projector.displayIndex].Bounds; // TODO: catch the case where the display is not available
                    StartPosition = FormStartPosition.Manual;
                    Location = new System.Drawing.Point(bounds.X, bounds.Y);
                    Size = new Size(bounds.Width, bounds.Height);
                }
                else
                {
                    // switch to windowed
                    ShowInTaskbar = true;
                    FormBorderStyle = FormBorderStyle.Sizable;
                    Location = windowedLocation;
                    Size = windowedSize;
                }
            }
        }

        bool fullScreen = false;
        System.Drawing.Point windowedLocation;
        Size windowedSize;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // cache location etc for when we come out of fullscreen
            windowedLocation = Location;
            windowedSize = Size;

            // pick up view and projection for projector
            view = new SharpDX.Matrix();
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
            float far = 100.0f;

            float w = projector.width;
            float h = projector.height;

            projection = GraphicsTransforms.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
            projection.Transpose();
        }

        ProjectorCameraEnsemble.Projector projector;
        public SharpDX.Matrix view, projection;

    }


}
 