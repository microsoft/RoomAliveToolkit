using UnityEngine;

namespace RoomAliveToolkit
{
    /// <summary>
    /// Calibration data asset. This is a component of the scene where the XML calibration file is read that is acquired by the room calibration routines. 
    /// To obtain a Kinect calibration XML file see CalibrateEnsamble example in RoomAlive Toolkit ProCamCalibration
    /// https://github.com/Kinect/RoomAliveToolkit/tree/master/ProCamCalibration
    /// </summary>
    [AddComponentMenu("RoomAliveToolkit/RATCalibrationData")]
    [ExecuteInEditMode]
    public class RATCalibrationData : MonoBehaviour
    {
        /// <summary>
        /// Flag to signal whether calibration is loaded. 
        /// </summary>
        public bool IsLoaded { get { return loaded; } }
         
        /// <summary>
        /// XML file containing calibration data. 
        /// </summary>
        public TextAsset calibration = null;

        private ProjectorCameraEnsemble ensemble;
        private bool loaded = false;

        public void LoadAsset()
        {
            if(calibration!=null)
                ensemble = ProjectorCameraEnsemble.ReadCalibration(calibration.text);
            loaded = true;
        }

        void Update()
        {

        }

        public ProjectorCameraEnsemble GetEnsemble()
        {
            if (!loaded)
                LoadAsset();
            return ensemble;
        }

        public bool IsValid()
        {
            return GetEnsemble() != null;
        }

        void OnValidate()
        {
            if(IsValid())
            {
                RATKinectClient[] cameras = GetComponentsInChildren<RATKinectClient>();
                RATProjector[] projectors = GetComponentsInChildren<RATProjector>();

                foreach(RATKinectClient camera in cameras) 
                {
                    if (camera.calibrationData == null)
                        camera.calibrationData = this;
                    if(camera.calibrationData==this)
                    {
                        camera.LoadCalibrationData();
                    }
                }
                foreach (RATProjector projector in projectors)
                {
                    if (projector.calibrationData == null)
                        projector.calibrationData = this;
                    if (projector.calibrationData == this)
                    {
                        projector.LoadCalibrationData();
                    }
                }
            }
        }
    }
}


