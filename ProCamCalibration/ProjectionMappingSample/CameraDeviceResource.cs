using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System.IO;


namespace RoomAliveToolkit
{
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
            {
                for (int x = 0; x < Kinect2Calibration.depthImageWidth - 1; x++)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        int vertexX = x + quadOffsets[i].X;
                        int vertexY = y + quadOffsets[i].Y;

                        var point = table[Kinect2Calibration.depthImageWidth * vertexY + vertexX];

                        var vertex = new VertexPosition();
                        vertex.position = new SharpDX.Vector4(point.X, point.Y, vertexX, vertexY);
                        vertices[vertexIndex++] = vertex;
                    }
                }
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

            //var colorImage = new RoomAliveToolkit.ARGBImage(Kinect2Calibration.colorImageWidth, Kinect2Calibration.colorImageHeight);
            //ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, colorImage, directory + "/camera" + camera.name + "/color.tiff");

            var depthImage = new RoomAliveToolkit.ShortImage(Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);
            ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, depthImage, directory + "/camera" + camera.name + "/mean.tiff");

            lock (renderLock) // necessary?
            {
                //UpdateColorImage(device.ImmediateContext, colorImage.DataIntPtr);
                UpdateDepthImage(device.ImmediateContext, depthImage.DataIntPtr);
            }

            //colorImage.Dispose();
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

        bool live = false;

        public void StartLive()
        {
            live = true;
            //new System.Threading.Thread(ColorCameraLoop).Start();
            new System.Threading.Thread(DepthCameraLoop).Start();
        }

        public void StopLive()
        {
            live = false;
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
}
