using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Vision;
using Kinect2;

namespace ProjectorServer
{
    [ServiceContract]
    public interface IProjectorServer
    {
        [OperationContract]
        Matrix GetMatrix();

    }
}
