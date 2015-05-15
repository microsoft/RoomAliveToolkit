using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Xml.Serialization;

namespace RoomAliveToolkit
{
    public class ProjectorCameraEnsemble
    {
        public string name;
        public List<Camera> cameras;
        public List<Projector> projectors;

        public class Camera
        {
            public string name;
            public string hostNameOrAddress;
            public Matrix pose;
            public Kinect2.Kinect2Calibration calibration;

            [XmlIgnore]
            public List<Matrix> colorImagePoints;
            [XmlIgnore]
            public List<Matrix> depthCameraPoints;
            [XmlIgnore]
            public KinectServer2Client Client
            {
                get
                {
                    if ((client == null) || (client.InnerChannel.State != CommunicationState.Opened))
                    {
                        var binding = new NetTcpBinding();
                        binding.MaxReceivedMessageSize = 8295424;
                        binding.Security.Mode = SecurityMode.None;
                        var uri = "net.tcp://" + hostNameOrAddress + ":9000/KinectServer2/service";
                        var address = new EndpointAddress(uri);
                        client = new KinectServer2Client(binding, address);
                        try
                        {
                            client.Open();
                        }
                        catch (EndpointNotFoundException e)
                        {
                            client = null;
                            Console.WriteLine("could not connect to Kinect server '{0}' at '{1}'", name, hostNameOrAddress);
                            throw e;
                        }
                    }
                    return client;
                }
            }
            private KinectServer2Client client;
        }

        public class Projector
        {
            public string name;
            public string hostNameOrAddress;
            public int displayIndex;
            public int width, height;
            public Matrix cameraMatrix;
            public Matrix lensDistortion;
            public Matrix pose;

            [XmlIgnore]
            public Dictionary<Camera, CalibrationPointSet> calibrationPointSets;

            [XmlIgnore]
            public ProjectorServerClient Client
            {
                get
                {
                    if ((client == null) || (client.InnerChannel.State != CommunicationState.Opened))
                    {
                        var binding = new NetTcpBinding();
                        binding.Security.Mode = SecurityMode.None;
                        var uri = "net.tcp://" + hostNameOrAddress + ":9001/ProjectorServer/service";
                        var address = new EndpointAddress(uri);
                        client = new ProjectorServerClient(binding, address);
                        try
                        {
                            client.Open();
                        }
                        catch (EndpointNotFoundException e)
                        {
                            client = null;
                            Console.WriteLine("could not connect to projector server '{0}' at '{1}'", name, hostNameOrAddress);
                            throw e;
                        }
                    }
                    return client;
                }
            }
            private ProjectorServerClient client;
        }

        [Serializable()]
        public class CalibrationFailedException : System.Exception
        {
            public CalibrationFailedException() : base() { }
            public CalibrationFailedException(string message) : base(message) { }
            public CalibrationFailedException(string message, System.Exception inner) : base(message, inner) { }

            // A constructor is needed for serialization when an
            // exception propagates from a remoting server to the client. 
            protected CalibrationFailedException(System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context) { }
        }


        public class CalibrationPointSet
        {
            public List<Matrix> worldPoints = new List<Matrix>();
            public List<System.Drawing.PointF> imagePoints = new List<System.Drawing.PointF>();
            public List<Matrix> worldPointInliers = new List<Matrix>();
            public List<System.Drawing.PointF> imagePointInliers = new List<System.Drawing.PointF>();
            public Matrix pose;
        }

        public ProjectorCameraEnsemble() { }

        public ProjectorCameraEnsemble(int numProjectors, int numCameras)
        {
            projectors = new List<ProjectorCameraEnsemble.Projector>();
            for (int i = 0; i < numProjectors; i++)
            {
                var projector = new ProjectorCameraEnsemble.Projector();
                projector.name = i.ToString();
                projector.hostNameOrAddress = "localhost";
                projectors.Add(projector);
            }
            cameras = new List<ProjectorCameraEnsemble.Camera>();
            for (int i = 0; i < numCameras; i++)
            {
                var camera = new ProjectorCameraEnsemble.Camera();
                camera.name = i.ToString();
                camera.hostNameOrAddress = "localhost";
                cameras.Add(camera);

                if (i == 0)
                    camera.pose = RoomAliveToolkit.Matrix.Identity(4, 4);
            }
            name = "Untitled";
        }

        public static ProjectorCameraEnsemble FromFile(string filename)
        {
            var serializer = new XmlSerializer(typeof(ProjectorCameraEnsemble));
            var fileStream = new FileStream(filename, FileMode.Open);
            var room = (ProjectorCameraEnsemble)serializer.Deserialize(fileStream);
            fileStream.Close();
            return room;
        }

        public void Save(string filename)
        {
            var serializer = new XmlSerializer(typeof(ProjectorCameraEnsemble));
            var writer = new StreamWriter(filename);
            serializer.Serialize(writer, this);
            writer.Close();
        }


        // maybe better somewhere else:
        const int depthWidth = 512;
        const int depthHeight = 424;
        const int colorWidth = 1920;
        const int colorHeight = 1080;

        public void CaptureGrayCodes(string directory)
        {
            // for each projector
            //    for each gray code
            //       display gray code
            //       for each camera (fork?)
            //          capture color image; save to file

            // store as projector#/camera#/grayCode#

            // foreach camera
            //   get calibration 
            //   save depth map to file

            var grayImage = new ByteImage(colorWidth, colorHeight);

            // pick up projector image dimensions form server and save them in configuration
            // put up projector's name on each
            foreach (var projector in projectors)
            {
                var size = projector.Client.Size(projector.displayIndex);
                projector.width = size.Width;
                projector.height = size.Height;
                projector.Client.OpenDisplay(projector.displayIndex);
                projector.Client.DisplayName(projector.displayIndex, projector.name);
            }

            // let AGC settle
            System.Threading.Thread.Sleep(2000);

            // save an image with projector name displayed, useful for later for visualization of results
            foreach (var camera in cameras)
            {
                string cameraDirectory = directory + "/camera" + camera.name;
                if (!Directory.Exists(cameraDirectory))
                    Directory.CreateDirectory(cameraDirectory);
                //var jpegBytes = camera.client.LatestJPEGImage();
                //File.WriteAllBytes(cameraDirectory + "/projectorLabels.jpg", jpegBytes);
                var colorBytes = camera.Client.LatestRGBImage();
                var image = new ARGBImage(colorWidth, colorHeight);
                Marshal.Copy(colorBytes, 0, image.DataIntPtr, colorWidth * colorHeight * 4);
                SaveToTiff(imagingFactory, image, cameraDirectory + "/projectorLabels.tiff");
                image.Dispose();
            }

            // TODO: consider combining with later aquiring color and depth


            // set all projectors to black
            foreach (var projector in projectors)
                projector.Client.SetColor(projector.displayIndex, 0, 0, 0);


            foreach (var projector in projectors)
            {
                string projectorDirectory = directory + "/projector" + projector.name;
                if (!Directory.Exists(projectorDirectory))
                    Directory.CreateDirectory(projectorDirectory);

                int numberOfGrayCodeImages = projector.Client.NumberOfGrayCodeImages(projector.displayIndex);

                // set display to gray, to give AGC a chance to settle
                projector.Client.SetColor(projector.displayIndex, 0.5f, 0.5f, 0.5f);
                System.Threading.Thread.Sleep(1500);


                for (int i = 0; i < numberOfGrayCodeImages; i++)
                {
                    projector.Client.DisplayGrayCode(projector.displayIndex, i);

                    // wait for the image to be displayed and give camera AGC time to settle
                    System.Threading.Thread.Sleep(500);

                    // TODO: parallelize?
                    foreach (var camera in cameras)
                    {
                        string cameraDirectory = projectorDirectory + "/camera" + camera.name;
                        if (!Directory.Exists(cameraDirectory))
                            Directory.CreateDirectory(cameraDirectory);

                        //// acquire color frames until exposure and gain have settled to a stable value
                        //int numUnchanging = 0;
                        //long lastExposureTime = 0;
                        //float lastGain = 0;
                        //const int numUnchangingThreshold = 5;
                        //byte[] colorImageBytes = null;
                        //while (numUnchanging < numUnchangingThreshold)
                        //{
                        //    colorImageBytes = camera.client.NextColorImage(); // consider providing a way of getting color exposure etc. w/o calling NextColorImage
                        //    long exposureTime = camera.client.LastColorExposureTimeTicks();
                        //    float gain = camera.client.LastColorGain();
                        //    if ((gain == lastGain) && (exposureTime == lastExposureTime))
                        //        numUnchanging++;
                        //    lastGain = gain;
                        //    lastExposureTime = exposureTime;
                        //}

                        var colorImageBytes = camera.Client.LatestYUVImage();

                        // convert YUY2 to grayscale
                        for (int y = 0; y < colorHeight; y++)
                            for (int x = 0; x < colorWidth; x++)
                                grayImage[x, y] = colorImageBytes[2 * (colorWidth * y + x)];

                        // save to file
                        SaveToTiff(imagingFactory, grayImage, cameraDirectory + "/grayCode" + i + ".tiff");
                    }
                }
                projector.Client.SetColor(projector.displayIndex, 0, 0, 0);
            }

            // close all displays
            foreach (var projector in projectors)
            {
                projector.Client.CloseDisplay(projector.displayIndex);
            }
        }

