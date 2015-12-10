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
                cameraDeviceResources[camera] = new CameraDeviceResource(device, camera, renderLock, directory);

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




        class CameraDeviceResource : IDisposable
        {
            // encapsulates d3d resources for a camera
            public CameraDeviceResource(SharpDX.Direct3D11.Device device, ProjectorCameraEnsemble.Camera camera, Object renderLock, string directory)
            {
                this.device = device;
                this.camera = camera;
                this.renderLock = renderLock;

                // Kinect depth image
                var depthImageTextureDesc = new Texture2DDescription()
                {
                    Width = Kinect2Calibration.depthImageWidth,
                    Height = Kinect2Calibration.depthImageHeight,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.R16_UInt,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.Write,
                };
                depthImageTexture = new Texture2D(device, depthImageTextureDesc);
                depthImageTextureRV = new ShaderResourceView(device, depthImageTexture);

                var floatDepthImageTextureDesc = new Texture2DDescription()
                {
                    Width = Kinect2Calibration.depthImageWidth,
                    Height = Kinect2Calibration.depthImageHeight,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.R32_Float,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                };

                floatDepthImageTexture = new Texture2D(device, floatDepthImageTextureDesc);
                floatDepthImageRV = new ShaderResourceView(device, floatDepthImageTexture);
                floatDepthImageRenderTargetView = new RenderTargetView(device, floatDepthImageTexture);

                floatDepthImageTexture2 = new Texture2D(device, floatDepthImageTextureDesc);
                floatDepthImageRV2 = new ShaderResourceView(device, floatDepthImageTexture2);
                floatDepthImageRenderTargetView2 = new RenderTargetView(device, floatDepthImageTexture2);

                // Kinect color image
                var colorImageStagingTextureDesc = new Texture2DDescription()
                {
                    Width = Kinect2Calibration.colorImageWidth,
                    Height = Kinect2Calibration.colorImageHeight,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.Write
                };
                colorImageStagingTexture = new Texture2D(device, colorImageStagingTextureDesc);

                var colorImageTextureDesc = new Texture2DDescription()
                {
                    Width = Kinect2Calibration.colorImageWidth,
                    Height = Kinect2Calibration.colorImageHeight,
                    MipLevels = 0,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.GenerateMipMaps
                };
                colorImageTexture = new Texture2D(device, colorImageTextureDesc);
                colorImageTextureRV = new ShaderResourceView(device, colorImageTexture);

                // vertex buffer
                var table = camera.calibration.ComputeDepthFrameToCameraSpaceTable();
                int numVertices = 6 * (Kinect2Calibration.depthImageWidth - 1) * (Kinect2Calibration.depthImageHeight - 1);
                var vertices = new VertexPosition[numVertices];

                Int3[] quadOffsets = new Int3[]
                {
                    new Int3(0, 0, 0),  
                    new Int3(1, 0, 0),  
                    new Int3(0, 1, 0),  
                    new Int3(1, 0, 0),  
                    new Int3(1, 1, 0),  
                    new Int3(0, 1, 0),  
                };

                int vertexIndex = 0;
                for (int y = 0; y < Kinect2Calibration.depthImageHeight - 1; y++)
                    for (int x = 0; x < Kinect2Calibration.depthImageWidth - 1; x++)
                        for (int i = 0; i < 6; i++)
                        {
                            int vertexX = x + quadOffsets[i].X;
                            int vertexY = y + quadOffsets[i].Y;

                            var point = table[Kinect2Calibration.depthImageWidth * vertexY + vertexX];

                            var vertex = new VertexPosition();
                            vertex.position = new SharpDX.Vector4(point.X, point.Y, vertexX, vertexY);
                            vertices[vertexIndex++] = vertex;
                        }

                var stream = new DataStream(numVertices * VertexPosition.SizeInBytes, true, true);
                stream.WriteRange(vertices);
                stream.Position = 0;

                var vertexBufferDesc = new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Usage = ResourceUsage.Default,
                    SizeInBytes = numVertices * VertexPosition.SizeInBytes,
                };
                vertexBuffer = new SharpDX.Direct3D11.Buffer(device, stream, vertexBufferDesc);

                vertexBufferBinding = new VertexBufferBinding(vertexBuffer, VertexPosition.SizeInBytes, 0);

                stream.Dispose();

                var colorImage = new RoomAliveToolkit.ARGBImage(Kinect2Calibration.colorImageWidth, Kinect2Calibration.colorImageHeight);
                ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, colorImage, directory + "/camera" + camera.name + "/color.tiff");

                var depthImage = new RoomAliveToolkit.ShortImage(Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);
                ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, depthImage, directory + "/camera" + camera.name + "/mean.tiff");

                lock (renderLock) // necessary?
                {
                    UpdateColorImage(device.ImmediateContext, colorImage.DataIntPtr);
                    UpdateDepthImage(device.ImmediateContext, depthImage.DataIntPtr);
                }

                colorImage.Dispose();
                depthImage.Dispose();




            }

            struct VertexPosition
            {
                public SharpDX.Vector4 position;
                static public int SizeInBytes { get { return 4 * 4; } }
            }

            public void Dispose()
            {
                depthImageTexture.Dispose();
                depthImageTextureRV.Dispose();
                colorImageTexture.Dispose();
                colorImageTextureRV.Dispose();
                colorImageStagingTexture.Dispose();
                vertexBuffer.Dispose();
            }

            SharpDX.Direct3D11.Device device;
            public Texture2D depthImageTexture, floatDepthImageTexture, floatDepthImageTexture2;
            public ShaderResourceView depthImageTextureRV, floatDepthImageRV, floatDepthImageRV2;
            public RenderTargetView floatDepthImageRenderTargetView, floatDepthImageRenderTargetView2;
            public Texture2D colorImageTexture;
            public ShaderResourceView colorImageTextureRV;
            public Texture2D colorImageStagingTexture;
            public SharpDX.Direct3D11.Buffer vertexBuffer;
            VertexBufferBinding vertexBufferBinding;
            ProjectorCameraEnsemble.Camera camera;
            public bool renderEnabled = true;

            public void UpdateDepthImage(DeviceContext deviceContext, IntPtr depthImage)
            {
                DataStream dataStream;
                deviceContext.MapSubresource(depthImageTexture, 0,
                   MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                dataStream.WriteRange(depthImage, Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 2);
                deviceContext.UnmapSubresource(depthImageTexture, 0);
            }

            public void UpdateDepthImage(DeviceContext deviceContext, byte[] depthImage)
            {
                DataStream dataStream;
                deviceContext.MapSubresource(depthImageTexture, 0,
                   MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                dataStream.WriteRange<byte>(depthImage, 0, Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 2);
                deviceContext.UnmapSubresource(depthImageTexture, 0);
            }

            public void UpdateColorImage(DeviceContext deviceContext, IntPtr colorImage)
            {
                DataStream dataStream;
                deviceContext.MapSubresource(colorImageStagingTexture, 0,
                    MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                dataStream.WriteRange(colorImage, Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 4);
                deviceContext.UnmapSubresource(colorImageStagingTexture, 0);

                var resourceRegion = new ResourceRegion()
                {
                    Left = 0,
                    Top = 0,
                    Right = Kinect2Calibration.colorImageWidth,
                    Bottom = Kinect2Calibration.colorImageHeight,
                    Front = 0,
                    Back = 1,
                };
                deviceContext.CopySubresourceRegion(colorImageStagingTexture, 0, resourceRegion, colorImageTexture, 0);
                deviceContext.GenerateMips(colorImageTextureRV);
            }

            public void UpdateColorImage(DeviceContext deviceContext, byte[] colorImage)
            {
                DataStream dataStream;
                deviceContext.MapSubresource(colorImageStagingTexture, 0,
                    MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
                dataStream.WriteRange<byte>(colorImage, 0, Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 4);
                deviceContext.UnmapSubresource(colorImageStagingTexture, 0);

                var resourceRegion = new ResourceRegion()
                {
                    Left = 0,
                    Top = 0,
                    Right = Kinect2Calibration.colorImageWidth,
                    Bottom = Kinect2Calibration.colorImageHeight,
                    Front = 0,
                    Back = 1,
                };
                deviceContext.CopySubresourceRegion(colorImageStagingTexture, 0, resourceRegion, colorImageTexture, 0);
                deviceContext.GenerateMips(colorImageTextureRV);
            }

            public void Render(DeviceContext deviceContext)
            {
                deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
                deviceContext.VertexShader.SetShaderResource(0, depthImageTextureRV);
                deviceContext.PixelShader.SetShaderResource(0, colorImageTextureRV);
                deviceContext.Draw((Kinect2Calibration.depthImageWidth - 1) * (Kinect2Calibration.depthImageHeight - 1) * 6, 0);
            }

            //bool live = false;

            public void StartLive()
            {
                //live = true;
                //new System.Threading.Thread(ColorCameraLoop).Start();
                new System.Threading.Thread(DepthCameraLoop).Start();
            }

            public void StopLive()
            {
                //live = false;
            }


            Object renderLock;
            public bool depthImageChanged = true;

            //byte[] colorData = new byte[4 * Kinect2.Kinect2Calibration.colorImageWidth * Kinect2.Kinect2Calibration.colorImageHeight];
            byte[] nextColorData = new byte[4 * RoomAliveToolkit.Kinect2Calibration.colorImageWidth * RoomAliveToolkit.Kinect2Calibration.colorImageHeight];
            SharpDX.WIC.ImagingFactory2 imagingFactory = new SharpDX.WIC.ImagingFactory2();
            void ColorCameraLoop()
            {
                while (true)
                {
                    var encodedColorData = camera.Client.LatestJPEGImage();

                    // decode JPEG
                    var memoryStream = new MemoryStream(encodedColorData);
                    var stream = new WICStream(imagingFactory, memoryStream);
                    // decodes to 24 bit BGR
                    var decoder = new SharpDX.WIC.BitmapDecoder(imagingFactory, stream, SharpDX.WIC.DecodeOptions.CacheOnLoad);
                    var bitmapFrameDecode = decoder.GetFrame(0);

                    // convert to 32 bpp
                    var formatConverter = new FormatConverter(imagingFactory);
                    formatConverter.Initialize(bitmapFrameDecode, SharpDX.WIC.PixelFormat.Format32bppBGR);
                    formatConverter.CopyPixels(nextColorData, Kinect2Calibration.colorImageWidth * 4); // TODO: consider copying directly to texture native memory
                    //lock (colorData)
                    //    Swap<byte[]>(ref colorData, ref nextColorData);
                    lock (renderLock) // necessary?
                    {
                        UpdateColorImage(device.ImmediateContext, nextColorData);
                    }
                    memoryStream.Close();
                    memoryStream.Dispose();
                    stream.Dispose();
                    decoder.Dispose();
                    formatConverter.Dispose();
                    bitmapFrameDecode.Dispose();
                }
            }

            //byte[] depthData = new byte[2 * Kinect2.Kinect2Calibration.depthImageWidth * Kinect2.Kinect2Calibration.depthImageHeight];
            byte[] nextDepthData;
            void DepthCameraLoop()
            {
                while (true)
                {
                    nextDepthData = camera.Client.LatestDepthImage();
                    //lock (remoteDepthData)
                    //    Swap<byte[]>(ref remoteDepthData, ref nextRemoteDepthData);
                    lock (renderLock)
                    {
                        depthImageChanged = true;
                        UpdateDepthImage(device.ImmediateContext, nextDepthData);
                    }
                }
            }

            static void Swap<T>(ref T first, ref T second)
            {
                T temp = first;
                first = second;
                second = temp;
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