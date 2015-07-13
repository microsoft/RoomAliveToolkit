using System;
using Microsoft.Kinect;

namespace RoomAliveToolkit
{
    public class CoordinateMappingExample
    {
        /// <summary>
        /// Demonstrates how to use our Kinect calibration to convert a depth image point to color image coordinates.
        /// </summary>
        /// <param name="calibration"></param>
        /// <param name="kinectSensor"></param>
        public void Run(Kinect2Calibration calibration, KinectSensor kinectSensor)
        {
            this.calibration = calibration;
            this.kinectSensor = kinectSensor;
            depthImage = new ShortImage(Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);
            depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
            depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;
        
        }

        DepthFrameReader depthFrameReader;
        ShortImage depthImage;
        Kinect2Calibration calibration;
        KinectSensor kinectSensor;

        void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            var depthFrame = e.FrameReference.AcquireFrame();
            if (depthFrame != null)
            {
                using (depthFrame)
                {
                    depthFrame.CopyFrameDataToIntPtr(depthImage.DataIntPtr, Kinect2Calibration.depthImageWidth*Kinect2Calibration.depthImageHeight*2);

                    // convert depth image coords to color image coords
                    int x = 100, y = 100;
                    ushort depthImageValue = depthImage[x, y]; // depth image values are in mm

                    if (depthImageValue == 0)
                    {
                        Console.WriteLine("Sorry, depth value input coordinates is zero");
                        return;
                    }

                    float depth = (float)depthImageValue / 1000f; // convert to m, to match our calibration and the rest of the Kinect SDK
                    double colorX, colorY;
                    calibration.DepthImageToColorImage(x, y, depth, out colorX, out colorY);

                    //// when converting many points, it may be faster to precompute pass in the distortion table:
                    //var depthFrameToCameraSpaceTable = calibration.ComputeDepthFrameToCameraSpaceTable();
                    //calibration.DepthImageToColorImage(x, y, depth, depthFrameToCameraSpaceTable, out colorX, out colorY);

                    Console.WriteLine("our color coordinates: {0} {1}", colorX, colorY);

                    // compare to Kinect SDK
                    var depthSpacePoint = new DepthSpacePoint();
                    depthSpacePoint.X = x;
                    depthSpacePoint.Y = y;
                    var colorSpacePoint = kinectSensor.CoordinateMapper.MapDepthPointToColorSpace(depthSpacePoint, depthImageValue);
                    Console.WriteLine("SDK's color coordinates: {0} {1}", colorSpacePoint.X, colorSpacePoint.Y);

                    // convert back to depth image
                    Matrix depthPoint;
                    double depthX, depthY;

                    calibration.ColorImageToDepthImage(colorX, colorY, depthImage, out depthPoint, out depthX, out depthY);

                    //// when converting many points, it may be faster to precompute and pass in the distortion table:
                    //var colorFrameToCameraSapceTable = calibration.ComputeColorFrameToCameraSpaceTable();
                    //calibration.ColorImageToDepthImage((int)colorX, (int)colorY, depthImage, colorFrameToCameraSapceTable, out depthPoint, out depthX, out depthY);

                    Console.WriteLine("convert back to depth: {0} {1}", depthX, depthY);
                }
            }

        
        
        }


    }
}
