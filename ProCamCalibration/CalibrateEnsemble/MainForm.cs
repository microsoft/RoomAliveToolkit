using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceModel.Discovery;
using System.Windows.Forms;

namespace RoomAliveToolkit
{
    public partial class MainForm : Form
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }

        public MainForm(string[] args)
        {
            InitializeComponent();
            this.args = args;
        }

        ProjectorCameraEnsemble ensemble;
        string path, directory;
        string[] args;
        bool unsavedChanges = false;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // create d3d device and shaders
            var swapChainDesc = new SwapChainDescription
            {
                BufferCount = 1,
                Usage = Usage.RenderTargetOutput,
                OutputHandle = videoPanel1.Handle, // splitContainer1.Panel1.Handle,
                IsWindowed = true,
                ModeDescription = new ModeDescription(0, 0, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                Flags = SwapChainFlags.AllowModeSwitch,
                SwapEffect = SwapEffect.Discard,
                SampleDescription = new SampleDescription(1, 0),
            };

            // When using DeviceCreationFlags.Debug on Windows 10, ensure that "Graphics Tools" are installed via Settings/System/Apps & features/Manage optional features.
            // Also, when debugging in VS, "Enable native code debugging" must be selected on the project.
            SharpDX.Direct3D11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug, swapChainDesc, out device, out swapChain);

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

            // shaders
            depthAndColorShader = new DepthAndColorShader(device);
            depthToWorldCoordinateShader = new DepthToWorldCoordinateShader(device);

            manipulator = new Manipulator(videoPanel1);

            // disable most menu items when no file is loaded
            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            reloadToolStripMenuItem.Enabled = false;
            calibrateToolStripMenuItem.Enabled = false;
            renderToolStripMenuItem.Enabled = false;
            viewToolStripMenuItem.Enabled = false;
            displayProjectorDisplayIndexesToolStripMenuItem.Enabled = false;
            displayProjectorNamesToolStripMenuItem.Enabled = false;

            if (args.Length > 0)
            {
                path = args[0];
                directory = Path.GetDirectoryName(path);
                LoadEnsemble();
            }

            // TODO: sanity check these?
            Width = Properties.Settings.Default.FormWidth;
            Height = Properties.Settings.Default.FormHeight;
            splitContainer1.SplitterDistance = Properties.Settings.Default.SplitterDistance;

            new System.Threading.Thread(RenderLoop).Start();
        }

        SharpDX.Direct3D11.Device device;
        SwapChain swapChain;
        Texture2D renderTarget, depthStencil;
        RenderTargetView renderTargetView;
        DepthStencilView depthStencilView;
        Viewport viewport;
        DepthAndColorShader depthAndColorShader;
        DepthToWorldCoordinateShader depthToWorldCoordinateShader;
        SharpDX.Direct3D11.Buffer vertexBuffer, indexBuffer;
        VertexBufferBinding vertexBufferBinding;

        Manipulator manipulator;
        Object renderLock = new Object();
        FrameRate frameRate = new FrameRate(2000);

        void RenderLoop()
        {
            while (true)
            {
                lock (renderLock) // TODO: probably coarser than it needs to be
                {
                    view = manipulator.Update();

                    var deviceContext = device.ImmediateContext;

                    // fill our vertex/index buffer with world coordinate transformed vertices over all cameras
                    deviceContext.ComputeShader.Set(depthToWorldCoordinateShader.computeShader);
                    deviceContext.ComputeShader.SetConstantBuffer(0, depthToWorldCoordinateShader.constantBuffer);

                    if (ensemble != null)
                        foreach (var camera in ensemble.cameras)
                        {
                            // TODO: this should be done only if the corresponding depth image or world transform has changed

                            if (cameraDeviceResources.ContainsKey(camera))
                            {
                                var world = new SharpDX.Matrix();
                                for (int i = 0; i < 4; i++)
                                    for (int j = 0; j < 4; j++)
                                        world[i, j] = (float)camera.pose[i, j];
                                world.Transpose();

                                // offset adjusts index value to point to this camera's vertices
                                int cameraIndex = cameraDeviceResources[camera].cameraIndex;
                                int offset = cameraIndex * Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight;
                                depthToWorldCoordinateShader.SetConstants(deviceContext, world, (uint)offset);

                                deviceContext.ComputeShader.SetShaderResource(0, cameraDeviceResources[camera].depthImageTextureRV);
                                deviceContext.ComputeShader.SetShaderResource(1, cameraDeviceResources[camera].depthFrameToCameraSpaceTableTextureRV);
                                deviceContext.ComputeShader.SetUnorderedAccessView(0, cameraDeviceResources[camera].vertexBufferUAV);
                                deviceContext.ComputeShader.SetUnorderedAccessView(1, cameraDeviceResources[camera].indexBufferUAV);

                                deviceContext.Dispatch(16, 22, 1);
                            }
                        }
                    deviceContext.ComputeShader.SetShaderResource(0, null);
                    deviceContext.ComputeShader.SetShaderResource(1, null);
                    deviceContext.ComputeShader.SetUnorderedAccessView(0, null);
                    deviceContext.ComputeShader.SetUnorderedAccessView(1, null);
                    deviceContext.ComputeShader.Set(null);


                    // texture mapped view for UI
                    deviceContext.InputAssembler.InputLayout = depthAndColorShader.vertexInputLayout;
                    deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
                    deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
                    deviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
                    deviceContext.VertexShader.Set(depthAndColorShader.vertexShader);
                    deviceContext.VertexShader.SetConstantBuffer(0, depthAndColorShader.constantBuffer);
                    deviceContext.PixelShader.Set(depthAndColorShader.pixelShader);
                    deviceContext.PixelShader.SetSampler(0, depthAndColorShader.colorSamplerState);
                    deviceContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
                    deviceContext.OutputMerger.DepthStencilState = depthAndColorShader.depthStencilState;
                    deviceContext.Rasterizer.State = depthAndColorShader.rasterizerState;
                    deviceContext.Rasterizer.SetViewport(viewport);
                    deviceContext.ClearRenderTargetView(renderTargetView, Color4.Black);
                    deviceContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1, 0);

                    // view and projection matrix are post-multiply
                    var worldViewProjection = view * projection;

                    // render all cameras
                    if (ensemble != null)
                        foreach (var camera in ensemble.cameras)
                        {
                            // while we have a single vertex buffer, we need separate draw calls here for each camera because we 
                            // are texture mapping each set of depth camera vertices with the corresponding color camera image
                            if (cameraDeviceResources.ContainsKey(camera))
                                if (cameraDeviceResources[camera].renderEnabled && (camera.pose != null))
                                {
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

                                    depthAndColorShader.SetConstants(deviceContext, camera.calibration, worldToColor, worldViewProjection);

                                    var camerDeviceResource = cameraDeviceResources[camera];

                                    deviceContext.VertexShader.SetShaderResource(0, camerDeviceResource.depthImageTextureRV);
                                    deviceContext.VertexShader.SetShaderResource(1, camerDeviceResource.depthFrameToCameraSpaceTableTextureRV);
                                    deviceContext.PixelShader.SetShaderResource(0, camerDeviceResource.colorImageTextureRV);
                                    int numVertices = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6;
                                    deviceContext.DrawIndexed(numVertices, numVertices * camerDeviceResource.cameraIndex, 0);
                                }
                        }

                    deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
                    deviceContext.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);



                    swapChain.Present(0, PresentFlags.None);

                    frameRate.Tick();
                    frameRate.PrintMessage();
                }
            }
        }

        SharpDX.Matrix view, projection;

        Dictionary<ProjectorCameraEnsemble.Camera, CameraDeviceResource> cameraDeviceResources = new Dictionary<ProjectorCameraEnsemble.Camera, CameraDeviceResource>();

        void EnsembleChanged()
        {
            lock (renderLock)
            {
                // dispose/create device resources that depend on number of cameras; do this first, as camera device resources will depend on them

                if (vertexBuffer != null)
                    vertexBuffer.Dispose();
                var vertexBufferDesc = new BufferDescription()
                {
                    BindFlags = BindFlags.UnorderedAccess | BindFlags.VertexBuffer,
                    OptionFlags = ResourceOptionFlags.BufferAllowRawViews,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Usage = ResourceUsage.Default,
                    SizeInBytes = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 3 * 4 * ensemble.cameras.Count,
                };
                vertexBuffer = new SharpDX.Direct3D11.Buffer(device, vertexBufferDesc);
                vertexBufferBinding = new VertexBufferBinding(vertexBuffer, 3 * 4, 0);

                if (indexBuffer != null)
                    indexBuffer.Dispose();
                var indexBufferDesc = new BufferDescription()
                {
                    BindFlags = BindFlags.UnorderedAccess | BindFlags.IndexBuffer,
                    OptionFlags = ResourceOptionFlags.BufferAllowRawViews,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Usage = ResourceUsage.Default,
                    SizeInBytes = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6 * 4 * ensemble.cameras.Count,
                };
                indexBuffer = new SharpDX.Direct3D11.Buffer(device, indexBufferDesc);


                // deallocate/allocate camera d3d resources
                foreach (var cameraDeviceResource in cameraDeviceResources.Values)
                    cameraDeviceResource.Dispose();
                cameraDeviceResources.Clear();


                foreach (var camera in ensemble.cameras)
                {
                    int cameraIndex = ensemble.cameras.IndexOf(camera);

                    if (camera.calibration != null) // TODO: this might not be the right way to check
                        cameraDeviceResources[camera] = new CameraDeviceResource(device, camera, cameraIndex, renderLock, directory, vertexBuffer, indexBuffer);
                }
            }

            Invoke((Action)delegate
            {
                // build Render menu
                foreach (var menuItem in cameraMenuItems)
                    renderToolStripMenuItem.DropDownItems.Remove(menuItem);
                foreach (var camera in ensemble.cameras)
                {
                    var toolStripMenuItem = new ToolStripMenuItem("Camera " + camera.name, null, renderMenuItem_Click);
                    toolStripMenuItem.Tag = camera;
                    toolStripMenuItem.Checked = true;
                    renderToolStripMenuItem.DropDownItems.Add(toolStripMenuItem);
                    cameraMenuItems.Add(toolStripMenuItem);
                }

                // build View menu
                foreach (var menuItem in projectorMenuItems)
                    viewToolStripMenuItem.DropDownItems.Remove(menuItem);
                foreach (var projector in ensemble.projectors)
                {
                    var toolStripMenuItem = new ToolStripMenuItem("Projector " + projector.name, null, viewMenuItem_Click);
                    toolStripMenuItem.Tag = projector;
                    toolStripMenuItem.Checked = false;
                    viewToolStripMenuItem.DropDownItems.Add(toolStripMenuItem);
                    projectorMenuItems.Add(toolStripMenuItem);
                }

                SetDefaultView();

                // we have a file loaded, so enable menu items
                saveToolStripMenuItem.Enabled = true;
                saveAsToolStripMenuItem.Enabled = true;
                reloadToolStripMenuItem.Enabled = true;
                calibrateToolStripMenuItem.Enabled = true;
                renderToolStripMenuItem.Enabled = true;
                viewToolStripMenuItem.Enabled = true;
                displayProjectorDisplayIndexesToolStripMenuItem.Enabled = true;
                displayProjectorNamesToolStripMenuItem.Enabled = true;

                Text = Path.GetFileName(path) + " - CalibrateEnsemble";
            });

        }

        List<ToolStripMenuItem> cameraMenuItems = new List<ToolStripMenuItem>();
        List<ToolStripMenuItem> projectorMenuItems = new List<ToolStripMenuItem>();


        #region Form event handlers

        void renderMenuItem_Click(object sender, EventArgs e)
        {
            var toolStripMenuItem = (ToolStripMenuItem)sender;
            var camera = (ProjectorCameraEnsemble.Camera)toolStripMenuItem.Tag;
            toolStripMenuItem.Checked = !toolStripMenuItem.Checked;
            cameraDeviceResources[camera].renderEnabled = toolStripMenuItem.Checked;
        }

        bool perspectiveView;

        private void perspectiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var menuItem in projectorMenuItems)
                menuItem.Checked = false;
            SetDefaultView();
        }

        void viewMenuItem_Click(object sender, EventArgs e)
        {
            var toolStripMenuItem = (ToolStripMenuItem)sender;

            perspectiveAtOriginToolStripMenuItem.Checked = false;
            foreach (var menuItem in projectorMenuItems)
                menuItem.Checked = false;

            toolStripMenuItem.Checked = true;

            var projector = (ProjectorCameraEnsemble.Projector)toolStripMenuItem.Tag;
            SetViewProjectionFromProjector(projector);
            manipulator.View = view;
            manipulator.OriginalView = view;
            perspectiveView = false;
        }

        private void acquireToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateToolStripMenuItem.Enabled = false;
            new System.Threading.Thread(Acquire).Start();
        }

        private void solveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateToolStripMenuItem.Enabled = false;
            new System.Threading.Thread(Solve).Start();
        }

        private void acquireDepthAndColorOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateToolStripMenuItem.Enabled = false;
            new System.Threading.Thread(AcquireDepthAndColor).Start();
        }

        private void decodeGrayCodesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateToolStripMenuItem.Enabled = false;
            new System.Threading.Thread(DecodeGrayCodes).Start();
        }

        private void calibrateProjectorGroupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateToolStripMenuItem.Enabled = false;
            new System.Threading.Thread(CalibrateProjectorGroups).Start();
        }

        private void optimizePoseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            calibrateToolStripMenuItem.Enabled = false;
            new System.Threading.Thread(OptimizePose).Start();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var newDialog = new NewDialog();
            if (newDialog.ShowDialog(this) == DialogResult.OK)
            {
                var newEnsemble = new ProjectorCameraEnsemble(newDialog.NumProjectors, newDialog.NumCameras);

                var saveFileDialog = new SaveFileDialog();

                saveFileDialog.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 0;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        path = saveFileDialog.FileName;
                        directory = Path.GetDirectoryName(path);
                        newEnsemble.Save(path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Could not save file to disk.\n" + ex);
                        return;
                    }
                    unsavedChanges = false;
                    lock (renderLock)
                    {
                        ensemble = newEnsemble;
                        EnsembleChanged();
                    }
                }
            }
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadEnsemble();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                ensemble.Save(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not save file to disk.\n" + ex);
                return;
            }
            unsavedChanges = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                path = openFileDialog.FileName;
                directory = Path.GetDirectoryName(path);
                LoadEnsemble();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 0;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    path = saveFileDialog.FileName;
                    directory = Path.GetDirectoryName(path);
                    ensemble.Save(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not save file to disk.\n" + ex);
                    return;
                }
                unsavedChanges = false;
            }
        }

        private void saveToOBJToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "obj files (*.obj)|*.obj|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 0;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ensemble.SaveToOBJ(directory, saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not save file to disk.\n" + ex);
                }
            }
        }

        private void discoverCamerasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new System.Threading.Thread(DiscoverCameras).Start();
        }

        private void discoverProjectorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new System.Threading.Thread(DiscoverProjectors).Start();
        }

        private void displayProjectorDisplayIndexesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (showingDisplayIndexes)
            {
                try
                {
                    HideDisplayIndexes();
                    displayProjectorDisplayIndexesToolStripMenuItem.Text = "Show Projector Server Connected Displays";
                }
                catch (Exception)
                {
                    Console.WriteLine("HideDisplayIndexes failed");
                }
            }
            else
            {
                try
                {
                    ShowDisplayIndexes();
                    displayProjectorDisplayIndexesToolStripMenuItem.Text = "Hide Projector Server Connected Displays";
                }
                catch (Exception)
                {
                    Console.WriteLine("ShowDisplayIndexes failed");
                }
            }
        }

        private void displayProjectorNamesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (showingDisplayNames)
            {
                try
                {
                    HideDisplayNames();
                    displayProjectorNamesToolStripMenuItem.Text = "Show Projector Names";
                }
                catch (Exception)
                {
                    Console.WriteLine("HideProjectorNames failed");
                }
            }
            else
            {
                try
                {
                    ShowDisplayNames();
                    displayProjectorNamesToolStripMenuItem.Text = "Hide Projector Names";
                }
                catch (Exception)
                {
                    Console.WriteLine("ShowProjectorNames failed");
                }
            }
        }

        bool live = false;

        private void liveViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            live = !live;
            if (live)
                foreach (var cameraDeviceResource in cameraDeviceResources.Values)
                    cameraDeviceResource.StartLive();
            else
                foreach (var cameraDeviceResource in cameraDeviceResources.Values)
                    cameraDeviceResource.StopLive();
            liveViewToolStripMenuItem.Checked = live;
        }

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

                    int newWidth = videoPanel1.Width;
                    int newHeight = videoPanel1.Height;
                    if (newWidth < 8)
                        newWidth = 8;
                    if (newHeight < 8)
                        newHeight = 8;

                    swapChain.ResizeBuffers(1, newWidth, newHeight, Format.Unknown, SwapChainFlags.AllowModeSwitch);

                    renderTarget = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
                    renderTargetView = new RenderTargetView(device, renderTarget);

                    // depth buffer
                    var depthBufferDesc = new Texture2DDescription()
                    {
                        Width = newWidth,
                        Height = newHeight,
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
                    viewport = new Viewport(0, 0, newWidth, newHeight, 0f, 1f);
                    manipulator.Viewport = viewport;

                    // in the case of perspective projection, change projection to follow change in aspect
                    if (perspectiveView)
                    {
                        //float aspect = (float)videoPanel1.Width / (float)videoPanel1.Height;
                        //projection = GraphicsTransforms.PerspectiveFov(35.0f / 180.0f * (float)Math.PI, aspect, 0.1f, 100.0f);
                        //projection.Transpose();
                        SetDefaultView();
                    }
                }
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Exit())
                e.Cancel = true;
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }

        void SaveSettings()
        {
            Properties.Settings.Default.FormWidth = Width;
            Properties.Settings.Default.FormHeight = Height;
            Properties.Settings.Default.SplitterDistance = splitContainer1.SplitterDistance;
            Properties.Settings.Default.Save();
        }

        bool Exit()
        {
            if (unsavedChanges && (ensemble != null))
            {
                var unsavedChangesDialog = new UnsavedChangesDialog(Path.GetFileNameWithoutExtension(path));
                var result = unsavedChangesDialog.ShowDialog();
                switch (result)
                {
                    case DialogResult.Yes: // save
                        try
                        {
                            ensemble.Save(path);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Could not save file to disk.\n" + ex);
                            return false;
                        }
                        break;
                    case DialogResult.No: // don't save
                        break;
                    case DialogResult.Cancel:
                        return false;
                }
            }
            SaveSettings();
            Environment.Exit(0);
            return true;
        }
        #endregion



        // these probably should be methods on ProjectorCameraEnsemble

        void DiscoverCameras()
        {
            var discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
            var findCriteria = new FindCriteria(typeof(KinectServer2));
            findCriteria.Duration = new TimeSpan(0, 0, 2);
            Console.WriteLine("finding Kinect servers...");
            var services = discoveryClient.Find(findCriteria);
            discoveryClient.Close();
            Console.WriteLine("found {0} servers", services.Endpoints.Count);
            foreach (var endPoint in services.Endpoints)
                Console.WriteLine(endPoint.Address);
        }

        void DiscoverProjectors()
        {
            var discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
            var findCriteria = new FindCriteria(typeof(ProjectorServer));
            findCriteria.Duration = new TimeSpan(0, 0, 2);
            Console.WriteLine("finding projector servers...");
            var services = discoveryClient.Find(findCriteria);
            discoveryClient.Close();
            Console.WriteLine("found {0} servers", services.Endpoints.Count);
            foreach (var endPoint in services.Endpoints)
                Console.WriteLine(endPoint.Address);
        }


        bool showingDisplayIndexes = false;

        void ShowDisplayIndexes()
        {
            foreach (var projector in ensemble.projectors)
            {
                int screenCount = projector.Client.ScreenCount();
                for (int i = 0; i < screenCount; i++)
                {
                    projector.Client.OpenDisplay(i);
                    projector.Client.DisplayName(i, projector.hostNameOrAddress + ":" + i);
                }
            }
            showingDisplayIndexes = true;
        }

        void HideDisplayIndexes()
        {
            foreach (var projector in ensemble.projectors)
            {
                int screenCount = projector.Client.ScreenCount();
                for (int i = 0; i < screenCount; i++)
                    projector.Client.CloseDisplay(i);
            }
            showingDisplayIndexes = false;
        }

        bool showingDisplayNames = false;

        void ShowDisplayNames()
        {
            foreach (var projector in ensemble.projectors)
            {
                projector.Client.OpenDisplay(projector.displayIndex);
                projector.Client.DisplayName(projector.displayIndex, projector.name);
            }
            showingDisplayNames = true;
        }

        void HideDisplayNames()
        {
            foreach (var projector in ensemble.projectors)
                projector.Client.CloseDisplay(projector.displayIndex);
            showingDisplayNames = false;
        }

        void LoadEnsemble()
        {
            lock (renderLock)
            {
                try
                {
                    ensemble = ProjectorCameraEnsemble.FromFile(path);
                    Console.WriteLine("Loaded " + path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not read file from disk.\n" + ex);
                    return;
                }
                EnsembleChanged();
                unsavedChanges = false;
            }
        }

        void Acquire()
        {
            try
            {
                ensemble.CaptureGrayCodes(directory);
                EnsembleChanged();
            }
            catch (Exception e)
            {
                Console.WriteLine("Acquire failed\n" + e);
            }
            Console.WriteLine("Acquire complete");
            unsavedChanges = true;
            Invoke((Action)delegate { calibrateToolStripMenuItem.Enabled = true; });
        }


        void Solve()
        {
            ensemble.DecodeGrayCodeImages(directory);
            try
            {
                ensemble.CalibrateProjectorGroups(directory);
                ensemble.OptimizePose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Solve failed\n" + e);
            }
            Console.WriteLine("Solve complete");
            unsavedChanges = true;
            Invoke((Action)delegate { calibrateToolStripMenuItem.Enabled = true; });
        }

        void DecodeGrayCodes()
        {
            ensemble.DecodeGrayCodeImages(directory);
            Invoke((Action)delegate { calibrateToolStripMenuItem.Enabled = true; });
        }

        void CalibrateProjectorGroups()
        {
            try
            {
                ensemble.CalibrateProjectorGroups(directory);
            }
            catch (Exception e)
            {
                Console.WriteLine("Solve failed\n" + e);
            }
            Console.WriteLine("Solve complete");
            unsavedChanges = true;
            Invoke((Action)delegate { calibrateToolStripMenuItem.Enabled = true; });
        }

        void OptimizePose()
        {
            try
            {
                //TODO: not sure if this works if just loaded from file; since UnifyPose is the first step
                // and the various pose members may not be set
                ensemble.OptimizePose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Solve failed\n" + e);
            }
            Console.WriteLine("Solve complete");
            unsavedChanges = true;
            Invoke((Action)delegate { calibrateToolStripMenuItem.Enabled = true; });
        }

        void AcquireDepthAndColor()
        {
            try
            {
                ensemble.CaptureDepthAndColor(directory);
                EnsembleChanged();
            }
            catch (Exception e)
            {
                Console.WriteLine("Acquire Depth and Color failed\n" + e);
            }
            Console.WriteLine("Acquire Depth and Color complete");
            Invoke((Action)delegate { calibrateToolStripMenuItem.Enabled = true; });
        }


        // could be method on Projector:
        void SetViewProjectionFromProjector(ProjectorCameraEnsemble.Projector projector)
        {
            if ((projector.pose == null) || (projector.cameraMatrix == null))
                Console.WriteLine("Projector pose/camera matrix not set. Please perform a calibration.");
            else
            {
                // pick up view and projection for a given projector
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
        }

        void SetViewProjectionFromCamera(ProjectorCameraEnsemble.Camera camera)
        {
            if ((camera.pose == null) || (camera.calibration.colorCameraMatrix == null))
                Console.WriteLine("Camera pose not set.");
            else
            {
                view = new SharpDX.Matrix();
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 4; j++)
                        view[i, j] = (float)camera.pose[i, j];
                view.Invert();
                view.Transpose();

                var cameraMatrix = camera.calibration.colorCameraMatrix;
                float fx = (float)cameraMatrix[0, 0];
                float fy = (float)cameraMatrix[1, 1];
                float cx = (float)cameraMatrix[0, 2];
                float cy = (float)cameraMatrix[1, 2];

                float near = 0.1f;
                float far = 100.0f;

                float w = Kinect2Calibration.colorImageWidth;
                float h = Kinect2Calibration.colorImageHeight;

                projection = GraphicsTransforms.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
                projection.Transpose();
            }
        }

        void SetDefaultView()
        {
            view = SharpDX.Matrix.Identity;
            float aspect = (float)videoPanel1.Width / (float)videoPanel1.Height;
            projection = GraphicsTransforms.PerspectiveFov(35.0f / 180.0f * (float)Math.PI, aspect, 0.1f, 100.0f);
            projection.Transpose();

            manipulator.View = view;
            manipulator.Projection = projection;
            manipulator.Viewport = viewport;
            manipulator.OriginalView = view;
            perspectiveAtOriginToolStripMenuItem.Checked = true;
            perspectiveView = true;
        }

    }
}
