using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RoomAliveToolkit
{
    [DataContract]
    public partial class ProjectorCameraEnsemble
    {
        [DataMember]
        public string name;
        [DataMember]
        public List<Camera> cameras;
        [DataMember]
        public List<Projector> projectors;

        [DataContract]
        public partial class Camera
        {
            [DataMember]
            public string name;
            [DataMember]
            public string hostNameOrAddress;
            [DataMember]
            public Matrix pose;
            [DataMember]
            public Kinect2Calibration calibration;
        }

        [DataContract]
        public partial class Projector
        {
            [DataMember]
            public string name;
            [DataMember]
            public string hostNameOrAddress;
            [DataMember]
            public int displayIndex;
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
    }
}