        public void CaptureDepthAndColor(string directory)
        {
            // foreach camera:
            // average a bunch of frames to find a good depth image
            // get calibration
            // TODO: parallelize

            foreach (var camera in cameras)
            {
                string cameraDirectory = directory + "/camera" + camera.name;
                if (!Directory.Exists(cameraDirectory))
                    Directory.CreateDirectory(cameraDirectory);

                // compute mean and variance of depth image
                var sum = new FloatImage(depthWidth, depthHeight);
                sum.Zero();
                var sumSquared = new FloatImage(depthWidth, depthHeight);
                sumSquared.Zero();
                var count = new ShortImage(depthWidth, depthHeight);
                count.Zero();
                var depth = new ShortImage(depthWidth, depthHeight);
                for (int i = 0; i < 100; i++)
                {
                    var depthBytes = camera.Client.LatestDepthImage();
                    Marshal.Copy(depthBytes, 0, depth.DataIntPtr, depthWidth * depthHeight * 2);
                    Console.WriteLine("acquired depth image " + i);
                    for (int y = 0; y < depthHeight; y++)
                        for (int x = 0; x < depthWidth; x++)
                            if (depth[x, y] != 0)
                            {
                                ushort d = depth[x, y];
                                count[x, y]++;
                                sum[x, y] += d;
                                sumSquared[x, y] += d * d;
                            }
                }

                var meanImage = new FloatImage(depthWidth, depthHeight);
                meanImage.Zero(); // not all pixels will be assigned
                var varianceImage = new FloatImage(depthWidth, depthHeight);
                varianceImage.Zero(); // not all pixels will be assigned

                for (int y = 0; y < depthHeight; y++)
                    for (int x = 0; x < depthWidth; x++)
                    {
                        if (count[x, y] > 50)
                        {
                            float mean = sum[x, y] / count[x, y];
                            meanImage[x, y] = mean;
                            float variance = sumSquared[x, y] / count[x, y] - mean * mean;
                            varianceImage[x, y] = variance;
                        }
                    }

                // WIC doesn't support encoding float tiff images, so for now we write to a binary file
                meanImage.SaveToFile(cameraDirectory + "/mean.bin");
                varianceImage.SaveToFile(cameraDirectory + "/variance.bin");

                // create a short version that we can write, used only for debugging
                var meanDepthShortImage = new ShortImage(depthWidth, depthHeight);
                for (int y = 0; y < depthHeight; y++)
                    for (int x = 0; x < depthWidth; x++)
                        meanDepthShortImage[x, y] = (ushort)meanImage[x, y];
                SaveToTiff(imagingFactory, meanDepthShortImage, cameraDirectory + "/mean.tiff");

                // convert to world coordinates and save to ply file
                camera.calibration = camera.Client.GetCalibration();
                var depthFrameToCameraSpaceTable = camera.calibration.ComputeDepthFrameToCameraSpaceTable();
                var world = new Float3Image(depthWidth, depthHeight); // TODO: move out/reuse
                for (int y = 0; y < depthHeight; y++)
                    for (int x = 0; x < depthWidth; x++)
                    {
                        var pointF = depthFrameToCameraSpaceTable[y * depthWidth + x];
                        Float3 worldPoint;
                        worldPoint.x = pointF.X * meanImage[x, y];
                        worldPoint.y = pointF.Y * meanImage[x, y];
                        worldPoint.z = meanImage[x, y];
                        world[x, y] = worldPoint;
                    }
                SaveToPly(cameraDirectory + "/mean.ply", world);

                // TODO: consider writing OBJ instead
            }

            // connect to projectors
            foreach (var projector in projectors)
            {
                //var binding = new NetTcpBinding();
                //binding.Security.Mode = SecurityMode.None;
                //var uri = "net.tcp://" + projector.hostNameOrAddress + ":9001/ProjectorServer/service";
                //var address = new EndpointAddress(uri);
                //projector.client = new ProjectorServerClient(binding, address);
                projector.Client.OpenDisplay(projector.displayIndex);
            }

            // collect color images when projecting all white and all black
            // set projectors to white
            foreach (var projector in projectors)
                projector.Client.SetColor(projector.displayIndex, 1f, 1f, 1f);
            System.Threading.Thread.Sleep(5000);
            foreach (var camera in cameras)
            {
                // save color image
                string cameraDirectory = directory + "/camera" + camera.name;
                var jpegBytes = camera.Client.LatestJPEGImage();
                File.WriteAllBytes(cameraDirectory + "/color.jpg", jpegBytes);
                var colorBytes = camera.Client.LatestRGBImage();
                var image = new ARGBImage(colorWidth, colorHeight);
                Marshal.Copy(colorBytes, 0, image.DataIntPtr, colorWidth * colorHeight * 4);
                SaveToTiff(imagingFactory, image, cameraDirectory + "/color.tiff");
                image.Dispose();

            }
            foreach (var projector in projectors)
                projector.Client.SetColor(projector.displayIndex, 0f, 0f, 0f);
            System.Threading.Thread.Sleep(5000);
            foreach (var camera in cameras)
            {
                // save color image
                string cameraDirectory = directory + "/camera" + camera.name;
                var jpegBytes = camera.Client.LatestJPEGImage();
                File.WriteAllBytes(cameraDirectory + "/colorDark.jpg", jpegBytes);
                var colorBytes = camera.Client.LatestRGBImage();
                var image = new ARGBImage(colorWidth, colorHeight);
                Marshal.Copy(colorBytes, 0, image.DataIntPtr, colorWidth * colorHeight * 4);
                SaveToTiff(imagingFactory, image, cameraDirectory + "/colorDark.tiff");
                image.Dispose();

            }

            // close all displays
            foreach (var projector in projectors)
            {
                projector.Client.CloseDisplay(projector.displayIndex);
            }

        }

        [XmlIgnore]
        public SharpDX.WIC.ImagingFactory2 imagingFactory = new SharpDX.WIC.ImagingFactory2();
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

