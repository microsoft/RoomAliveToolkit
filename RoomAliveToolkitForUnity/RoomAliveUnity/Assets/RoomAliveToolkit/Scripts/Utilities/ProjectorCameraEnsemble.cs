using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using UnityEngine;

namespace RoomAliveToolkit
{ 
    [DataContract]
    public class ProjectorCameraEnsemble
    {
        [DataMember]
        public string name;
        [DataMember]
        public List<Camera> cameras;
        [DataMember]
        public List<Projector> projectors;

        [DataContract]
        public class Projector
        {
            public Projector()
            {
                pose = new Matrix(4, 4);
                pose.Identity();
                cameraMatrix = new Matrix(3,3);
                cameraMatrix.Identity();
                lensDistortion = new Matrix(2, 1);
            }

            [DataMember]
            public string name = "Untitled";
            [DataMember]
            public string hostNameOrAddress = "localhost";
            //public string uri = "net.tcp://localhost:9001/ProjectorServer/service";
            [DataMember]
            public int displayIndex = 0;

            [DataMember]
            public int width, height;
            [DataMember]
            public Matrix cameraMatrix;
            [DataMember]
            public Matrix lensDistortion;
            [DataMember]
            public Matrix pose;
            [DataMember]
            public bool lockIntrinsics;
        }

        [DataContract]
        public class Camera
        {
            public Camera()
            {
                pose = new Matrix(4, 4);
                pose.Identity();
                calibration = new Kinect2Calibration();
            }
            [DataMember]
            public string name = "Untitled";
            [DataMember]
            public string hostNameOrAddress = "localhost";
            //public string uri = "net.tcp://localhost:9000/KinectServer2/service";
            [DataMember]
            public Matrix pose;
            [DataMember]
            public Kinect2Calibration calibration;

            [DataContract]
            public class Kinect2Calibration
            {
                public Kinect2Calibration()
                {
                    colorCameraMatrix = new Matrix(3, 3);
                    colorLensDistortion = new Matrix(5, 1);
                    depthCameraMatrix = new Matrix(3, 3);
                    depthLensDistortion = new Matrix(5, 1);
                    depthToColorTransform = new Matrix(4, 4);
                }

                public const int depthImageWidth = 512;
                public const int depthImageHeight = 424;
                public const int colorImageWidth = 1920;
                public const int colorImageHeight = 1080;

                [DataMember]
                public Matrix colorCameraMatrix;
                [DataMember]
                public Matrix colorLensDistortion;
                [DataMember]
                public Matrix depthCameraMatrix;
                [DataMember]
                public Matrix depthLensDistortion;
                [DataMember]
                public Matrix depthToColorTransform;
            }
        }


        public ProjectorCameraEnsemble()
        {

        }

        public void Save(string filename)
        {
            var serializer = new XmlSerializer(typeof(ProjectorCameraEnsemble));
            var writer = new StreamWriter(filename);
            serializer.Serialize(writer, this);
            writer.Close();
        }

        public static ProjectorCameraEnsemble ReadCalibration(string calibrationString)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(calibrationString);
            MemoryStream stream = new MemoryStream(byteArray);

            return ReadCalibration(stream);
        }

        public static ProjectorCameraEnsemble ReadCalibration(Stream stream)
        {
            ProjectorCameraEnsemble room = null;
            try
            {
                var knownTypeList = new List<Type>();
                //knownTypeList.Add(typeof(Matrix));
                knownTypeList.Add(typeof(ProjectorCameraEnsemble.Camera.Kinect2Calibration));
                var serializer = new DataContractSerializer(typeof(ProjectorCameraEnsemble), knownTypeList);
                room = (ProjectorCameraEnsemble)serializer.ReadObject(stream);
                stream.Close();

                //var serializer = new XmlSerializer(typeof(ProjectorCameraEnsemble));
                ////var fileStream = new FileStream(filename, FileMode.Open);
                //room = (ProjectorCameraEnsemble)serializer.Deserialize(stream);
                //stream.Close();
            }
            catch (Exception e)
            {
                Debug.LogError("Error loading configuration file: " + e.Message);
            }

            return room;
        }

        public static ProjectorCameraEnsemble Load(string filename)
        {
            ProjectorCameraEnsemble room = null;
            try
            {
                /*
                var serializer = new XmlSerializer(typeof(ProjectorCameraEnsemble));
                var fileStream = new FileStream(filename, FileMode.Open);
                room = (ProjectorCameraEnsemble)serializer.Deserialize(fileStream);
                fileStream.Close();
                */

                room = ReadCalibration(new FileStream(filename, FileMode.Open));

                // testing
                {
                    /*
					 * var outputFilestream = new FileStream(filename + ".out.xml", FileMode.Create);
					serializer.WriteObject(outputFilestream, room);
					outputFilestream.Close();
					*/

                    /*var outKnownTypeList = new List<Type>();
                    knownTypeList.Add(typeof(Kinect2Calibration));
                    var outSerializer = new DataContractSerializer(typeof(ProjectorCameraEnsemble), knownTypeList);
                    var settings = new XmlWriterSettings { Indent = true };
                    using (var writer = XmlWriter.Create(filename + ".out.xml", settings))
                        outSerializer.WriteObject(writer, room);*/
                }

            }
            catch (Exception e)
            {
                Debug.LogError("Error loading configuration file: " + e.Message);
            }

            return room;
        }

    }

}
