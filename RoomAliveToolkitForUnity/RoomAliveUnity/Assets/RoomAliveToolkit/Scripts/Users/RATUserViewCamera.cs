using UnityEngine;


namespace RoomAliveToolkit
{
    public enum ViewDebugMode
    {
        None, RGB//, Depth
    }
    [AddComponentMenu("RoomAliveToolkit/RATUserViewCamera")]
    /// <summary>
    /// Unity Camera for rendering the user's view in view-dependent projection mapping scenarios.
    /// 
    /// The logic behind the RoomAlive User Views explained:
    /// 
    /// Let's assume that there are at least 4 different layers in the scene that will control what objects are
    /// visible from which camera (user or projector)
    /// 
    /// Create 4 layers in your scene:
    /// - VirtualTextures - for virtual objects that should be texture mapped onto existing surfaces
    /// - Virtual3DObjects - for virtual 3D objects that could be perspective mapped
    /// - StaticSurfaces - for existing static room geometry that is loaded from a file(obj mesh for example)
    /// - DynamicSurfaces- for dynamic depth meshes that represent the physical space
    /// 
    /// In RATProjectionManager set Texture Layers to be "VirtualTextures". These are view independent and therefore do not need to be rendered in user's views. 
    /// 
    /// In this component, select Virtual3DObjects as VirtualObjectMask (layer mask). This will render only the virtual (view-dependent) 3D objects for each user. 
    /// 
    /// However, it is also important to account for real world geometry to correctly occlude the virtual objects. To accomplish that, use RATProjectionPass components and set them up for each
    /// type of real world objects in the scene. The most common situations are for static (OBJ meshes captured during scene calibration) and dynamic objects (Kinect depth meshes)
    /// 
    /// To each RATUserViewCamera add a component RATProjectionPass for each physical layer that you want to projecto on:
    /// select: StaticSurfaces For TargetSurfaceLayer 
    /// Press on "Set Static Defaults" button
    ///
    /// select: DynamicSurfaces For TargetSurfaceLayer
    /// Press on "Set Dynamic Defaults" button
    /// 
    /// </summary>
    public class RATUserViewCamera : MonoBehaviour
    {
        public RATProjectionManager projectionManager;

        [ReadOnly]
        public RenderTexture targetRGBTexture;


        [Space(10)]
        
        [Space(10)]
        public float fieldOfView = 90;
        public float nearClippingPlane = 0.1f;
        public float farClippingPlane = 8f;
        public LayerMask virtualObjectsMask; //select only the layers you want to see in the user's view
          
        [Space(10)]
        public ViewDebugMode debugPlane = ViewDebugMode.RGB;
        /// <summary>
        /// the size of the debug view plane visible in the scene view
        /// </summary>
        [Range(0.1f,3)]
        public float debugPlaneSize = 0.1f; 

        [Space(10)]
        public Color backgroundColor = new Color(0, 0, 0, 0);
        public Color realSurfaceColor = new Color(0, 0, 0, 0);

        public RATProjectionPass[] projectionLayers;

        public Camera viewCamera
        {
            get { return cam; }
        }

        protected int texWidth = 2048; //width of the off-screen render texture for this user view (needs to be power of 2)
        protected int texHeight = 2048;//height of the off-screen render texture for this user view (needs to be power of 2)

        protected Mesh debugPlaneM;
        protected MeshFilter meshFilter;
        protected MeshRenderer meshRenderer;
        protected Material meshMat;

        protected int[] indices = new int[] { 0,1,2, 3,2,1};
        protected Vector2[] uv = new Vector2[] { new Vector2(0,1),new Vector2(1,1),new Vector2(0,0),new Vector2(1,0) };
        protected Vector3[] pos = new Vector3[4];

        protected bool initialized = false;
        protected GameObject cameraGO;
        protected Camera cam;
        protected Rect rectReadRT;
        protected RATDepthMesh[] depthMeshes;


        public bool hasManager
        {
            get
            {
                return projectionManager != null && projectionManager.isActiveAndEnabled;
            }
        }

        void Awake()
        {
            projectionLayers = gameObject.GetComponents<RATProjectionPass>();

            foreach (RATProjectionPass layer in projectionLayers)
                layer.Init();
        }

        void Start()
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            Shader unlitShader = Shader.Find("Unlit/Texture");
            meshMat = new Material(unlitShader);
            debugPlaneM = new Mesh();
            meshFilter.hideFlags = HideFlags.HideInInspector;
            meshRenderer.hideFlags = HideFlags.HideInInspector;
            meshMat.hideFlags = HideFlags.HideInInspector;