        public void DecodeGrayCodeImages(string directory)
        {
            stopWatch.Start();


            // decode Gray code captures
            foreach (var projector in projectors)
            {
                string projectorDirectory = directory + "/projector" + projector.name;

                var grayCode = new GrayCode(projector.width, projector.height);

                // allocate space for captured images
                int nCapturedImages = 2 * (grayCode.numXBits + grayCode.numYBits); // varies by projector
                var capturedImages = new ByteImage[nCapturedImages];
                for (int i = 0; i < nCapturedImages; i++) // varies by projector
                    capturedImages[i] = new ByteImage(colorWidth, colorHeight);


                foreach (var camera in cameras)
                {
                    Console.WriteLine("decoding Gray code images for projector " + projector.name + ", camera " + camera.name);

                    string cameraDirectory = projectorDirectory + "/camera" + camera.name;

                    // load and decode Gray code images
                    for (int i = 0; i < nCapturedImages; i++)
                        LoadFromTiff(imagingFactory, capturedImages[i], cameraDirectory + "/grayCode" + i + ".tiff");

                    var decodedColumns = new ShortImage(colorWidth, colorHeight);
                    var decodedRows = new ShortImage(colorWidth, colorHeight);
                    var mask = new ByteImage(colorWidth, colorHeight);

                    // TODO: there are a couple of interesting thresholds in Decode; they should be surfaced here
                    grayCode.Decode(capturedImages, decodedColumns, decodedRows, mask);

                    //Console.WriteLine("saving camera " + camera.displayName);

                    SaveToTiff(imagingFactory, decodedColumns, cameraDirectory + "/decodedColumns.tiff");
                    SaveToTiff(imagingFactory, decodedRows, cameraDirectory + "/decodedRows.tiff");
                    SaveToTiff(imagingFactory, mask, cameraDirectory + "/mask.tiff");


                    var decodedColumnsMasked = new ShortImage(colorWidth, colorHeight);
                    var decodedRowsMasked = new ShortImage(colorWidth, colorHeight);

                    for (int y = 0; y < colorHeight; y++)
                        for (int x = 0; x < colorWidth; x++)
                        {
                            if (mask[x, y] > 0)
                            {
                                decodedColumnsMasked[x, y] = decodedColumns[x, y];
                                decodedRowsMasked[x, y] = decodedRows[x, y];
                            }
                            else
                            {
                                decodedColumnsMasked[x, y] = 0;
                                decodedRowsMasked[x, y] = 0;
                            }
                        }
                    SaveToTiff(imagingFactory, decodedColumnsMasked, cameraDirectory + "/decodedColumnsMasked.tiff");
                    SaveToTiff(imagingFactory, decodedRowsMasked, cameraDirectory + "/decodedRowsMasked.tiff");
                }
            }

            Console.WriteLine("elapsed time " + stopWatch.ElapsedMilliseconds);
        }

