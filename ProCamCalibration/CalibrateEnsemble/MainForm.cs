﻿using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceModel;
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

        FrustumShader frustumShader;
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
            SharpDX.Direct3D11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.None, swapChainDesc, out device, out swapChain);

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

            
            frustumShader = new FrustumShader(device);

            // shaders
            var shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorVS.cso"));
            depthAndColorVS2 = new VertexShader(device, shaderByteCode);
            depthAndColorGS2 = new GeometryShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorGS.cso")));
            depthAndColorPS2 = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/DepthAndColorPS.cso")));

            // depth stencil state
            var depthStencilStateDesc = new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.LessEqual,
                IsStencilEnabled = false,
            };
            depthStencilState = new DepthStencilState(device, depthStencilStateDesc);

            UpdateRasterizerState();

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

            // vertex layout is the same for all cameras
            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("SV_POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
            });

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

            Width = Properties.Settings.Default.FormWidth;
            Height = Properties.Settings.Default.FormHeight;
            splitContainer1.SplitterDistance = Properties.Settings.Default.SplitterDistance;

            new System.Threading.Thread(RenderLoop).Start();
        }

        private void UpdateRasterizerState()
        {
            lock (renderLock)
            {
                // rasterizer states
                var rasterizerStateDesc = new RasterizerStateDescription()
                {
                    CullMode = CullMode.None, // beware what this does to both shaders
                    FillMode = wireframe ? FillMode.Wireframe : FillMode.Solid,
                    IsDepthClipEnabled = true,
                    IsFrontCounterClockwise = true,
                    IsMultisampleEnabled = true,
                };
                rasterizerState = new RasterizerState(device, rasterizerStateDesc);
            }
        }

        SharpDX.Direct3D11.Device device;
        SwapChain swapChain;
        Texture2D renderTarget, depthStencil;
        RenderTargetView renderTargetView;
        DepthStencilView depthStencilView;
        Viewport viewport;
        VertexShader depthAndColorVS2;
        GeometryShader depthAndColorGS2;
        PixelShader depthAndColorPS2;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SamplerState colorSamplerState;
        InputLayout vertexInputLayout;
        SharpDX.Direct3D11.Buffer constantBuffer;
        Manipulator manipulator;



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


                if (File.Exists(directory + "/camera" + camera.name + "/color.tiff")) // FIX: this assumes that mean.tiff is exists (very likely)
                {
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

                //StartLive();

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
            public Texture2D depthImageTexture;
            public ShaderResourceView depthImageTextureRV;
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

            bool live = false;

            public void StartLive()
            {
                live = true;
                new System.Threading.Thread(ColorCameraLoop).Start();
                new System.Threading.Thread(DepthCameraLoop).Start();
            }

            public void StopLive()
            {
                live = false;
            }

            Object renderLock;

            //byte[] colorData = new byte[4 * Kinect2.Kinect2Calibration.colorImageWidth * Kinect2.Kinect2Calibration.colorImageHeight];
            byte[] nextColorData = new byte[4 * RoomAliveToolkit.Kinect2Calibration.colorImageWidth * RoomAliveToolkit.Kinect2Calibration.colorImageHeight];
            SharpDX.WIC.ImagingFactory imagingFactory = new SharpDX.WIC.ImagingFactory();
            void ColorCameraLoop()
            {
                while (live)
                {
                    try
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
                    catch (EndpointNotFoundException)
                    {
                        Console.WriteLine("Could not find Kinect server for live color update.");
                        live = false;
                    }
                }
            }

            //byte[] depthData = new byte[2 * Kinect2.Kinect2Calibration.depthImageWidth * Kinect2.Kinect2Calibration.depthImageHeight];
            byte[] nextDepthData;
            void DepthCameraLoop()
            {
                while (live)
                {
                    try
                    {
                        nextDepthData = camera.Client.LatestDepthImage();

                        //lock (remoteDepthData)
                        //    Swap<byte[]>(ref remoteDepthData, ref nextRemoteDepthData);
                        lock (renderLock)
                        {
                            UpdateDepthImage(device.ImmediateContext, nextDepthData);
                        }
                    }
                    catch (EndpointNotFoundException)
                    {
                        Console.WriteLine("Could not find Kinect server for live depth update.");
                        live = false;
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

        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = ConstantBuffer.size)]
        unsafe struct ConstantBuffer
        {
            public const int size = 160;

            [FieldOffset(0)]
            public fixed float depthToColorTransform[16]; // 4-component padding
            [FieldOffset(64)]
            public fixed float f[2];
            [FieldOffset(72)]
            public fixed float c[2];
            [FieldOffset(80)]
            public float k1;
            [FieldOffset(84)]
            public float k2;
            [FieldOffset(96)]
            public fixed float projection[16];
        };

        public unsafe void SetConstants(DeviceContext deviceContext, RoomAliveToolkit.Kinect2Calibration kinect2Calibration, SharpDX.Matrix projection)
        {
            // hlsl matrices are default column order
            var constants = new ConstantBuffer();
            for (int i = 0, col = 0; col < 4; col++)
            {
                for (int row = 0; row < 4; row++)
                {
                    constants.projection[i] = projection[row, col];
                    constants.depthToColorTransform[i] = (float)kinect2Calibration.depthToColorTransform[row, col];
                    i++;
                }
            }


            constants.f[0] = (float)kinect2Calibration.colorCameraMatrix[0, 0];
            constants.f[1] = (float)kinect2Calibration.colorCameraMatrix[1, 1];
            constants.c[0] = (float)kinect2Calibration.colorCameraMatrix[0, 2];
            constants.c[1] = (float)kinect2Calibration.colorCameraMatrix[1, 2];
            constants.k1 = (float)kinect2Calibration.colorLensDistortion[0];
            constants.k2 = (float)kinect2Calibration.colorLensDistortion[1];

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.Write<ConstantBuffer>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        Object renderLock = new Object();


        void RenderLoop()
        {
            while (true)
            {
                lock (renderLock)
                {
                    view = manipulator.Update();

                    var deviceContext = device.ImmediateContext;
                    deviceContext.InputAssembler.InputLayout = vertexInputLayout;
                    deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
                    deviceContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);
                    deviceContext.OutputMerger.DepthStencilState = depthStencilState;
                    deviceContext.Rasterizer.State = rasterizerState;
                    deviceContext.Rasterizer.SetViewport(viewport);
                    deviceContext.VertexShader.Set(depthAndColorVS2);
                    deviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
                    deviceContext.GeometryShader.Set(depthAndColorGS2);
                    deviceContext.PixelShader.Set(depthAndColorPS2);
                    deviceContext.PixelShader.SetSampler(0, colorSamplerState);
                    deviceContext.ClearRenderTargetView(renderTargetView, Color4.Black);
                    deviceContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1, 0);

                    // render all cameras
                    if (ensemble != null)
                    {
                        var viewProjection = view * projection;

                        foreach (var camera in ensemble.cameras)
                        {
                            if (cameraDeviceResources.ContainsKey(camera))
                            {
                                if (cameraDeviceResources[camera].renderEnabled && (camera.pose != null))
                                {
                                    var world = new SharpDX.Matrix();
                                    for (int i = 0; i < 4; i++)
                                        for (int j = 0; j < 4; j++)
                                            world[i, j] = (float)camera.pose[i, j];
                                    world.Transpose();

                                    // view and projection matrix are post-multiply
                                    var worldViewProjection = world * viewProjection;

                                    SetConstants(deviceContext, camera.calibration, worldViewProjection);
                                    cameraDeviceResources[camera].Render(deviceContext);
                                }
                            }
                        }

                        foreach (var camera in ensemble.cameras)
                        {
                            if (cameraDeviceResources.ContainsKey(camera))
                            {
                                if (cameraDeviceResources[camera].renderEnabled && (camera.pose != null))
                                {
                                    var world = new SharpDX.Matrix();
                                    for (int i = 0; i < 4; i++)
                                        for (int j = 0; j < 4; j++)
                                            world[i, j] = (float)camera.pose[i, j];
                                    world.Transpose();

                                    // view and projection matrix are post-multiply
                                    var worldViewProjection = world * viewProjection;

                                    if (renderCameraColorFrustums)
                                    {
                                        frustumShader.SetPixelShaderConstants(deviceContext, new Color3(1f, 1f, 0f));

                                        SharpDX.Matrix cameraProjection = GetCameraColorProjectionMatrix(camera);
                                        cameraProjection.Transpose();
                                        cameraProjection.Invert();

                                        frustumShader.SetVertexShaderConstants(deviceContext, world, viewProjection, cameraProjection);
                                        frustumShader.Render(deviceContext);
                                    }

                                    if (renderCameraDepthFrustums)
                                    {
                                        frustumShader.SetPixelShaderConstants(deviceContext, new Color3(0f, 1f, 1f));

                                        SharpDX.Matrix cameraProjection = GetCameraDepthProjectionMatrix(camera);
                                        cameraProjection.Transpose();
                                        cameraProjection.Invert();

                                        frustumShader.SetVertexShaderConstants(deviceContext, world, viewProjection, cameraProjection);
                                        frustumShader.Render(deviceContext);
                                    }
                                }
                            }
                        }

                        if (renderProjectorFrustums)
                        {
                            frustumShader.SetPixelShaderConstants(deviceContext, new Color3(1f, 0f, 1f));
                            foreach (var projector in ensemble.projectors)
                            {
                                if (projector.pose != null)
                                {
                                    SharpDX.Matrix projectorProjection = GetProjectorProjectionMatrix(projector);
                                    projectorProjection.Transpose();
                                    projectorProjection.Invert();

                                    var world = new SharpDX.Matrix();
                                    for (int i = 0; i < 4; i++)
                                        for (int j = 0; j < 4; j++)
                                            world[i, j] = (float)projector.pose[i, j];
                                    world.Transpose();

                                    frustumShader.SetVertexShaderConstants(deviceContext, world, viewProjection, projectorProjection);
                                    frustumShader.Render(deviceContext);
                                }
                            }
                        }
                    }

                    swapChain.Present(1, PresentFlags.None);
                }
            }
        }

        SharpDX.Matrix view, projection;

        Dictionary<ProjectorCameraEnsemble.Camera, CameraDeviceResource> cameraDeviceResources = new Dictionary<ProjectorCameraEnsemble.Camera, CameraDeviceResource>();

        void EnsembleChanged()
        {
            lock (renderLock)
            {
                // deallocate/allocate camera d3d resources
                foreach (var cameraDeviceResource in cameraDeviceResources.Values)
                    cameraDeviceResource.Dispose();
                cameraDeviceResources.Clear();

                foreach (var camera in ensemble.cameras)
                {
                    if (camera.calibration != null) // TODO: this might not be the right way to check
                        cameraDeviceResources[camera] = new CameraDeviceResource(device, camera, renderLock, directory);
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

            // Prevents KeyNotFoundException if camera was not calibrated and therefore CameraDeviceResource was not created
            CameraDeviceResource cameraDeviceResource;
            if (cameraDeviceResources.TryGetValue(camera, out cameraDeviceResource))
            {
                cameraDeviceResource.renderEnabled = toolStripMenuItem.Checked;
            }
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
            projection = GetProjectorProjectionMatrix(projector);
            projection.Transpose(); 
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

        bool wireframe = false;
        private void wireframeViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            wireframe = !wireframe;
            wireframeToolStripMenuItem.Checked = wireframe;
            UpdateRasterizerState();
        }

        bool renderCameraColorFrustums = false;
        private void cameraColorFrustumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderCameraColorFrustums = !renderCameraColorFrustums;
            cameraColorFrustumsToolStripMenuItem.Checked = renderCameraColorFrustums;
        }

        bool renderCameraDepthFrustums = false;
        private void cameraDepthFrustumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderCameraDepthFrustums = !renderCameraDepthFrustums;
            cameraDepthFrustumsToolStripMenuItem.Checked = renderCameraDepthFrustums;
        }

        bool renderProjectorFrustums = false;
        private void projectorFrustumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            renderProjectorFrustums = !renderProjectorFrustums;
            projectorFrustumsToolStripMenuItem.Checked = renderProjectorFrustums;
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
                    Console.WriteLine("Containing: {0} camera(s), {1} projector(s)", ensemble.cameras.Count, ensemble.projectors.Count);
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
        SharpDX.Matrix GetProjectorProjectionMatrix(ProjectorCameraEnsemble.Projector projector)
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

                return GraphicsTransforms.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
            }

            return SharpDX.Matrix.Identity;
        }

        SharpDX.Matrix GetCameraColorProjectionMatrix(ProjectorCameraEnsemble.Camera camera)
        {
            if ((camera.pose == null) || (camera.calibration == null))
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

                return GraphicsTransforms.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
            }

            return SharpDX.Matrix.Identity;
        }

        SharpDX.Matrix GetCameraDepthProjectionMatrix(ProjectorCameraEnsemble.Camera camera)
        {
            if ((camera.pose == null) || (camera.calibration == null))
                Console.WriteLine("Camera pose not set.");
            else
            {
                view = new SharpDX.Matrix();
                for (int i = 0; i < 4; i++)
                    for (int j = 0; j < 4; j++)
                        view[i, j] = (float)camera.pose[i, j];
                view.Invert();
                view.Transpose();

                var cameraMatrix = camera.calibration.depthCameraMatrix;
                float fx = (float)cameraMatrix[0, 0];
                float fy = (float)cameraMatrix[1, 1];
                float cx = (float)cameraMatrix[0, 2];
                float cy = (float)cameraMatrix[1, 2];

                float near = 0.1f;
                float far = 100.0f;

                float w = Kinect2Calibration.depthImageWidth;
                float h = Kinect2Calibration.depthImageHeight;

                return GraphicsTransforms.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
            }

            return SharpDX.Matrix.Identity;
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
