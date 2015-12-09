using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.Kinect;
using System.Runtime.InteropServices;

namespace RoomAliveToolkit
{
    public class ProjectionMappingSample : ApplicationContext
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Run(new ProjectionMappingSample(args));
        }

        public ProjectionMappingSample(string[] args)
        {
            // load ensemble.xml
            string path = args[0];
            string directory = Path.GetDirectoryName(path);
            ensemble = RoomAliveToolkit.ProjectorCameraEnsemble.FromFile(path);

            // create d3d device
            var factory = new Factory1();
            var adapter = factory.Adapters[0];

            // When using DeviceCreationFlags.Debug on Windows 10, ensure that "Graphics Tools" are installed via Settings/System/Apps & features/Manage optional features.
            // Also, when debugging in VS, "Enable native code debugging" must be selected on the project.
            device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.None);

            // shaders
            depthAndColorShader = new DepthAndColorShader(device);
            projectiveTexturingShader = new ProjectiveTexturingShader(device);
            passThroughShader = new PassThrough(device, userViewTextureWidth, userViewTextureHeight);
            radialWobbleShader = new RadialWobble(device, userViewTextureWidth, userViewTextureHeight);
            meshShader = new MeshShader(device);
            fromUIntPS = new FromUIntPS(device, Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);
            bilateralFilter = new BilateralFilter(device, Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);

            // create device objects for each camera
            foreach (var camera in ensemble.cameras)
            {
                int cameraIndex = ensemble.cameras.IndexOf(camera);

                cameraDeviceResources[camera] = new CameraDeviceResource(device, camera, cameraIndex, renderLock, directory);
            }

            // one user view
            // user view render target, depth buffer, viewport for user view
            var userViewTextureDesc = new Texture2DDescription()
            {
                Width = userViewTextureWidth,
                Height = userViewTextureHeight,
                MipLevels = 1, // revisit this; we may benefit from mipmapping?
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            var userViewRenderTarget = new Texture2D(device, userViewTextureDesc);
            userViewRenderTargetView = new RenderTargetView(device, userViewRenderTarget);
            userViewSRV = new ShaderResourceView(device, userViewRenderTarget);

            var filteredUserViewRenderTarget = new Texture2D(device, userViewTextureDesc);
            filteredUserViewRenderTargetView = new RenderTargetView(device, filteredUserViewRenderTarget);
            filteredUserViewSRV = new ShaderResourceView(device, filteredUserViewRenderTarget);

            // user view depth buffer
            var userViewDpethBufferDesc = new Texture2DDescription()
            {
                Width = userViewTextureWidth,
                Height = userViewTextureHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.D32_Float, // necessary?
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None
            };
            var userViewDepthStencil = new Texture2D(device, userViewDpethBufferDesc);
            userViewDepthStencilView = new DepthStencilView(device, userViewDepthStencil);

            // user view viewport
            userViewViewport = new Viewport(0, 0, userViewTextureWidth, userViewTextureHeight, 0f, 1f);

            // create a form for each projector
            foreach (var projector in ensemble.projectors)
            {
                var form = new ProjectorForm(factory, device, renderLock, projector);
                if (fullScreenEnabled)
                    form.FullScreen = fullScreenEnabled; // TODO: fix this so can be called after Show
                form.Show();
                projectorForms.Add(form);
            }

            // example 3d object
            var mesh = Mesh.FromOBJFile("Content/FloorPlan.obj");
            meshDeviceResources = new MeshDeviceResources(device, imagingFactory, mesh);

            // desktop duplication
            var output = new Output1(factory.Adapters[0].Outputs[0].NativePointer); // TODO: change adapter and output number
            outputDuplication = output.DuplicateOutput(device);


            userViewForm = new Form1(factory, device, renderLock);
            userViewForm.Text = "User View";
            userViewForm.Show();


            userViewForm.videoPanel1.MouseClick += videoPanel1_MouseClick;

            // connect to local camera to acquire head position
            if (localHeadTrackingEnabled)
            {
                localKinectSensor = KinectSensor.GetDefault();
                bodyFrameReader = localKinectSensor.BodyFrameSource.OpenReader();
                localKinectSensor.Open();
                Console.WriteLine("connected to local camera");
                new System.Threading.Thread(LocalBodyLoop).Start();
            }


            if (liveDepthEnabled)
            {
                foreach (var cameraDeviceResource in cameraDeviceResources.Values)
                    cameraDeviceResource.StartLive();
            }


            new System.Threading.Thread(RenderLoop).Start();
        }

        void videoPanel1_MouseClick(object sender, MouseEventArgs e)
        {
            alpha = 0;
        }


        OutputDuplication outputDuplication;


        const int userViewTextureWidth = 2000;
        const int userViewTextureHeight = 1000;
        List<ProjectorForm> projectorForms = new List<ProjectorForm>();
        DepthAndColorShader depthAndColorShader;
        ProjectiveTexturingShader projectiveTexturingShader;
        Dictionary<ProjectorCameraEnsemble.Camera, CameraDeviceResource> cameraDeviceResources = new Dictionary<ProjectorCameraEnsemble.Camera, CameraDeviceResource>();
        Object renderLock = new Object();
        RenderTargetView userViewRenderTargetView, filteredUserViewRenderTargetView;
        DepthStencilView userViewDepthStencilView;
        ShaderResourceView userViewSRV, filteredUserViewSRV, desktopTextureSRV;
        Viewport userViewViewport;
        SharpDX.Direct3D11.Device device;
        ProjectorCameraEnsemble ensemble;
        Form1 userViewForm;
        MeshShader meshShader;
        MeshDeviceResources meshDeviceResources;
        PassThrough passThroughShader;
        RadialWobble radialWobbleShader;
        FromUIntPS fromUIntPS;
        BilateralFilter bilateralFilter;
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        PointLight pointLight = new PointLight();
        Texture2D desktopTexture;

        SharpDX.WIC.ImagingFactory2 imagingFactory = new ImagingFactory2();
        float alpha = 1;

        // TODO: make so these can be changed live, put in menu
        bool threeDObjectEnabled = Properties.Settings.Default.ThreeDObjectEnabled;
        bool wobbleEffectEnabled = Properties.Settings.Default.WobbleEffectEnabled;
        bool localHeadTrackingEnabled = Properties.Settings.Default.LocalHeadTrackingEnabled;
        bool liveDepthEnabled = Properties.Settings.Default.LiveDepthEnabled;
        bool fullScreenEnabled = Properties.Settings.Default.FullScreenEnabled;
        bool desktopDuplicationEnabled = Properties.Settings.Default.DesktopDuplicationEnabled;

        void RenderLoop()
        {
 
            while (true)
            {
                lock (renderLock)
                {
                    var deviceContext = device.ImmediateContext;

                    // render user view
                    deviceContext.ClearRenderTargetView(userViewRenderTargetView, Color4.Black);
                    deviceContext.ClearDepthStencilView(userViewDepthStencilView, DepthStencilClearFlags.Depth, 1, 0);

                    SharpDX.Vector3 headPosition =  new SharpDX.Vector3(0f, 1.1f, -1.4f); // may need to change this default

                    if (localHeadTrackingEnabled)
                    {
                        float distanceSquared = 0;
                        lock (headCameraSpacePointLock)
                        {
                            headPosition = new SharpDX.Vector3(headCameraSpacePoint.X, headCameraSpacePoint.Y, headCameraSpacePoint.Z);

                            float dx = handLeftCameraSpacePoint.X - handRightCameraSpacePoint.X;
                            float dy = handLeftCameraSpacePoint.Y - handRightCameraSpacePoint.Y;
                            float dz = handLeftCameraSpacePoint.Z - handRightCameraSpacePoint.Z;
                            distanceSquared = dx * dx + dy * dy + dz * dz;
                        }
                        var transform = SharpDX.Matrix.RotationY((float)Math.PI) * SharpDX.Matrix.Translation(0, 0.45f, 0);
                        headPosition = SharpDX.Vector3.TransformCoordinate(headPosition, transform);

                        if (trackingValid && (distanceSquared < 0.02f) && (alpha > 1))
                            alpha = 0;
                        //Console.WriteLine(distanceSquared);
                    }

                    var userView = GraphicsTransforms.LookAt(headPosition, headPosition + SharpDX.Vector3.UnitZ, SharpDX.Vector3.UnitY);
                    userView.Transpose();


                    //Console.WriteLine("headPosition = " + headPosition);


                    float aspect = (float)userViewTextureWidth / (float)userViewTextureHeight;
                    var userProjection = GraphicsTransforms.PerspectiveFov(55.0f / 180.0f * (float)Math.PI, aspect, 0.001f, 1000.0f);
                    userProjection.Transpose();

                    // smooth depth images
                    foreach (var camera in ensemble.cameras)
                    {
                        var cameraDeviceResource = cameraDeviceResources[camera];
                        if (cameraDeviceResource.depthImageChanged)
                        {
                            fromUIntPS.Render(deviceContext, cameraDeviceResource.depthImageTextureRV, cameraDeviceResource.floatDepthImageRenderTargetView);
                            for (int i = 0; i < 1; i++)
                            {
                                bilateralFilter.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, cameraDeviceResource.floatDepthImageRenderTargetView2);
                                bilateralFilter.Render(deviceContext, cameraDeviceResource.floatDepthImageRV2, cameraDeviceResource.floatDepthImageRenderTargetView);
                            }
                            cameraDeviceResource.depthImageChanged = false;
                        }
                    }

                    // wobble effect
                    if (wobbleEffectEnabled)
                        foreach (var camera in ensemble.cameras)
                        {
                            var cameraDeviceResource = cameraDeviceResources[camera];

                            var world = new SharpDX.Matrix();
                            for (int i = 0; i < 4; i++)
                                for (int j = 0; j < 4; j++)
                                    world[i, j] = (float)camera.pose[i, j];
                            world.Transpose();

                            // view and projection matrix are post-multiply
                            var userWorldViewProjection = world * userView * userProjection;

                            depthAndColorShader.SetConstants(deviceContext, camera.calibration, userWorldViewProjection);
                            depthAndColorShader.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, cameraDeviceResource.colorImageTextureRV, cameraDeviceResource.vertexBuffer, userViewRenderTargetView, userViewDepthStencilView, userViewViewport);
                        }

                    // 3d object
                    if (threeDObjectEnabled)
                    {
                        var world = SharpDX.Matrix.Scaling(1.0f) * SharpDX.Matrix.RotationY(90.0f / 180.0f * (float)Math.PI) *
                            SharpDX.Matrix.RotationX(-40.0f / 180.0f * (float)Math.PI) * SharpDX.Matrix.Translation(0, 0.7f, 0.0f);

                        var pointLight = new PointLight();
                        pointLight.position = new Vector3(0, 2, 0);
                        pointLight.Ia = new Vector3(0.1f, 0.1f, 0.1f);
                        meshShader.SetVertexShaderConstants(deviceContext, world, userView * userProjection, pointLight.position);
                        meshShader.Render(deviceContext, meshDeviceResources, pointLight, userViewRenderTargetView, userViewDepthStencilView, userViewViewport);
                    }

                    // wobble effect
                    if (wobbleEffectEnabled)
                    {
                        alpha += 0.05f;
                        if (alpha > 1)
                            radialWobbleShader.SetConstants(deviceContext, 0);
                        else
                            radialWobbleShader.SetConstants(deviceContext, alpha);
                        radialWobbleShader.Render(deviceContext, userViewSRV, filteredUserViewRenderTargetView);
                    }

                    // desktop duplication
                    if (desktopDuplicationEnabled)
                    {
                        // update the desktop texture; this will block until there is some change
                        var outputDuplicateFrameInformation = default(OutputDuplicateFrameInformation);
                        SharpDX.DXGI.Resource resource = null;
                        outputDuplication.AcquireNextFrame(1000, out outputDuplicateFrameInformation, out resource);
                        var texture = resource.QueryInterface<Texture2D>();

                        // pick up the window under the cursor
                        var cursorPos = new POINT();
                        GetCursorPos(out cursorPos);
                        var hwnd = WindowFromPoint(cursorPos);
                        var rect = new RECT();
                        GetWindowRect(hwnd, out rect);

                        // adjust bounds so falls within source texture
                        if (rect.Left < 0) rect.Left = 0;
                        if (rect.Top < 0) rect.Top = 0;
                        if (rect.Right > texture.Description.Width - 1) rect.Right = texture.Description.Width;
                        if (rect.Bottom > texture.Description.Height - 1) rect.Bottom = texture.Description.Height;

                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;

                        // resize our texture if necessary
                        if ((desktopTexture == null) || (desktopTexture.Description.Width != width) || (desktopTexture.Description.Height != height))
                        {
                            if (desktopTexture != null)
                            {
                                desktopTextureSRV.Dispose();
                                desktopTexture.Dispose();
                            }
                            var desktopTextureDesc = new Texture2DDescription()
                            {
                                Width = width,
                                Height = height,
                                MipLevels = 1, // revisit this; we may benefit from mipmapping?
                                ArraySize = 1,
                                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                                Usage = ResourceUsage.Default,
                                BindFlags = BindFlags.ShaderResource,
                                CpuAccessFlags = CpuAccessFlags.None,
                            };
                            desktopTexture = new Texture2D(device, desktopTextureDesc);
                            desktopTextureSRV = new ShaderResourceView(device, desktopTexture);
                        }

                        // copy the winodw region into our texture
                        var sourceRegion = new ResourceRegion()
                        {
                            Left = rect.Left,
                            Right = rect.Right,
                            Top = rect.Top,
                            Bottom = rect.Bottom,
                            Front = 0,
                            Back = 1,
                        };
                        deviceContext.CopySubresourceRegion(texture, 0, sourceRegion, desktopTexture, 0);
                        texture.Dispose();
                    }

                    // render user view to seperate form
                    passThroughShader.viewport = new Viewport(0, 0, userViewForm.videoPanel1.Width, userViewForm.videoPanel1.Height);

                    // TODO: clean this up by simply using a pointer to the userViewSRV
                    if (threeDObjectEnabled)
                    {
                        passThroughShader.Render(deviceContext, userViewSRV, userViewForm.renderTargetView);
                    }
                    if (wobbleEffectEnabled)
                    {
                        passThroughShader.Render(deviceContext, filteredUserViewSRV, userViewForm.renderTargetView);
                    }
                    if (desktopDuplicationEnabled)
                    {
                        passThroughShader.Render(deviceContext, desktopTextureSRV, userViewForm.renderTargetView);
                    }
                    userViewForm.swapChain.Present(0, PresentFlags.None);

                    // projection puts x and y in [-1,1]; adjust to obtain texture coordinates [0,1]
                    // TODO: put this in SetContants?
                    userProjection[0, 0] /= 2;
                    userProjection[1, 1] /= -2; // y points down
                    userProjection[2, 0] += 0.5f;
                    userProjection[2, 1] += 0.5f;

                    // projection mapping for each projector
                    foreach (var form in projectorForms)
                    {
                        deviceContext.ClearRenderTargetView(form.renderTargetView, Color4.Black);
                        deviceContext.ClearDepthStencilView(form.depthStencilView, DepthStencilClearFlags.Depth, 1, 0);

                        foreach (var camera in ensemble.cameras)
                        {
                            var cameraDeviceResource = cameraDeviceResources[camera];

                            var world = new SharpDX.Matrix();
                            for (int i = 0; i < 4; i++)
                                for (int j = 0; j < 4; j++)
                                    world[i, j] = (float)camera.pose[i, j];
                            world.Transpose();

                            var projectorWorldViewProjection = world * form.view * form.projection;
                            var userWorldViewProjection = world * userView * userProjection;

                            projectiveTexturingShader.SetConstants(deviceContext, userWorldViewProjection, projectorWorldViewProjection);

                            // TODO: clean this up by simply using a pointer to the userViewSRV
                            if (wobbleEffectEnabled)
                                projectiveTexturingShader.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, filteredUserViewSRV, cameraDeviceResource.vertexBuffer, form.renderTargetView, form.depthStencilView, form.viewport);
                            if (threeDObjectEnabled)
                                projectiveTexturingShader.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, userViewSRV, cameraDeviceResource.vertexBuffer, form.renderTargetView, form.depthStencilView, form.viewport);
                            if (desktopDuplicationEnabled)
                                projectiveTexturingShader.Render(deviceContext, cameraDeviceResource.floatDepthImageRV, desktopTextureSRV, cameraDeviceResource.vertexBuffer, form.renderTargetView, form.depthStencilView, form.viewport);
                        }

                        form.swapChain.Present(1, PresentFlags.None);
                    }


                    if (desktopDuplicationEnabled)
                        outputDuplication.ReleaseFrame();


                    //Console.WriteLine(stopwatch.ElapsedMilliseconds);
                    stopwatch.Restart();
                }
            }
        }

        KinectSensor localKinectSensor;
        BodyFrameReader bodyFrameReader;
        Body[] bodies = null;
        CameraSpacePoint headCameraSpacePoint, handLeftCameraSpacePoint, handRightCameraSpacePoint;
        Object headCameraSpacePointLock = new Object();
        bool trackingValid = false;

        void LocalBodyLoop()
        {
            while (true)
            {
                // find closest tracked head
                var bodyFrame = bodyFrameReader.AcquireLatestFrame();

                if (bodyFrame != null)
                {
                    if (bodies == null)
                        bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    bool foundTrackedBody = false;
                    float distanceToNearest = float.MaxValue;
                    var nearestHeadCameraSpacePoint = new CameraSpacePoint();
                    var nearestHandRightCameraSpacePoint = new CameraSpacePoint();
                    var nearestHandLeftCameraSpacePoint = new CameraSpacePoint();


                    foreach (var body in bodies)
                        if (body.IsTracked)
                        {
                            var cameraSpacePoint = body.Joints[JointType.Head].Position;
                            if (cameraSpacePoint.Z < distanceToNearest)
                            {
                                distanceToNearest = cameraSpacePoint.Z;
                                nearestHeadCameraSpacePoint = cameraSpacePoint;
                                nearestHandLeftCameraSpacePoint = body.Joints[JointType.HandLeft].Position;
                                nearestHandRightCameraSpacePoint = body.Joints[JointType.HandRight].Position;
                                foundTrackedBody = true;
                            }
                        }

                    lock (headCameraSpacePointLock)
                    {
                        if (foundTrackedBody)
                        {
                            headCameraSpacePoint = nearestHeadCameraSpacePoint;
                            handLeftCameraSpacePoint = nearestHandLeftCameraSpacePoint;
                            handRightCameraSpacePoint = nearestHandRightCameraSpacePoint;

                            trackingValid = true;
                            //Console.WriteLine("{0} {1} {2}", headCameraSpacePoint.X, headCameraSpacePoint.Y, headCameraSpacePoint.Z);
                        }
                        else
                        {
                            headCameraSpacePoint.X = 0f;
                            headCameraSpacePoint.Y = 0.3f;
                            headCameraSpacePoint.Z = 1.5f;

                            trackingValid = false;
                        }
                    }

                    bodyFrame.Dispose();
                }
                else
                    System.Threading.Thread.Sleep(5);
            }
        }





        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x, y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetCursorPos(out POINT lpPoint);
    }
}