        public void CalibrateProjectorGroups(string directory)
        {
            // for all cameras, take depth image points to color image points
            var depthImage = new FloatImage(depthWidth, depthHeight);
            var varianceImage = new FloatImage(depthWidth, depthHeight);
            var validMask = new ByteImage(depthWidth, depthHeight);

            foreach (var camera in cameras)
            {
                Console.WriteLine("projecting depth points to color camera " + camera.name);

                // load depth image
                string cameraDirectory = directory + "/camera" + camera.name;
                depthImage.LoadFromFile(cameraDirectory + "/mean.bin");
                varianceImage.LoadFromFile(cameraDirectory + "/variance.bin");
                validMask.Zero();

                var calibration = camera.calibration;
                var depthFrameToCameraSpaceTable = calibration.ComputeDepthFrameToCameraSpaceTable();

                // TODO: consider using just one 4x4 in calibration class
                var colorCamera = new Matrix(4, 1);
                camera.colorImagePoints = new List<Matrix>();
                camera.depthCameraPoints = new List<Matrix>();
                var depthCamera4 = new Matrix(4, 1);

                // for each valid point in depth image
                int numRejected = 0;
                for (int y = 0; y < depthHeight; y += 1)
                    for (int x = 0; x < depthWidth; x += 1)
                    {
                        float depth = depthImage[x, y] / 1000f; // m
                        float variance = varianceImage[x, y];

                        if (depth == 0)
                            continue;
                        if (variance > 6 * 6)
                        {
                            numRejected++;
                            continue;
                        }
                        validMask[x, y] = (byte)255;

                        // convert to depth camera space
                        var point = depthFrameToCameraSpaceTable[y * depthWidth + x];
                        depthCamera4[0] = point.X * depth;
                        depthCamera4[1] = point.Y * depth;
                        depthCamera4[2] = depth;
                        depthCamera4[3] = 1;

                        // convert to color camera space
                        colorCamera.Mult(calibration.depthToColorTransform, depthCamera4);
                        //colorCamera.Scale(1.0 / colorCamera[3]);

                        // project to color image
                        double colorU, colorV;
                        CameraMath.Project(calibration.colorCameraMatrix, calibration.colorLensDistortion, colorCamera[0], colorCamera[1], colorCamera[2], out colorU, out colorV);

                        if ((colorU >= 0) && (colorU < (colorWidth - 1)) && (colorV >= 0) && (colorV < (colorHeight - 1))) // BEWARE: later do we round or truncate??
                        {
                            var colorImagePoint = new Matrix(2, 1);
                            colorImagePoint[0] = colorU;
                            colorImagePoint[1] = colorV;
                            camera.colorImagePoints.Add(colorImagePoint);

                            // expect a 3-vector?
                            var depthCamera = new Matrix(3, 1);
                            depthCamera[0] = depthCamera4[0];
                            depthCamera[1] = depthCamera4[1];
                            depthCamera[2] = depthCamera4[2];

                            camera.depthCameraPoints.Add(depthCamera);

                            //Console.WriteLine(depthCamera[0] + "\t" + depthCamera[1] + "\t -> " + colorImagePoint[0] + "\t" + colorImagePoint[1]);
                        }
                        
                    }
                SaveToTiff(imagingFactory, validMask, cameraDirectory + "/validMask.tiff");

                Console.WriteLine("rejected " + 100 * (float)numRejected / (float)(depthWidth * depthHeight) + "% pixels for high variance");

            }


            // we never save colorImagePoints, depthCameraPoints, so we must remember to run previous

            Console.WriteLine("elapsed time " + stopWatch.ElapsedMilliseconds);


            // use decoded Gray code images to create calibration point sets
            foreach (var projector in projectors)
            {
                string projectorDirectory = directory + "/projector" + projector.name;

                projector.calibrationPointSets = new Dictionary<Camera, CalibrationPointSet>();

                foreach (var camera in cameras)
                {
                    string cameraDirectory = projectorDirectory + "/camera" + camera.name;

                    var decodedColumns = new ShortImage(colorWidth, colorHeight);
                    var decodedRows = new ShortImage(colorWidth, colorHeight);
                    var mask = new ByteImage(colorWidth, colorHeight);

                    LoadFromTiff(imagingFactory, decodedColumns, cameraDirectory + "/decodedColumns.tiff");
                    LoadFromTiff(imagingFactory, decodedRows, cameraDirectory + "/decodedRows.tiff");
                    LoadFromTiff(imagingFactory, mask, cameraDirectory + "/mask.tiff");

                    // we have a bunch of color camera / depth camera point corrspondences
                    // use the Gray code to find the position of the color camera point in the projector frame

                    // find 2D projector coordinates from decoded Gray code images
                    var imagePoints = new List<System.Drawing.PointF>();
                    var worldPoints = new List<Matrix>();

                    for (int i = 0; i < camera.colorImagePoints.Count; i++)
                    {
                        var colorImagePoint = camera.colorImagePoints[i];

                        // We would like to relate projected color points to color images stored in memory. 
                        // The Kinect SDK and our camera calibration assumes X left, Y up (from the POV of the camera).
                        // We index images in memory with X right and Y down.
                        // Our Gray code images are flipped in the horizontal direction.
                        // Therefore to map an image space coordinate to a memory location we flip Y (and not X):
                        int x = (int)(colorImagePoint[0] + 0.5f);
                        int y = colorHeight - (int)(colorImagePoint[1] + 0.5f);

                        if ((x < 0) || (x >= colorWidth) || (y < 0) || (y >= colorHeight))
                        {
                            //Console.WriteLine("out of bounds");
                            continue;
                        }

                        if (mask[x, y] > 0) // Gray code is valid
                        {
                            // We would like to relate decoded row/column values to projector coordinates.
                            // To match the camera, we want projector's coordinate system X left, Y up (from the POV of the projector).
                            // We assume that the projector is configured in front projection mode (i.e., projected text looks correct in the real world).
                            // In that case decoded columns run X right (in the real world), decoded rows run Y down (in the real world).
                            // So we need to flip both X and Y decoded values.

                            var projectorImagePoint = new System.Drawing.PointF(projector.width - decodedColumns[x, y], projector.height - decodedRows[x, y]);
                            var depthCameraPoint = camera.depthCameraPoints[i];

                            imagePoints.Add(projectorImagePoint);
                            worldPoints.Add(depthCameraPoint);

                            //Console.WriteLine(depthCameraPoint[0] + "\t" + depthCameraPoint[1] + "\t" + depthCameraPoint[2] + "-> \t" + x + "\t" + y + "-> \t" + projectorImagePoint.X + "\t" + projectorImagePoint.Y);
                        }
                    }

                    if (worldPoints.Count > 1000)
                    {
                        var pointSet = new CalibrationPointSet();
                        pointSet.worldPoints = worldPoints;
                        pointSet.imagePoints = imagePoints;
                        projector.calibrationPointSets[camera] = pointSet;
                        Console.WriteLine("projector " + projector.name + " is seen by camera " + camera.name + " (" + worldPoints.Count + " points)");
                    }
                }
            }

            Console.WriteLine("elapsed time " + stopWatch.ElapsedMilliseconds);



            // calibration
            foreach (var projector in projectors)
            {
                Console.WriteLine("calibrating projector " + projector.name);

                string projectorDirectory = directory + "/projector" + projector.name;

                // RANSAC
                double minError = Double.PositiveInfinity;
                var random = new Random(0); // provide seed to ease debugging

                int numCompletedFits = 0;

                for (int i = 0; (numCompletedFits < 4) && (i < 10); i++)
                {
                    Console.WriteLine("RANSAC iteration " + i);

                    // randomly select small number of points from each calibration set
                    var worldPointSubsets = new List<List<Matrix>>();
                    var imagePointSubsets = new List<List<System.Drawing.PointF>>();

                    foreach (var pointSet in projector.calibrationPointSets.Values)
                    {
                        var worldPointSubset = new List<Matrix>();
                        var imagePointSubset = new List<System.Drawing.PointF>();

                        bool nonCoplanar = false;
                        int nTries = 0;

                        while (!nonCoplanar)
                        {
                            for (int j = 0; j < 100; j++)
                            {
                                int k = random.Next(pointSet.worldPoints.Count);
                                worldPointSubset.Add(pointSet.worldPoints[k]);
                                imagePointSubset.Add(pointSet.imagePoints[k]);
                            }

                            // check that points are not coplanar
                            Matrix X;
                            double D;
                            double ssdToPlane = PlaneFit(worldPointSubset, out X, out D);
                            int numOutliers = 0;
                            foreach (var point in worldPointSubset)
                            {
                                double distanceFromPlane = X.Dot(point) + D;
                                if (Math.Abs(distanceFromPlane) > 0.1f)
                                    numOutliers++;
                            }
                            nonCoplanar = (numOutliers > worldPointSubset.Count * 0.10f);
                            if (!nonCoplanar)
                            {
                                Console.WriteLine("points are coplanar (try #{0})", nTries);
                                worldPointSubset.Clear();
                                imagePointSubset.Clear();
                            }
                            if (nTries++ > 1000)
                            {
                                throw new CalibrationFailedException("Unable to find noncoplanar points.");
                                // consider moving this check up with variance check (when calibration point sets are formed)
                            }
                        }

                        worldPointSubsets.Add(worldPointSubset);
                        imagePointSubsets.Add(imagePointSubset);
                    }


                    var cameraMatrix = new Matrix(3, 3);
                    cameraMatrix[0, 0] = 1000; //fx TODO: can we instead init this from FOV?
                    cameraMatrix[1, 1] = 1000; //fy
                    cameraMatrix[0, 2] = projector.width / 2; //cx
                    cameraMatrix[1, 2] = 0; // projector lens shift; note this assumes desktop projection mode
                    cameraMatrix[2, 2] = 1;
                    var distCoeffs = new RoomAliveToolkit.Matrix(2, 1);
                    List<RoomAliveToolkit.Matrix> rotations = null;
                    List<RoomAliveToolkit.Matrix> translations = null;


                    var error = CalibrateCamera(worldPointSubsets, imagePointSubsets, cameraMatrix, ref rotations, ref translations);
                    Console.WriteLine("error = " + error);
                    //Console.WriteLine("intrinsics = \n" + cameraMatrix);



                    //// we differ from opencv's 'error' in that we do not distinguish between x and y.
                    //// i.e. opencv uses the method below; this number would match if we used pointsInSum2*2 in the divisor.
                    //// double check opencv's error
                    //{
                    //    double sumError2 = 0;
                    //    int pointsInSum2 = 0;
                    //    for (int ii = 0; ii < worldPointSubsets.Count; ii++)
                    //    {
                    //        var R = Orientation.Rodrigues(rotations[ii]);
                    //        var t = translations[ii];
                    //        var p = new Matrix(3, 1);

                    //        var worldPointSet = worldPointSubsets[ii];
                    //        var imagePointSet = imagePointSubsets[ii];

                    //        for (int k = 0; k < worldPointSet.Count; k++)
                    //        {
                    //            p.Mult(R, worldPointSet[k]);
                    //            p.Add(t);
                    //            double u, v;
                    //            Kinect2.Kinect2Calibration.Project(cameraMatrix, distCoeffs, p[0], p[1], p[2], out u, out v);

                    //            double dx = imagePointSet[k].X - u;
                    //            double dy = imagePointSet[k].Y - v;

                    //            double thisError = dx * dx + dy * dy;
                    //            sumError2 += thisError;
                    //            pointsInSum2++;
                    //        }
                    //    }

                    //    // opencv's error is rms but over both x and y combined

                    //    Console.WriteLine("average projection error = " + Math.Sqrt(sumError2 / (float)(pointsInSum2)));
                    //}


                    // find inliers from overall dataset
                    var worldPointInlierSets = new List<List<Matrix>>();
                    var imagePointInlierSets = new List<List<System.Drawing.PointF>>();
                    int setIndex = 0;

                    bool enoughInliers = true;
                    double sumError = 0;
                    int pointsInSum = 0;
                    foreach (var pointSet in projector.calibrationPointSets.Values)
                    {
                        var worldPointInlierSet = new List<Matrix>();
                        var imagePointInlierSet = new List<System.Drawing.PointF>();

                        //var R = Vision.Orientation.Rodrigues(rotations[setIndex]);
                        var R = RotationMatrixFromRotationVector(rotations[setIndex]);
                        var t = translations[setIndex];
                        var p = new Matrix(3, 1);


                        for (int k = 0; k < pointSet.worldPoints.Count; k++)
                        {
                            p.Mult(R, pointSet.worldPoints[k]);
                            p.Add(t);

                            double u, v;
                            CameraMath.Project(cameraMatrix, distCoeffs, p[0], p[1], p[2], out u, out v);

                            double dx = pointSet.imagePoints[k].X - u;
                            double dy = pointSet.imagePoints[k].Y - v;
                            double thisError = Math.Sqrt((dx * dx) + (dy * dy));

                            if (thisError < 1.0f)
                            {
                                worldPointInlierSet.Add(pointSet.worldPoints[k]);
                                imagePointInlierSet.Add(pointSet.imagePoints[k]);
                            }
                            sumError += thisError * thisError;
                            pointsInSum++;
                        }
                        setIndex++;

                        // require that each view has a minimum number of inliers
                        enoughInliers = enoughInliers && (worldPointInlierSet.Count > 1000);

                        worldPointInlierSets.Add(worldPointInlierSet);
                        imagePointInlierSets.Add(imagePointInlierSet);

                    }

                    // if number of inliers > some threshold (should be for each subset)
                    if (enoughInliers) // should this threshold be a function of the number of cameras, a percentage?
                    {
                        var error2 = CalibrateCamera(worldPointInlierSets, imagePointInlierSets, cameraMatrix, ref rotations, ref translations);

                        Console.WriteLine("error with inliers = " + error2);
                        Console.Write("camera matrix = \n" + cameraMatrix);

                        // if err < besterr save model (save rotation and translation to calibrationPointSets, cameraMatrix and distortion coeffs to projector)
                        if (error < minError)
                        {
                            minError = error;
                            projector.cameraMatrix = cameraMatrix;
                            projector.lensDistortion = distCoeffs;
                            setIndex = 0;
                            numCompletedFits++;

                            foreach (var pointSet in projector.calibrationPointSets.Values)
                            {
                                // convert to 4x4 transform
                                var R = RotationMatrixFromRotationVector(rotations[setIndex]);
                                var t = translations[setIndex];

                                var T = new Matrix(4, 4);
                                T.Identity();
                                for (int ii = 0; ii < 3; ii++)
                                {
                                    for (int jj = 0; jj < 3; jj++)
                                        T[ii, jj] = R[ii, jj];
                                    T[ii, 3] = t[ii];
                                }
                                pointSet.pose = T;
                                pointSet.worldPointInliers = worldPointInlierSets[setIndex];
                                pointSet.imagePointInliers = imagePointInlierSets[setIndex];

                                setIndex++;
                            }
                        }
                    }

                }

                if (numCompletedFits == 0)
                    throw new CalibrationFailedException("Unable to successfully calibrate projector: " + projector.name);
 
                Console.WriteLine("final calibration:");
                Console.Write("camera matrix = \n" + projector.cameraMatrix);
                Console.Write("distortion = \n" + projector.lensDistortion);
                Console.WriteLine("error = " + minError);

                foreach (var camera in projector.calibrationPointSets.Keys)
                {
                    Console.WriteLine("camera " + camera.name + " pose:");
                    Console.Write(projector.calibrationPointSets[camera].pose);
                }
            }

            Console.WriteLine("elapsed time " + stopWatch.ElapsedMilliseconds);
        }
        