            if (projectionManager==null)
                projectionManager = GameObject.FindObjectOfType<RATProjectionManager>();
            if(projectionManager!=null)
                projectionManager.RegisterUser(this);

            //Code assumes that this script is added to the camera GO 
            cameraGO = this.gameObject;

            cam = this.GetComponent<Camera>();
            if (cam == null)
                cam = gameObject.AddComponent<Camera>();
            cam.hideFlags = HideFlags.HideInInspector;  // | HideFlags.HideInHierarchy

            cam.rect = new Rect(0, 0, 1, 1);
            cam.enabled = false; //important to disable this camera as we will be calling Render() directly. 
            cam.aspect = texWidth / texHeight;

            cameraGO.transform.localPosition = new Vector3();

            targetRGBTexture = new RenderTexture(texWidth, texHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
            targetRGBTexture.filterMode = FilterMode.Trilinear;
            targetRGBTexture.autoGenerateMips = true;
            targetRGBTexture.depth = 24;
            targetRGBTexture.Create();

            rectReadRT = new Rect(0, 0, texWidth, texHeight);

            depthMeshes = GameObject.FindObjectsOfType<RATDepthMesh>();

            initialized = true;
        }

        public void Update()
        {
            // this mostly updates the little debug view in the scene editor view
            if (debugPlaneSize < 0)
                debugPlaneSize = 0;

            cam.nearClipPlane = nearClippingPlane;
            cam.farClipPlane = farClippingPlane;
            cam.fieldOfView = fieldOfView;

            meshRenderer.enabled = debugPlane != ViewDebugMode.None;
            if (meshRenderer.enabled)
            {
                //meshMat.mainTexture = debugPlane == ViewDebugMode.RGB?targetRGBTexture:targetDepthTexture;
                meshMat.mainTexture = targetRGBTexture;
                meshRenderer.sharedMaterial = meshMat;

                float z = debugPlaneSize<= nearClippingPlane ? nearClippingPlane:debugPlaneSize;
                float fac = Mathf.Tan(cam.fieldOfView / 2 / 180f * Mathf.PI);
                float w = z * fac;
                float h = z * fac;
                pos[0] = new Vector3(-w, h, nearClippingPlane);
                pos[1] = new Vector3(w, h, nearClippingPlane);
                pos[2] = new Vector3(-w, -h, nearClippingPlane);
                pos[3] = new Vector3(w, -h, nearClippingPlane);
                debugPlaneM.vertices = pos;
                debugPlaneM.uv = uv;
                debugPlaneM.triangles = indices;
                meshFilter.mesh = debugPlaneM;
            }
        }


        public void LateUpdate()
        {
            if (!initialized)
                return;

            RenderUserView();

            // Projection mapping rendering is actually done by each of the projector cameras
            // Setup things for the last pass which will be rendered from the perspective of the projectors (i.e., Render Pass 3)
            // this "pass" doesn't  do any rendering at this point, but merely sets the correct shaders/materials on all 
            // physical objects in the scene. 
        }

        /// <summary>
        /// Render both virtual and physical objects together from the perspective of the user
        /// </summary>
        public void RenderUserView()
        {
            cam.cullingMask = virtualObjectsMask;
            cam.backgroundColor = backgroundColor;
            cam.targetTexture = targetRGBTexture;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.Render();

            cam.clearFlags = CameraClearFlags.Nothing;

            foreach (RATProjectionPass layer in projectionLayers)
            {
                if (layer.renderUserView && layer.userViewShader != null && layer.enabled)
                {
                    cam.cullingMask = layer.targetSurfaceLayers;
                    Shader.SetGlobalColor("_ReplacementColor", realSurfaceColor);

                    cam.RenderWithShader(layer.userViewShader, null);
                    
                }
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        public virtual void RenderProjection(Camera camera)
        {
            RATProjectionPass[] layers = projectionLayers;
            for (int layerId=0; layerId < layers.Length; layerId++) {
                RATProjectionPass layer = layers[layerId];
                if (layer == null || !layer.enabled || layer.projectionShader==null || !layer.renderProjectionPass)
                    continue;
                camera.cullingMask = layer.targetSurfaceLayers;

                //todo preload IDs
                Shader.SetGlobalVector("_UserViewPos", this.cam.transform.position);
                Shader.SetGlobalTexture("_UserViewPointRGB", targetRGBTexture);
                //Shader.SetGlobalTexture("_UserViewPointDepth", targetDepthTexture);
                Shader.SetGlobalMatrix("_UserVP", this.cam.projectionMatrix * this.cam.worldToCameraMatrix);
                camera.RenderWithShader(layer.projectionShader, null);
            }
        }
    }
}
