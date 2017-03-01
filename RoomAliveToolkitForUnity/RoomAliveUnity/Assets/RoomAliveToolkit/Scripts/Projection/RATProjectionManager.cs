using System.Collections.Generic;
using UnityEngine;


namespace RoomAliveToolkit {

    public enum ConfigOptions { Editor, MultiDisplay }

    /// <summary>
    /// Behavior that keeps track of various users, depth meshes, and projectors in the scene and allows for projection mapping to occur. 
    /// </summary>
    [AddComponentMenu("RoomAliveToolkit/RATProjectionManager")]
    public class RATProjectionManager : MonoBehaviour {

        public List<RATUserViewCamera> userViewCameras = new List<RATUserViewCamera>();
        public List<RATProjector> projections = new List<RATProjector>();
        public List<RATDepthMesh> depthMeshes = new List<RATDepthMesh>();
        public Color backgroundColor = Color.black;

        public ConfigOptions screenSetup = ConfigOptions.Editor;
        /// <summary>
        /// A set of viewport rectangles that describe which part of the Game window is taken by each projector image
        /// </summary>
        public Rect[] screenViewports = {
            new Rect(0,0,1,1)
        };

        public LayerMask textureLayers;
        public bool renderOnlyTextures = false;

        public Shader projPassShader { get; private set; }

        private ConfigOptions lstSetup;

        public int userCount
        {
            get
            {
                return userViewCameras.Count;
            }
        }

        public void FindProjectionsDepthMeshesAndUsers()
        {
            if(depthMeshes != null) depthMeshes.Clear();
            if (projections != null) projections.Clear();
            if (userViewCameras != null) userViewCameras.Clear();

            var dms = GameObject.FindObjectsOfType<RATDepthMesh>();
            foreach (RATDepthMesh d in dms) depthMeshes.Add(d);

            var projs = GameObject.FindObjectsOfType<RATProjector>();
            foreach (RATProjector p in projs)
            {
                projections.Add(p);
                p.projectionManager = this;
            }
            var users = GameObject.FindObjectsOfType<RATUserViewCamera>();
            foreach(RATUserViewCamera u in users)
            {
                userViewCameras.Add(u);
                u.projectionManager = this;
            }
        }

        void OnValidate()
        {
            FindProjectionsDepthMeshesAndUsers();
        }
        void Awake()
        {
            lstSetup = screenSetup;
            FindProjectionsDepthMeshesAndUsers();
        }

        void Start () {

            if (gameObject.GetComponent<Camera>() == null)
            {
                //projection manager needs a hidden camera so that its OnPostRender() function get called. 
                gameObject.AddComponent<Camera>().hideFlags = HideFlags.HideInInspector; //no need to see this camera or ever use it
            }

            if(screenSetup == ConfigOptions.MultiDisplay)
            {
                for (int i = 1; i < Mathf.Min(Display.displays.Length, projections.Count); i++)
                    Display.displays[i].Activate(); //1920,1080,60
            }

            UpdateSetup();
        }

        void UpdateSetup()
        {
            int cnt = 0;
            foreach (RATProjector projection in projections)
            {
                if (screenSetup == ConfigOptions.Editor)
                {
                    projection.GetComponent<Camera>().targetDisplay = 0;
                    if(projection.displayIndex< screenViewports.Length)
                        projection.GetComponent<Camera>().rect = screenViewports[cnt];
                }
                if (screenSetup == ConfigOptions.MultiDisplay)
                {
                    projection.GetComponent<Camera>().targetDisplay = projection.displayIndex;
                    projection.GetComponent<Camera>().rect = new Rect(0, 0, 1, 1);
                }
                cnt++;
            }

            lstSetup = screenSetup;
        }
	
	    void Update () {
            if (lstSetup!=screenSetup)
            {
                UpdateSetup();
            }
	    }
        void OnPostRender()
        {
            if (!renderOnlyTextures)
            {
                foreach (RATProjector projector in projections)
                {
                    projector.Render();
                }
            } else
            {
                foreach (RATProjector projector in projections)
                {
                    projector.RenderTexturesOnly();
                }
            }      
        }

        internal void RegisterUser(RATUserViewCamera user)
        {
            if(!userViewCameras.Contains(user))
                userViewCameras.Add(user);
        }

        internal void RegisterProjector(RATProjector projection)
        {
            if (!projections.Contains(projection))
                projections.Add(projection);
        }

    }
}