        public void UnifyPose()
        {
            // unify extrinsics

            // greedily assign poses to projectors and cameras

            // The first camera is assumed to be in world coordinates already; its pose will not be modified.
            // In this way users can place the system in a useful world coordinate system that is external to calibration.

            // Set all camera poses except for the first to null. 
            for (int i = 1; i < cameras.Count; i++)
                cameras[i].pose = null;

            // Keep a list of all projectors that haven't been dealt with.
            var unfixed = new List<Projector>();
            unfixed.AddRange(projectors);

            // While "unfixed" is not empty
            while (unfixed.Count > 0)
            {
                // For each projector in "unfixed"
                Projector projectorToRemove = null;
                foreach (var projector in unfixed)
                {
                    // Is it associated with a camera that has its pose set?
                    Camera fixedCamera = null;
                    foreach (var camera in projector.calibrationPointSets.Keys)
                        if (camera.pose != null)
                        {
                            fixedCamera = camera;
                            break;
                        }

                    // If so, set pose of projector and all associated cameras; remove the projector from "unfixed" (be careful to remove outside loop)
                    if (fixedCamera != null)
                    {
                        // find pose of projector as concatenation of the fixed camera pose and pose from calibration
                        var T_CjStarW = fixedCamera.pose;
                        var T_CjStarPk = projector.calibrationPointSets[fixedCamera].pose;
                        var T_PkCjStar = new Matrix(4, 4);
                        T_PkCjStar.Inverse(T_CjStarPk);
                        var T_PkW = new Matrix(4, 4);
                        T_PkW.Mult(T_CjStarW, T_PkCjStar);
                        projector.pose = T_PkW;

                        // for all other cameras that do not have their pose set
                        foreach (var camera in projector.calibrationPointSets.Keys)
                        {
                            if (camera.pose == null)
                            {
                                // concatenate projector and local pose
                                var T_CjPk = projector.calibrationPointSets[camera].pose;
                                var T_CjW = new Matrix(4, 4);
                                T_CjW.Mult(T_PkW, T_CjPk);
                                camera.pose = T_CjW;
                            }
                        }

                        projectorToRemove = projector;
                        break;
                    }
                }
                unfixed.Remove(projectorToRemove); // if projectorToRemove null, graph is not fully connected?
            }

        }

        public void OptimizePose()
        {
            UnifyPose();

            // joint estimate of projector and camera pose

            // minimize wrt T_CjW, T_WPk:  Sum_ijk  v_ijk [ p_k( T_WPk T_CjW x_i ) - y_ik ]^2

            // cameras observe points x_i (in camera coords)
            // point x_i is observed to project to point y_ik in projector k
            // v_ijk === 1 if point i is observed by camera j and imaged by projector k
            // p_k(x) projects point x in projector k; x in projector coordinates
            // T_CjW camera j local coordinates to world coordinates
            // T_WPk world to projector k coorindates

            // efficient implementation: list of points x_ijk for which v_ijk != 0; store j, k with each point x_i
            // solve for C_j, P_k; C_0 is not in the set of parameters



            // parameters: for each projector and camera: 1 rotation + 1 translation = 6 parameters
            //    We leave T_C0W fixed, so have 6 * (numProjectors + numCameras - 1) parameters
            int nParameters = 6 * (projectors.Count + cameras.Count - 1);
            //double[] parameters = new double[nParameters];
            var parameters = new Matrix(nParameters, 1);

            // loop over room.cameras, room.projectors to form up parameters array
            {
                int pi = 0; // index into our parameter array
                for (int i = 1; i < cameras.Count; i++) // skip first one, which is our root
                {
                    var T = cameras[i].pose;
                    var R = new Matrix(3, 3);
                    var t = new Matrix(3, 1);
                    for (int ii = 0; ii < 3; ii++)
                    {
                        t[ii] = T[ii, 3];
                        for (int jj = 0; jj < 3; jj++)
                            R[ii, jj] = T[ii, jj];
                    }

                    //var r = Vision.Orientation.RotationVector(R);
                    var r = ProjectorCameraEnsemble.RotationVectorFromRotationMatrix(R);

                    for (int ii = 0; ii < 3; ii++)
                        parameters[pi++] = r[ii];
                    for (int ii = 0; ii < 3; ii++)
                        parameters[pi++] = t[ii];
                }

                for (int i = 0; i < projectors.Count; i++)
                {
                    var T = projectors[i].pose;
                    var R = new Matrix(3, 3);
                    var t = new Matrix(3, 1);
                    for (int ii = 0; ii < 3; ii++)
                    {
                        t[ii] = T[ii, 3];
                        for (int jj = 0; jj < 3; jj++)
                            R[ii, jj] = T[ii, jj];
                    }

                    //var r = Vision.Orientation.RotationVector(R);
                    var r = ProjectorCameraEnsemble.RotationVectorFromRotationMatrix(R);

                    for (int ii = 0; ii < 3; ii++)
                        parameters[pi++] = r[ii];
                    for (int ii = 0; ii < 3; ii++)
                        parameters[pi++] = t[ii];
                }
            }

            // count the number of values
            // use only inliers from previous step
            int nValues = 0;
            foreach (var projector in projectors)
                foreach (var camera in projector.calibrationPointSets.Keys)
                    nValues += projector.calibrationPointSets[camera].worldPointInliers.Count * 2; // count components


            LevenbergMarquardt.Function optimize = delegate(Matrix p)
            {
                var fvec = new Matrix(nValues, 1);


                // convert p to transforms etc.
                // convert back to transforms and put back in our structures
                int pi = 0; // index into our parameter array
                for (int i = 1; i < cameras.Count; i++) // skip first one, which is our root
                {
                    var r = new Matrix(3, 1);
                    r[0] = p[pi++];
                    r[1] = p[pi++];
                    r[2] = p[pi++];
                    //var R = Vision.Orientation.Rodrigues(r);
                    var R = RotationMatrixFromRotationVector(r);

                    var t = new Matrix(3, 1);
                    t[0] = p[pi++];
                    t[1] = p[pi++];
                    t[2] = p[pi++];

                    var T = new Matrix(4, 4);
                    T.Identity();
                    for (int ii = 0; ii < 3; ii++)
                    {
                        for (int jj = 0; jj < 3; jj++)
                            T[ii, jj] = R[ii, jj];
                        T[ii, 3] = t[ii];
                    }
                    cameras[i].pose = T;
                }

                for (int i = 0; i < projectors.Count; i++)
                {
                    var r = new Matrix(3, 1);
                    r[0] = p[pi++];
                    r[1] = p[pi++];
                    r[2] = p[pi++];
                    //var R = Vision.Orientation.Rodrigues(r);
                    var R = RotationMatrixFromRotationVector(r);

                    var t = new Matrix(3, 1);
                    t[0] = p[pi++];
                    t[1] = p[pi++];
                    t[2] = p[pi++];

                    var T = new Matrix(4, 4);
                    T.Identity();
                    for (int ii = 0; ii < 3; ii++)
                    {
                        for (int jj = 0; jj < 3; jj++)
                            T[ii, jj] = R[ii, jj];
                        T[ii, 3] = t[ii];
                    }
                    projectors[i].pose = T;
                }

                int fveci = 0; // index into our fvec array

                foreach (var projector in projectors)
                {
                    // T_WPk is inverse of T_PkW, projector pose
                    var T_WPk = new Matrix(4, 4);
                    T_WPk.Inverse(projector.pose);

                    foreach (var camera in projector.calibrationPointSets.Keys)
                    {
                        var cameraPoints = projector.calibrationPointSets[camera].worldPointInliers;
                        var projectorPoints = projector.calibrationPointSets[camera].imagePointInliers;

                        // transforms camera to projector coordinates
                        var T_CjW = camera.pose;
                        var T_CjPk = new Matrix(4, 4);
                        T_CjPk.Mult(T_WPk, T_CjW);

                        var cameraInProjector4 = new Matrix(4, 1);
                        cameraInProjector4[3] = 1;

                        var cameraPoint4 = new Matrix(4, 1);
                        cameraPoint4[3] = 1;

                        for (int i = 0; i < cameraPoints.Count; i++)
                        {
                            var cameraPoint = cameraPoints[i];

                            cameraPoint4[0] = cameraPoint[0];
                            cameraPoint4[1] = cameraPoint[1];
                            cameraPoint4[2] = cameraPoint[2];

                            cameraInProjector4.Mult(T_CjPk, cameraPoint4);

                            cameraInProjector4.Scale(1.0 / cameraInProjector4[3]);

                            // fvec_i = y_i - p_k( T_CjPk x_i );
                            double u, v;
                            CameraMath.Project(projector.cameraMatrix, projector.lensDistortion, cameraInProjector4[0], cameraInProjector4[1], cameraInProjector4[2], out u, out v);

                            var projectorPoint = projectorPoints[i];
                            fvec[fveci++] = projectorPoint.X - u;
                            fvec[fveci++] = projectorPoint.Y - v;
                        }
                    }
                }

                //double sum = 0;
                //for (int i = 0; i < nValues; i++)
                //    sum += fvec[i] * fvec[i];

                //double rms = Math.Sqrt(sum / (double)nValues);
                //Console.WriteLine("in functor, rms == " + rms);

                return fvec;

            };



            // TODO: maybe compute error before final optimization

            var calibrate = new LevenbergMarquardt(optimize);
            calibrate.minimumReduction = 1.0e-4;
            while (calibrate.State == LevenbergMarquardt.States.Running)
            {
                double rmsError = calibrate.MinimizeOneStep(parameters);
                Console.WriteLine("rms error = " + rmsError);
            }

            //for (int i = 0; i < nParameters; i++)
            //    Console.WriteLine(parameters[i] + "\t");
            //Console.WriteLine();


            // convert back to transforms and put back in our structures
            {
                int pi = 0; // index into our parameter array
                for (int i = 1; i < cameras.Count; i++) // skip first one, which is our root
                {
                    var r = new Matrix(3, 1);
                    r[0] = parameters[pi++];
                    r[1] = parameters[pi++];
                    r[2] = parameters[pi++];
                    //var R = Vision.Orientation.Rodrigues(r);
                    var R = RotationMatrixFromRotationVector(r);

                    var t = new Matrix(3, 1);
                    t[0] = parameters[pi++];
                    t[1] = parameters[pi++];
                    t[2] = parameters[pi++];

                    var T = new Matrix(4, 4);
                    T.Identity();
                    for (int ii = 0; ii < 3; ii++)
                    {
                        for (int jj = 0; jj < 3; jj++)
                            T[ii, jj] = R[ii, jj];
                        T[ii, 3] = t[ii];
                    }
                    cameras[i].pose = T;
                }

                for (int i = 0; i < projectors.Count; i++)
                {
                    var r = new Matrix(3, 1);
                    r[0] = parameters[pi++];
                    r[1] = parameters[pi++];
                    r[2] = parameters[pi++];
                    //var R = Vision.Orientation.Rodrigues(r);
                    var R = RotationMatrixFromRotationVector(r);

                    var t = new Matrix(3, 1);
                    t[0] = parameters[pi++];
                    t[1] = parameters[pi++];
                    t[2] = parameters[pi++];

                    var T = new Matrix(4, 4);
                    T.Identity();
                    for (int ii = 0; ii < 3; ii++)
                    {
                        for (int jj = 0; jj < 3; jj++)
                            T[ii, jj] = R[ii, jj];
                        T[ii, 3] = t[ii];
                    }
                    projectors[i].pose = T;
                }
            }

            Console.WriteLine("elapsed time " + stopWatch.ElapsedMilliseconds);

        }

