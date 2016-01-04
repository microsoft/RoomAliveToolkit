using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using System;
using System.IO;

namespace RoomAliveToolkit
{
    public class EnsembleDeviceResources
    {
        public EnsembleDeviceResources(SharpDX.Direct3D11.Device device, ProjectorCameraEnsemble ensemble, string directory, Object renderLock)
        {
            var colorImageTextureDesc = new Texture2DDescription()
            {
                Width = Kinect2Calibration.colorImageWidth,
                Height = Kinect2Calibration.colorImageHeight,
                MipLevels = 0,
                ArraySize = ensemble.cameras.Count,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps
            };
            colorImageTexture = new Texture2D(device, colorImageTextureDesc);
            colorImageTextureRV = new ShaderResourceView(device, colorImageTexture);

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

            colorImageTextureRVs = new ShaderResourceView[ensemble.cameras.Count];


            foreach (var camera in ensemble.cameras)
            {
                var colorImage = new ARGBImage(Kinect2Calibration.colorImageWidth, Kinect2Calibration.colorImageHeight);
                ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, colorImage, directory + "/camera" + camera.name + "/color.tiff");

                //var depthImage = new ShortImage(Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);
                //ProjectorCameraEnsemble.LoadFromTiff(imagingFactory, depthImage, directory + "/camera" + camera.name + "/mean.tiff");

                //depthImage[0] = 0;

                int arraySlice = ensemble.cameras.IndexOf(camera);

                var shaderResourceViewDesc = new ShaderResourceViewDescription()
                {
                    Format = Format.B8G8R8A8_UNorm,
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DArray,
                    Texture2DArray = new ShaderResourceViewDescription.Texture2DArrayResource()
                    {
                        ArraySize = 1,
                        FirstArraySlice = arraySlice,
                        MipLevels = -1,
                    }
                };

                colorImageTextureRVs[arraySlice] = new ShaderResourceView(device, colorImageTexture, shaderResourceViewDesc);

                lock (renderLock) // necessary?
                {
                    UpdateColorImage(device.ImmediateContext, colorImage.DataIntPtr, arraySlice);
                    //UpdateDepthImage(device.ImmediateContext, depthImage.DataIntPtr);
                }

                colorImage.Dispose();
                //depthImage.Dispose();
            }


        }


        public Texture2D colorImageTexture;
        public ShaderResourceView colorImageTextureRV;
        public Texture2D colorImageStagingTexture;
        ImagingFactory imagingFactory = new ImagingFactory();
        ShaderResourceView[] colorImageTextureRVs;


        public void UpdateColorImage(DeviceContext deviceContext, IntPtr colorImage, int arraySlice)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(colorImageStagingTexture, 0,
                MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.WriteRange(colorImage, Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 4);
            deviceContext.UnmapSubresource(colorImageStagingTexture, 0);

            int mipSize;
            int subresourceIndex = colorImageTexture.CalculateSubResourceIndex(0, arraySlice, out mipSize);
            var resourceRegion = new ResourceRegion()
            {
                Left = 0,
                Top = 0,
                Right = Kinect2Calibration.colorImageWidth,
                Bottom = Kinect2Calibration.colorImageHeight,
                Front = 0,
                Back = 1,
            };
            deviceContext.CopySubresourceRegion(colorImageStagingTexture, 0, resourceRegion, colorImageTexture, subresourceIndex);

            deviceContext.GenerateMips(colorImageTextureRVs[arraySlice]);
        }

    }
}
