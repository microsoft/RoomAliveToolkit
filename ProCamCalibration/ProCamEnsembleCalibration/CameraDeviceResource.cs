using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.IO;

namespace RoomAliveToolkit
{
    public class CameraDeviceResource : IDisposable
    {
        // encapsulates d3d resources for a camera
        public CameraDeviceResource(SharpDX.Direct3D11.Device device, ProjectorCameraEnsemble.Camera camera, int cameraIndex, Object renderLock, string directory, SharpDX.Direct3D11.Buffer vertexBuffer, SharpDX.Direct3D11.Buffer indexBuffer)
        {
            this.device = device;
            this.camera = camera;
            this.cameraIndex = cameraIndex;
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


            // depthFrameToCameraSpaceTable
            var depthFrameToCameraSpaceTableTextureDesc = new Texture2DDescription()
            {
                Width = Kinect2Calibration.depthImageWidth,
                Height = Kinect2Calibration.depthImageHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.R32G32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Immutable,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
            };
            var table = camera.calibration.ComputeDepthFrameToCameraSpaceTable();
            var dataStream = new DataStream(Kinect2Calibration.depthImageHeight * Kinect2Calibration.depthImageWidth * 4 * 2, true, true);
            for (int y = 0; y < Kinect2Calibration.depthImageHeight; y++)
                for (int x = 0; x < Kinect2Calibration.depthImageWidth; x++)
                {
                    var point = table[Kinect2Calibration.depthImageWidth * y + x];
                    dataStream.Write(point.X);
                    dataStream.Write(point.Y);
                }
            var dataBox = new DataBox(dataStream.DataPointer);
            dataBox.RowPitch = Kinect2Calibration.depthImageWidth * 4 * 2;
            depthFrameToCameraSpaceTableTexture = new Texture2D(device, depthFrameToCameraSpaceTableTextureDesc, new DataBox[] { dataBox });
            depthFrameToCameraSpaceTableTextureRV = new ShaderResourceView(device, depthFrameToCameraSpaceTableTexture);

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

            // Views of vertex and index buffers that correspond to this camera's vertices;
            // have a separate view into the vertex and index buffers, but could have single view and do index math to hit the right
            // segmenet; we already are doing math with offsets to determine index value
            var vertexBufferUAVDesc = new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.R32_Typeless,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 3 * cameraIndex, // pick out this camera's segment
                    ElementCount = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 3,
                    Flags = UnorderedAccessViewBufferFlags.Raw,
                }
            };
            vertexBufferUAV = new UnorderedAccessView(device, vertexBuffer, vertexBufferUAVDesc);
            var indexBufferUAVDesc = new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Format = Format.R32_Typeless,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    FirstElement = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6 * cameraIndex, // pick out this camera's segment
                    ElementCount = Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6,
                    Flags = UnorderedAccessViewBufferFlags.Raw,
                }
            };
            indexBufferUAV = new UnorderedAccessView(device, indexBuffer, indexBufferUAVDesc);


            if (File.Exists(directory + "/camera" + camera.name + "/color.tiff")) // FIX: this assumes that mean.tiff is exists (very likely)
            {
                var colorImage = new ARGBImage(Kinect2Calibration.colorImageWidth, Kinect2Calibration.colorImageHeight);
                ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, colorImage, directory + "/camera" + camera.name + "/color.tiff");

                var depthImage = new ShortImage(Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);
                ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, depthImage, directory + "/camera" + camera.name + "/mean.tiff");

                depthImage[0] = 0;

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

        SharpDX.Direct3D11.Device device;
        public Texture2D depthImageTexture, floatDepthImageTexture, floatDepthImageTexture2;
        public ShaderResourceView depthImageTextureRV, floatDepthImageRV, floatDepthImageRV2;
        public RenderTargetView floatDepthImageRenderTargetView, floatDepthImageRenderTargetView2;
        public Texture2D colorImageTexture;
        public ShaderResourceView colorImageTextureRV;
        public Texture2D colorImageStagingTexture;
        public Texture2D depthFrameToCameraSpaceTableTexture;
        public ShaderResourceView depthFrameToCameraSpaceTableTextureRV;
        public UnorderedAccessView vertexBufferUAV, indexBufferUAV;
        ProjectorCameraEnsemble.Camera camera;
        public int cameraIndex;
        public bool renderEnabled = true;

        public void Dispose()
        {
            depthImageTexture.Dispose();
            depthImageTextureRV.Dispose();
            colorImageTexture.Dispose();
            colorImageTextureRV.Dispose();
            colorImageStagingTexture.Dispose();
            depthFrameToCameraSpaceTableTexture.Dispose();
            depthFrameToCameraSpaceTableTextureRV.Dispose();
            vertexBufferUAV.Dispose();
            indexBufferUAV.Dispose();
        }

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
            depthImage[0] = 0; // for compute shader
            depthImage[1] = 0; // for compute shader

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
            while (live)
            {
                nextDepthData = camera.Client.LatestDepthImage();
                //lock (remoteDepthData)
                //    Swap<byte[]>(ref remoteDepthData, ref nextRemoteDepthData);
                lock (renderLock)
                {
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