        static double CalibrateCamera(List<List<Matrix>> worldPointSets, List<List<System.Drawing.PointF>> imagePointSets,
            Matrix cameraMatrix, ref List<Matrix> rotations, ref List<Matrix> translations)
        {
            int nSets = worldPointSets.Count;
            int nPoints = 0;

            for (int i = 0; i < nSets; i++)
                nPoints += worldPointSets[i].Count; // for later

            var distCoeffs = Matrix.Zero(2, 1);


            // if necessary run DLT on each point set to get initial rotation and translations
            if (rotations == null)
            {
                rotations = new List<Matrix>();
                translations = new List<Matrix>();

                for (int i = 0; i < nSets; i++)
                {
                    Matrix R, t;
                    CameraMath.DLT(cameraMatrix, distCoeffs, worldPointSets[i], imagePointSets[i], out R, out t);

                    //var r = Vision.Orientation.RotationVector(R);
                    var r = ProjectorCameraEnsemble.RotationVectorFromRotationMatrix(R);

                    rotations.Add(r);
                    translations.Add(t);
                }
            }

            // Levenberg-Marquardt for camera matrix (ignore lens distortion for now)

            // pack parameters into vector
            // parameters: camera has f, cx, cy; each point set has rotation + translation (6)
            int nParameters = 3 + 6 * nSets;
            var parameters = new Matrix(nParameters, 1);

            {
                int pi = 0;
                parameters[pi++] = cameraMatrix[0, 0]; // f
                parameters[pi++] = cameraMatrix[0, 2]; // cx
                parameters[pi++] = cameraMatrix[1, 2]; // cy
                for (int i = 0; i < nSets; i++)
                {
                    parameters[pi++] = rotations[i][0];
                    parameters[pi++] = rotations[i][1];
                    parameters[pi++] = rotations[i][2];
                    parameters[pi++] = translations[i][0];
                    parameters[pi++] = translations[i][1];
                    parameters[pi++] = translations[i][2];
                }
            }

            // size of our error vector
            int nValues = nPoints * 2; // each component (x,y) is a separate entry



            LevenbergMarquardt.Function function = delegate(Matrix p)
            {
                var fvec = new Matrix(nValues, 1);

                // unpack parameters
                int pi = 0;
                double f = p[pi++];
                double cx = p[pi++];
                double cy = p[pi++];

                var K = Matrix.Identity(3, 3);
                K[0, 0] = f;
                K[1, 1] = f;
                K[0, 2] = cx;
                K[1, 2] = cy;

                var d = Matrix.Zero(2, 1);

                int fveci = 0;

                for (int i = 0; i < nSets; i++)
                {
                    var rotation = new Matrix(3, 1);
                    rotation[0] = p[pi++];
                    rotation[1] = p[pi++];
                    rotation[2] = p[pi++];
                    //var R = Vision.Orientation.Rodrigues(rotation);
                    var R = RotationMatrixFromRotationVector(rotation);

                    var t = new Matrix(3, 1);
                    t[0] = p[pi++];
                    t[1] = p[pi++];
                    t[2] = p[pi++];

                    var worldPoints = worldPointSets[i];
                    var imagePoints = imagePointSets[i];
                    var x = new Matrix(3, 1);

                    for (int j = 0; j < worldPoints.Count; j++)
                    {
                        // transform world point to local camera coordinates
                        x.Mult(R, worldPoints[j]);
                        x.Add(t);

                        // fvec_i = y_i - f(x_i)
                        double u, v;
                        CameraMath.Project(K, d, x[0], x[1], x[2], out u, out v);

                        var imagePoint = imagePoints[j];
                        fvec[fveci++] = imagePoint.X - u;
                        fvec[fveci++] = imagePoint.Y - v;
                    }
                }
                return fvec;
            };

            // optimize
            var calibrate = new LevenbergMarquardt(function);
            calibrate.minimumReduction = 1.0e-4;
            calibrate.Minimize(parameters);

            //while (calibrate.State == LevenbergMarquardt.States.Running)
            //{
            //    var rmsError = calibrate.MinimizeOneStep(parameters);
            //    //Console.WriteLine("rms error = " + rmsError);
            //}
            //for (int i = 0; i < nParameters; i++)
            //    Console.WriteLine(parameters[i] + "\t");
            //Console.WriteLine();

            // unpack parameters
            {
                int pi = 0;
                double f = parameters[pi++];
                double cx = parameters[pi++];
                double cy = parameters[pi++];
                cameraMatrix[0, 0] = f;
                cameraMatrix[1, 1] = f;
                cameraMatrix[0, 2] = cx;
                cameraMatrix[1, 2] = cy;

                for (int i = 0; i < nSets; i++)
                {
                    rotations[i][0] = parameters[pi++];
                    rotations[i][1] = parameters[pi++];
                    rotations[i][2] = parameters[pi++];

                    translations[i][0] = parameters[pi++];
                    translations[i][1] = parameters[pi++];
                    translations[i][2] = parameters[pi++];
                }
            }

            return calibrate.RMSError;
        }

