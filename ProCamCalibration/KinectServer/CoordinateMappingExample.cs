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
        public static void Run(Kinect2Calibration calibration, KinectSensor kinectSensor)
        {
            // depth image coordinates to convert
            int x = 100, y = 100;
            ushort depthImageValue = 2000; // depth image values are in mm
            float depth = (float) depthImageValue / 1000f; // convert to m, to match our calibration and the rest of the Kinect SDK

            double xUndistorted, yUndistorted;

            // convert to depth camera space
            float fx = (float)calibration.depthCameraMatrix[0, 0];
            float fy = (float)calibration.depthCameraMatrix[1, 1];
            float cx = (float)calibration.depthCameraMatrix[0, 2];
            float cy = (float)calibration.depthCameraMatrix[1, 2];
            float[] kappa = new float[] { (float)calibration.depthLensDistortion[0], (float)calibration.depthLensDistortion[1] };
            // flip y because our calibration expects y up (right handed coordinates at all times)
            CameraMath.Undistort(fx, fy, cx, cy, kappa, x, (Kinect2Calibration.depthImageHeight - y), out xUndistorted, out yUndistorted);

            //// convert to depth camera space
            //// use lookup table to perform undistortion; this will be faster when converting lots of points
            //// this matches the Kinect SDK's depthFrameToCameraSpace table (y down)
            //var depthFrameToCameraSpaceTable = calibration.ComputeDepthFrameToCameraSpaceTable();
            //var point = depthFrameToCameraSpaceTable[y * Kinect2Calibration.depthImageWidth + x];
            //xUndistorted = point.X;
            //yUndistorted = point.Y;

            var depthCamera = new Matrix(4, 1);
            depthCamera[0] = xUndistorted * depth;
            depthCamera[1] = yUndistorted * depth;
            depthCamera[2] = depth;
            depthCamera[3] = 1;

            // convert to color camera space
            var colorCamera = new Matrix(4, 1);
            colorCamera.Mult(calibration.depthToColorTransform, depthCamera);
            //colorCamera.Scale(1.0 / colorCamera[3]); // not necessary to divide by w in this case

            // project to color image
            double colorU, colorV;
            CameraMath.Project(calibration.colorCameraMatrix, calibration.colorLensDistortion, colorCamera[0], colorCamera[1], colorCamera[2], out colorU, out colorV);

            // convert back to Y down
            colorV = Kinect2Calibration.colorImageHeight - colorV;
            Console.WriteLine("our color coordinates: {0} {1}", colorU, colorV);

            // compare to Kinect SDK
            var depthSpacePoint = new DepthSpacePoint();
            depthSpacePoint.X = x;
            depthSpacePoint.Y = y;
            var colorPoints = new ColorSpacePoint[1];
            colorPoints[0] = new ColorSpacePoint();
            kinectSensor.CoordinateMapper.MapDepthPointsToColorSpace(new DepthSpacePoint[] { depthSpacePoint }, new ushort[] { depthImageValue }, colorPoints);

            Console.WriteLine("SDK's color coordinates: {0} {1}", colorPoints[0].X, colorPoints[0].Y);
        }

    }
}
