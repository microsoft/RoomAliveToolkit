using System;
using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;
using RoomAliveToolkit;

namespace KinectV2Server
{
    public class ObjFile
    {

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
                return String.Format(CultureInfo.InvariantCulture, "v {0:0.0000} {1:0.0000} {2:0.0000}\r\nvt {3:0.0000} {4:0.0000}", x, y, z, u, v);
            }
        }


        public static void SaveColorToJPEG(string filePath, ARGBImage rgbImage)
        {

            BitmapSource srcImage = BitmapSource.Create(
               rgbImage.Width,
               rgbImage.Height,
               96,
               96,
               System.Windows.Media.PixelFormats.Bgra32,
               null,
               rgbImage.DataIntPtr,
               rgbImage.Width * rgbImage.Height * 4,
               rgbImage.Width * 4);

            // Convert the color frame to RGB24 pixel format
            FormatConvertedBitmap image = new FormatConvertedBitmap();
            image.BeginInit();
            image.Source = srcImage;
            image.DestinationFormat = System.Windows.Media.PixelFormats.Rgb24;
            image.EndInit();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                // Add the frame to the encoder.
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(memoryStream);

                File.WriteAllBytes(filePath, memoryStream.ToArray());
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="objPath"></param>
        /// <param name="depthImage"></param>
        /// <param name="rgbImage"></param>
        /// <param name="pose">Optional transformation pose (4x4 Matrix). Supply Identity by default</param>
        public static void Save(string objPath, Kinect2Calibration calibration, ShortImage depthImage, string colorImageFileName, Matrix pose)
        {
            var objFilename = Path.GetFileNameWithoutExtension(objPath);
            var objDirectory = Path.GetDirectoryName(objPath);

            if (!Directory.Exists(objDirectory))
                Directory.CreateDirectory(objDirectory);

            //copy the background color image to file
            //SaveColorToJPEG(objDirectory + "/" + objFilename + ".jpg", rgbImage);
            if (File.Exists(colorImageFileName))
            {
                File.Copy(colorImageFileName, objDirectory + "/" + objFilename + ".jpg", true);
            }
            else
            {
                Console.WriteLine("Saving to OBJ Error! File " + colorImageFileName + " doesn't exists!");
            }

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

            var streamWriter = new StreamWriter(objDirectory + "/" + objFilename + ".obj");
            var mtlFileWriter = new StreamWriter(objDirectory + "/" + objFilename + ".mtl");
            streamWriter.WriteLine("mtllib " + objFilename + ".mtl");
            uint nextVertexIndex = 1;
            //var depthImage = new FloatImage(Kinect2Calibration.depthImageWidth, Kinect2Calibration.depthImageHeight);


            mtlFileWriter.WriteLine("newmtl camera0");
            mtlFileWriter.WriteLine("Ka 1.000000 1.000000 1.000000");
            mtlFileWriter.WriteLine("Kd 1.000000 1.000000 1.000000");
            mtlFileWriter.WriteLine("Ks 0.000000 0.000000 0.000000");
            mtlFileWriter.WriteLine("Tr 1.000000");
            mtlFileWriter.WriteLine("illum 1");
            mtlFileWriter.WriteLine("Ns 0.000000");
            mtlFileWriter.WriteLine("map_Kd " + objFilename + ".jpg");


            streamWriter.WriteLine("usemtl camera0");

            // load depth image
            //string cameraDirectory = directory + "/camera" + camera.name;
            //depthImage.LoadFromFile(cameraDirectory + "/mean.bin");

            //var calibration = camera.calibration;
            var depthFrameToCameraSpaceTable = calibration.ComputeDepthFrameToCameraSpaceTable();
            var vertices = new Vertex[Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight];
            var colorCamera = new Matrix(4, 1);
            var depthCamera = new Matrix(4, 1);
            var world = new Matrix(4, 1);

            for (int y = 0; y < Kinect2Calibration.depthImageHeight; y++)
                for (int x = 0; x < Kinect2Calibration.depthImageWidth; x++)
                {
                    // depth camera coords
                    var depth = depthImage[x, y] / 1000f; // m
                    // convert to depth camera space
                    var point = depthFrameToCameraSpaceTable[Kinect2Calibration.depthImageWidth * y + x];
                    depthCamera[0] = point.X * depth;
                    depthCamera[1] = point.Y * depth;
                    depthCamera[2] = depth;
                    depthCamera[3] = 1;

                    // world coordinates
                    world.Mult(pose, depthCamera);
                    //world.Scale(1.0 / world[3]); not necessary for this transform

                    // convert to color camera space
                    colorCamera.Mult(calibration.depthToColorTransform, depthCamera);
                    colorCamera.Scale(1.0 / colorCamera[3]);

                    // project to color image
                    double colorU, colorV;
                    CameraMath.Project(calibration.colorCameraMatrix, calibration.colorLensDistortion, colorCamera[0], colorCamera[1], colorCamera[2], out colorU, out colorV);
                    colorU /= (double)Kinect2Calibration.colorImageWidth;
                    colorV /= (double)Kinect2Calibration.colorImageHeight;

                    var vertex = new Vertex();
                    vertex.x = (float)world[0];
                    vertex.y = (float)world[1];
                    vertex.z = (float)world[2];
                    vertex.u = (float)colorU;
                    vertex.v = (float)colorV;
                    vertices[Kinect2Calibration.depthImageWidth * y + x] = vertex;

                }

            streamWriter.WriteLine("g camera0");
            streamWriter.WriteLine("usemtl camera0");

            // examine each triangle
            for (int y = 0; y < Kinect2Calibration.depthImageHeight - 1; y++)
                for (int x = 0; x < Kinect2Calibration.depthImageWidth - 1; x++)
                {
                    int offseti = 0;
                    for (int tri = 0; tri < 2; tri++)
                    {
                        // the indexes of the vertices of this triangle
                        var i0 = Kinect2Calibration.depthImageWidth * (y + quadOffsets[offseti].Y) + (x + quadOffsets[offseti].X);
                        var i1 = Kinect2Calibration.depthImageWidth * (y + quadOffsets[offseti + 1].Y) + (x + quadOffsets[offseti + 1].X);
                        var i2 = Kinect2Calibration.depthImageWidth * (y + quadOffsets[offseti + 2].Y) + (x + quadOffsets[offseti + 2].X);

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
            
            streamWriter.Close();
            mtlFileWriter.Close();
        }
    }
}