        public static List<RoomAliveToolkit.Matrix> TransformPoints(RoomAliveToolkit.Matrix A, List<RoomAliveToolkit.Matrix> points)
        {
            var transformedPoints = new List<RoomAliveToolkit.Matrix>();
            var point4 = new RoomAliveToolkit.Matrix(4, 1);
            point4[3] = 1;
            var transformedPoint4 = new RoomAliveToolkit.Matrix(4, 1);
            foreach (var point in points)
            {
                point4[0] = point[0]; point4[1] = point[1]; point4[2] = point[2];
                transformedPoint4.Mult(A, point4);
                transformedPoint4.Scale(1.0f / transformedPoint4[3]);
                var transformedPoint = new RoomAliveToolkit.Matrix(3, 1);
                transformedPoint[0] = transformedPoint4[0]; transformedPoint[1] = transformedPoint4[1]; transformedPoint[2] = transformedPoint4[2];
                transformedPoints.Add(transformedPoint);
            }
            return transformedPoints;
        }

        #region File I/O
        struct Vertex
        {
            public float x, y, z, u, v;
            public uint index;
            public static float DistanceSquared(Vertex a, Vertex b)
            {
                float dx = a.x - b.x; float dy = a.y - b.y; float dz = a.z - b.z;
                return dx * dx + dy * dy + dz * dz;
            }
            public override string ToString()
            {
                return String.Format("v {0:0.0000} {1:0.0000} {2:0.0000}\r\nvt {3:0.0000} {4:0.0000}", x, y, z, u, v);
            }
        }

        public void SaveToOBJ(string directory)
        {
            // force decimal point so standard programs like MeshLab can read this
            System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";

            var objDirectory = directory + "//obj";

            if (!Directory.Exists(objDirectory))
                Directory.CreateDirectory(objDirectory);

            // Because we need to form triangles, we go back to the depth image
            var quadOffsets = new System.Drawing.Point[]
            {
                new System.Drawing.Point(0, 0),
                new System.Drawing.Point(1, 0),
                new System.Drawing.Point(0, 1),
                new System.Drawing.Point(1, 0),
                new System.Drawing.Point(1, 1),
                new System.Drawing.Point(0, 1),
            };

            var streamWriter = new StreamWriter(objDirectory + "/ensemble.obj");
            var mtlFileWriter = new StreamWriter(objDirectory + "/ensemble.mtl");
            streamWriter.WriteLine("mtllib ensemble.mtl");
            uint nextVertexIndex = 1;
            var depthImage = new FloatImage(depthWidth, depthHeight);

            foreach (var camera in cameras)
            {
                mtlFileWriter.WriteLine("newmtl camera" + camera.name);
                mtlFileWriter.WriteLine("Ka 1.000000 1.000000 1.000000");
                mtlFileWriter.WriteLine("Kd 1.000000 1.000000 1.000000");
                mtlFileWriter.WriteLine("Ks 0.000000 0.000000 0.000000");
                mtlFileWriter.WriteLine("Tr 1.000000");
                mtlFileWriter.WriteLine("illum 1");
                mtlFileWriter.WriteLine("Ns 0.000000");
                mtlFileWriter.WriteLine("map_Kd camera" + camera.name + ".jpg");

                File.Copy(directory + "/camera" + camera.name + "/color.jpg", objDirectory + "//camera" + camera.name + ".jpg", true);
                
                streamWriter.WriteLine("usemtl camera" + camera.name);

                // load depth image
                string cameraDirectory = directory + "/camera" + camera.name;
                depthImage.LoadFromFile(cameraDirectory + "/mean.bin");

                var calibration = camera.calibration;
                var depthFrameToCameraSpaceTable = calibration.ComputeDepthFrameToCameraSpaceTable();
                var vertices = new Vertex[depthWidth * depthHeight];
                var colorCamera = new Matrix(4, 1);
                var depthCamera = new Matrix(4, 1);
                var world = new Matrix(4, 1);

                for (int y = 0; y < depthHeight; y++)
                    for (int x = 0; x < depthWidth; x++)
                    {
                        // depth camera coords
                        var depth = depthImage[x, y] / 1000f; // m
                        // convert to depth camera space
                        var point = depthFrameToCameraSpaceTable[depthWidth * y + x];
                        depthCamera[0] = point.X * depth;
                        depthCamera[1] = point.Y * depth;
                        depthCamera[2] = depth;
                        depthCamera[3] = 1;

                        // world coordinates
                        world.Mult(camera.pose, depthCamera);
                        //world.Scale(1.0 / world[3]); not necessary for this transform

                        // convert to color camera space
                        colorCamera.Mult(calibration.depthToColorTransform, depthCamera);
                        colorCamera.Scale(1.0 / colorCamera[3]);

                        // project to color image
                        double colorU, colorV;
                        CameraMath.Project(calibration.colorCameraMatrix, calibration.colorLensDistortion, colorCamera[0], colorCamera[1], colorCamera[2], out colorU, out colorV);
                        colorU /= (double)colorWidth;
                        colorV /= (double)colorHeight;

                        var vertex = new Vertex();
                        vertex.x = (float)world[0];
                        vertex.y = (float)world[1];
                        vertex.z = (float)world[2];
                        vertex.u = (float)colorU;
                        vertex.v = (float)colorV;
                        vertices[depthWidth * y + x] = vertex;

                    }

                streamWriter.WriteLine("g camera" + camera.name);
                streamWriter.WriteLine("usemtl camera" + camera.name);

                // examine each triangle
                for (int y = 0; y < depthHeight - 1; y++)
                    for (int x = 0; x < depthWidth - 1; x++)
                    {
                        int offseti = 0;
                        for (int tri = 0; tri < 2; tri++)
                        {
                            // the indexes of the vertices of this triangle
                            var i0 = depthWidth * (y + quadOffsets[offseti].Y) + (x + quadOffsets[offseti].X);
                            var i1 = depthWidth * (y + quadOffsets[offseti + 1].Y) + (x + quadOffsets[offseti + 1].X);
                            var i2 = depthWidth * (y + quadOffsets[offseti + 2].Y) + (x + quadOffsets[offseti + 2].X);

                            // is triangle valid?
                            bool nonZero = (vertices[i0].z != 0) && (vertices[i1].z != 0) && (vertices[i2].z != 0);

                            bool jump01 = Vertex.DistanceSquared(vertices[i0], vertices[i1]) < 0.2 * 0.2;
                            bool jump02 = Vertex.DistanceSquared(vertices[i0], vertices[i2]) < 0.2 * 0.2;
                            bool jump12 = Vertex.DistanceSquared(vertices[i1], vertices[i2]) < 0.2 * 0.2;

                            bool valid = nonZero && jump01 && jump02 && jump12;
                            if (valid)
                            {
                                // only add the vertex if we haven't already
                                if (vertices[i0].index == 0)
                                {
                                    streamWriter.WriteLine(vertices[i0]);
                                    vertices[i0].index = nextVertexIndex++;
                                }
                                if (vertices[i1].index == 0)
                                {
                                    streamWriter.WriteLine(vertices[i1]);
                                    vertices[i1].index = nextVertexIndex++;
                                }
                                if (vertices[i2].index == 0)
                                {
                                    streamWriter.WriteLine(vertices[i2]);
                                    vertices[i2].index = nextVertexIndex++;
                                }
                                streamWriter.WriteLine("f {0}/{0} {1}/{1} {2}/{2}", vertices[i0].index, vertices[i1].index, vertices[i2].index);
                            }
                            offseti += 3;
                        }
                    }
            }
            streamWriter.Close();
            mtlFileWriter.Close();
        }
   
