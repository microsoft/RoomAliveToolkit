using UnityEngine;
using System.Collections;
using UnityEditor;

namespace RoomAliveToolkit
{
    /// <summary>
    /// A helper component that can be used to automatically setup main portions of the RoomAlive scene provided that there is a valid RATCalibrationData component loaded. 
    /// </summary>
    [AddComponentMenu("RoomAliveToolkit/RATSceneSetup")]
    [ExecuteInEditMode]
    [RequireComponent(typeof(RATCalibrationData))]
    public class RATSceneSetup : MonoBehaviour
    {
        /// <summary>
        /// (optional) The 3d model (GameObject) used to depict Kinect cameras in the scene.
        /// </summary>
        public GameObject kinectModel = null;
        /// <summary>
        /// (optional) The 3d model (GameObject) used to depict projectors in the scene.
        /// </summary>
        public GameObject projectorModel = null;

        protected RATCalibrationData calibrationData;

        public bool IsCalibrationDataValid()
        {
            if (calibrationData != null) return calibrationData.IsValid();
            else return false;
        }

        void Start()
        {
            calibrationData = GetComponent<RATCalibrationData>();
        }

        public void LoadDefault3DModels()
        {
            kinectModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoomAliveToolkit/Models/Kinect.obj");
            projectorModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoomAliveToolkit/Models/Projector.obj");
        }

        public void BuildSceneComponents()
        {
            if (calibrationData.IsValid())
            {
                ProjectorCameraEnsemble ensemble = calibrationData.GetEnsemble();

                foreach (ProjectorCameraEnsemble.Camera cam in ensemble.cameras)
                {
                    GameObject kinectGameObject = new GameObject("Kinect_" + cam.name);
                    kinectGameObject.transform.parent = transform;
                    RATKinectClient kinect = kinectGameObject.AddComponent<RATKinectClient>();

                    kinect.calibrationData = calibrationData;
                    kinect.nameInConfiguration = cam.name;
                    kinect.UpdateFromCalibrationData();


                    GameObject deptMeshGameObject = new GameObject("DepthMesh");
                    deptMeshGameObject.transform.parent = kinectGameObject.transform;
                    deptMeshGameObject.AddComponent<RATDepthMesh>();
                    RATDepthMesh dm = deptMeshGameObject.GetComponent<RATDepthMesh>();
                    dm.kinectClient = kinect;
                    Shader s = Shader.Find("RoomAlive/DepthMeshSurfaceShader");
                    dm.surfaceMaterial = new Material(s);
                    deptMeshGameObject.transform.localPosition = Vector3.zero;
                    deptMeshGameObject.transform.localRotation = Quaternion.identity;

                    //this is purely for visualization purposes
                    if (kinectModel != null)
                    {
                        GameObject model = Instantiate(kinectModel);
                        model.name = "Kinect3DModel";
                        model.transform.parent = kinectGameObject.transform;
                        model.transform.localPosition = Vector3.zero;
                        model.transform.localRotation = Quaternion.identity;
                    }

                }
                foreach (ProjectorCameraEnsemble.Projector proj in ensemble.projectors)
                {
                    GameObject projectorGameObject = new GameObject("Projector_" + proj.name);
                    //Instantiate(projectorGameObject);
                    projectorGameObject.transform.parent = transform;

                    Camera cam = projectorGameObject.AddComponent<Camera>();
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.black;
                    cam.cullingMask = 0;     //should likely be set to render only the real world

                    RATProjector projrend = projectorGameObject.AddComponent<RATProjector>();
                    projrend.calibrationData = calibrationData;
                    projrend.nameInConfiguration = proj.name;
                    projrend.LoadCalibrationData();


                    //uncomment this if you want to add the option of Dynamic Masking the projection output
                    //projectorGameObject.AddComponent<RATDynamicMask>();

                    //this is purely for visualization purposes
                    if (projectorModel != null)
                    {
                        GameObject model = Instantiate(projectorModel);
                        model.name = "Projector3DModel";
                        model.transform.parent = projectorGameObject.transform;
                        model.transform.localPosition = Vector3.zero;
                        model.transform.localRotation = Quaternion.identity;
                    }

                }

                //add projection manager and setup appropriate viewports
                RATProjectionManager projManager = transform.gameObject.AddComponent<RATProjectionManager>();
                projManager.FindProjectionsDepthMeshesAndUsers();
                int n = ensemble.projectors.Count;
                float dx = 1f / n;
                projManager.screenViewports = new Rect[ensemble.projectors.Count];
                for (int i = 0; i < n; i++)
                    projManager.screenViewports[i] = new Rect(i * dx, 0, dx, 1); // this is a default configuration and it needs to be edited manually if the displays are aranged differently

                int cnt = 0;
                foreach (RATProjector proj in projManager.projections)
                {
                    proj.GetComponent<Camera>().rect = projManager.screenViewports[cnt++];
                }
            }
        }

        /// <summary>
        /// Very destructive operation. Use with caution!   
        /// </summary>
        public void ClearSceneComponents()
        {
            var klist = transform.GetComponentsInChildren<RATKinectClient>(true);
            foreach(RATKinectClient k in klist)
            {
                DestroyImmediate(k.transform.gameObject);
            }

            var plist = transform.GetComponentsInChildren<RATProjector>(true);
            foreach (RATProjector p in plist)
            {
                DestroyImmediate(p.transform.gameObject);
            }

            RATProjectionManager projManager = GetComponent<RATProjectionManager>();
            if (projManager != null) DestroyImmediate(projManager);
        }

        public void AddUser() //helper method that sets up a simple User with projection passes for both Surface and Depth 
        {
            GameObject userGO = new GameObject("User");
            userGO.transform.parent = transform;
            RATUser user = userGO.AddComponent<RATUser>();
            RATUserViewCamera userView = userGO.AddComponent<RATUserViewCamera>();
            RATProjectionPass pass1 = userGO.AddComponent<RATProjectionPass>();
            pass1.SetDefaultStaticShaders();
            RATProjectionPass pass2 = userGO.AddComponent<RATProjectionPass>();
            pass2.SetDefaultDynamicShaders();

            user.skeletonProvider = transform.GetComponentInChildren<RATKinectClient>(true);
            RATProjectionPass[] layers = new RATProjectionPass[2];
            layers[0] = pass1;
            layers[1] = pass2;
            userView.projectionLayers = layers;

        }

        void Update()
        {

        }
    }
}