        static public void LoadFromTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, UnmanagedImage image, string filename, int bytesPerPixel)
        {
            // TODO: this function is more generic; rewrite to handle different formats/bytesPerPixel
            var decoder = new SharpDX.WIC.BitmapDecoder(imagingFactory, filename, SharpDX.WIC.DecodeOptions.CacheOnLoad);
            var bitmapFrameDecode = decoder.GetFrame(0);
            bitmapFrameDecode.CopyPixels(image.Width * bytesPerPixel, image.DataIntPtr, image.Width * image.Height * bytesPerPixel);
            bitmapFrameDecode.Dispose();
            decoder.Dispose();
        }

        static public void LoadFromTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, ByteImage image, string filename)
        {
            LoadFromTiff(imagingFactory, image, filename, 1);
        }

        static public void LoadFromTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, ShortImage image, string filename)
        {
            LoadFromTiff(imagingFactory, image, filename, 2);
        }

        static public void LoadFromTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, ARGBImage image, string filename)
        {
            LoadFromTiff(imagingFactory, image, filename, 4);
        }

        static public void LoadFromTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, RGBImage image, string filename)
        {
            LoadFromTiff(imagingFactory, image, filename, 3);
        }

        static public void SaveToTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, UnmanagedImage image, string filename, Guid format, int bytesPerPixel)
        {
            var file = new System.IO.FileStream(filename, System.IO.FileMode.Create);
            var stream = new SharpDX.WIC.WICStream(imagingFactory, file);
            var encoder = new SharpDX.WIC.BitmapEncoder(imagingFactory, SharpDX.WIC.ContainerFormatGuids.Tiff);
            encoder.Initialize(stream);
            var bitmapFrameEncode = new SharpDX.WIC.BitmapFrameEncode(encoder);
            //bitmapFrameEncode.Options.TiffCompressionMethod = SharpDX.WIC.TiffCompressionOption.None;
            bitmapFrameEncode.Initialize();
            bitmapFrameEncode.SetSize(image.Width, image.Height);
            bitmapFrameEncode.SetPixelFormat(ref format);
            bitmapFrameEncode.WritePixels(image.Height, image.DataIntPtr, image.Width * bytesPerPixel);
            bitmapFrameEncode.Commit();
            encoder.Commit();
            bitmapFrameEncode.Dispose();
            encoder.Dispose();
            stream.Dispose();
            file.Close();
            file.Dispose();
        }

        static public void SaveToTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, ByteImage image, string filename)
        {
            SaveToTiff(imagingFactory, image, filename, SharpDX.WIC.PixelFormat.Format8bppGray, 1);
        }

        static public void SaveToTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, ShortImage image, string filename)
        {
            SaveToTiff(imagingFactory, image, filename, SharpDX.WIC.PixelFormat.Format16bppGray, 2);
        }

        static public void SaveToTiff(SharpDX.WIC.ImagingFactory2 imagingFactory, ARGBImage image, string filename)
        {
            SaveToTiff(imagingFactory, image, filename, SharpDX.WIC.PixelFormat.Format32bppRGBA, 4);
        }

        static public void SaveToPly(string filename, Float3Image pts3D)
        {
            // force decimal point so standard programs like MeshLab can read this
            System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename, false, Encoding.ASCII))
            {
                // Write Header
                file.WriteLine("ply");
                file.WriteLine("format ascii 1.0");
                file.WriteLine("comment VCGLIB generated");

                // Write Elements
                file.WriteLine("element vertex " + pts3D.Width * pts3D.Height);
                file.WriteLine("property float x\nproperty float y\nproperty float z");
                //file.WriteLine("element face 0");
                //file.WriteLine("property list uchar int vertex_indices");
                file.WriteLine("end_header");

                for (int r = 0; r < pts3D.Height; r++)
                {
                    for (int c = 0; c < pts3D.Width; c++)
                    {
                        Float3 xyz = pts3D[c, r];
                        if (xyz.z == float.NaN)
                        {
                            file.WriteLine("0 0 0");
                            continue;
                        }
                        file.WriteLine(xyz.x.ToString("0.000") + " " + xyz.y.ToString("0.000") + " " + xyz.z.ToString("0.000"));
                    }
                }
            }
        }
        #endregion




        public static Matrix RotationMatrixFromRotationVector(Matrix rotationVector)
        {
            double angle = rotationVector.Norm();
            var axis = new SharpDX.Vector3((float)(rotationVector[0] / angle), (float)(rotationVector[1] / angle), (float)(rotationVector[2] / angle));
            var sR = SharpDX.Matrix.RotationAxis(axis, -(float)angle); // TODO: why sign?

            var R = new Matrix(3, 3);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    R[i, j] = sR[i, j];
            return R;
        }

        public static Matrix RotationVectorFromRotationMatrix(Matrix R)
        {
            var sR = new SharpDX.Matrix();
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    sR[i, j] = (float) R[i, j];
            var q = SharpDX.Quaternion.RotationMatrix(sR);
            var sRotationVector = -q.Angle * q.Axis;

            var rotationVector = new Matrix(3, 1);
            for (int i = 0; i < 3; i++)
                rotationVector[i] = sRotationVector[i];
            return rotationVector;
        }

        static public double PlaneFit(IList<Matrix> points, out Matrix X, out double D)
        {
            X = new Matrix(3, 1);

            var mu = new RoomAliveToolkit.Matrix(3, 1);
            for (int i = 0; i < points.Count; i++)
                mu.Add(points[i]);
            mu.Scale(1f / (float)points.Count);

            var A = new RoomAliveToolkit.Matrix(3, 3);
            var pc = new RoomAliveToolkit.Matrix(3, 1);
            var M = new RoomAliveToolkit.Matrix(3, 3);
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                pc.Sub(p, mu);
                M.Outer(pc, pc);
                A.Add(M);
            }

            var V = new RoomAliveToolkit.Matrix(3, 3);
            var d = new RoomAliveToolkit.Matrix(3, 1);
            A.Eig(V, d); // TODO: replace with 3x3 version?

            //Console.WriteLine("------");
            //Console.WriteLine(A);
            //Console.WriteLine(V);
            //Console.WriteLine(d);

            double minEigenvalue = Double.MaxValue;
            int minEigenvaluei = 0;
            for (int i = 0; i < 3; i++)
                if (d[i] < minEigenvalue)
                {
                    minEigenvalue = d[i];
                    minEigenvaluei = i;
                }

            X.CopyCol(V, minEigenvaluei);

            D = -X.Dot(mu);

            // min eigenvalue is the sum of squared distances to the plane
            // signed distance is: double distance = X.Dot(point) + D;

            return minEigenvalue;
        }



        //[STAThread]
        //public static unsafe void Main()
        //{
        //    // save out a blank config file
        //    var newEnsemble = ProjectorCameraEnsemble.CreateNew(1, 1);
        //    newEnsemble.Save("ensemble.xml");

        //    string path = "calibration\\ensemble.xml";
        //    var ensemble = ProjectorCameraEnsemble.FromFile(path);
        //    string directory = Path.GetDirectoryName(path);

        //    //ensemble.ShowDisplayIndexes();
        //    //Console.ReadLine();
        //    //ensemble.HideDisplayIndexes();


        //    //ensemble.CaptureGrayCodes(directory);
        //    //ensemble.CaptureDepthAndColor(directory);
        //    //ensemble.DecodeGrayCodeImages(directory);
        //    //ensemble.CalibrateProjectorGroups(directory);
        //    //ensemble.OptimizePose();
        //    //ensemble.SaveToOBJ(directory);
        //    //ensemble.Save(path);

        //}


    }

}
