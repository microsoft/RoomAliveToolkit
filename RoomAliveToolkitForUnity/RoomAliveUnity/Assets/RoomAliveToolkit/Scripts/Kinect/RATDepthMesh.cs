using UnityEngine;
using System;

namespace RoomAliveToolkit
{
    [AddComponentMenu("RoomAliveToolkit/RATDepthMesh")]

    /// <summary>
    /// A behavior that is used to dynamically render depth meshes in the scene. Uses custom shaders to assemble a mesh out of the depth image and texture maps it from the color image (in real time). 
    /// Depth and color data are presumed to be supplied from RATKinectClient. 
    /// </summary>
    public class RATDepthMesh : MonoBehaviour
    {
        // ****************************
        // Public Member Variables
        // ****************************

        /// <summary>
        /// The realtime source of Kinect depth + color data
        /// </summary>
        public RATKinectClient kinectClient;
        /// <summary>
        /// Material which encapsulates the custom depth mesh shader used for rendering depth meshes. 
        /// </summary>
        public Material surfaceMaterial;
        /// <summary>
        /// The depth image from Kinect. 
        /// </summary>
        [Space(10)]
        [ReadOnly]
        public Texture2D depthTexture;
        /// <summary>
        /// The RGB color image from Kinect.
        /// </summary>
        [ReadOnly]
        public Texture2D rgbTexture;
        /// <summary>
        /// Flag for forcing the shader to work with a specific "replacement" color texture instead of the real-time acquired color image from Kinect. 
        /// (false by default)
        /// </summary>
        [Space(10)]
        public bool UseReplacementTexture = false;
        /// <summary>
        /// An image (as Texture2D) that can be used as "replacement" texture by the depth shader. 
        /// </summary>
        public Texture2D replacementTexture;
        
        /// <summary>
        /// Used in lighting calculations on whether this mesh should receive shadows. 
        /// </summary>
        [Space(10)]
        public bool ReceiveShadows = false;
        /// <summary>
        /// Used in lighting calculations on whether this mesh should cast shadows. 
        /// </summary>
        public bool CastShadows = false;

        // ****************************
        // Private Member Variables
        // ****************************
        protected Texture2D depthToCameraSpaceX, depthToCameraSpaceY;

        // Unity cannot render one full mesh due to number of triangles limit, so this splits it into segments to be rendered separately. 
        private int depthWidth = 512;//640;
        private int depthHeight = 8;//6;
        private int divTilesX = 1;
        private int divTilesY = 53;//80;
        private int numTiles;

        private bool meshActive = true;   //tracks change of kinect settings

        private Mesh[] meshes;
        private MeshFilter[] meshFilters;
        private MeshRenderer[] meshRenderers;

        private GameObject[] gameObjects;

        private int depthTableUpdateCount = 0;

        private bool inited = false;

        public void Start()
        {
            //do some sanity checking
            if (kinectClient == null)
            {
                Debug.LogError("DepthMesh error: No KinectV2Client specified! Please specify the clinet in the editor!");
            }
            else
            {
                depthTexture = kinectClient.DepthTexture;
                if (!UseReplacementTexture)
                    rgbTexture = kinectClient.RGBTexture;
            }

            if (surfaceMaterial == null)
            {
                Debug.LogError("DepthMesh error: No Surface Material specified!");
                return;
            }

            surfaceMaterial = new Material(surfaceMaterial);

            // encode the camera space table as two color textures
            depthToCameraSpaceX = new Texture2D(512, 424, TextureFormat.ARGB32, false, true);
            depthToCameraSpaceY = new Texture2D(512, 424, TextureFormat.ARGB32, false, true);

            UpdateMaterials();
            CreateResources();

            inited = true;

        }

        private void UpdateMaterials()
        {
            surfaceMaterial.SetMatrix("_IRIntrinsics", kinectClient.IR_Intrinsics);
            surfaceMaterial.SetMatrix("_RGBIntrinsics", kinectClient.RGB_Intrinsics);
            surfaceMaterial.SetMatrix("_RGBExtrinsics", kinectClient.RGB_Extrinsics);
            surfaceMaterial.SetMatrix("_CamToWorld", Matrix4x4.identity); //formerly kinectClient.localToWorld, now incorperated in transform
            surfaceMaterial.SetTexture("_MainTex", rgbTexture);
            surfaceMaterial.SetTexture("_KinectDepthSource", kinectClient.DepthTexture);
            surfaceMaterial.SetTexture("_DepthToCameraSpaceX", depthToCameraSpaceX);
            surfaceMaterial.SetTexture("_DepthToCameraSpaceY", depthToCameraSpaceY);
        }

        private void CreateResources()
        {
            int numPoints = 0;
            numPoints = (depthWidth - 1) * (depthHeight) * 6;

            numTiles = divTilesX * divTilesY;
            var verts = new Vector3[numPoints];
            for (var i = 0; i < numPoints; ++i)
                verts[i] = new Vector3(0.0f, 0.0f, 0.0f);

            var indices = new int[numPoints];
            for (var i = 0; i < numPoints; ++i)
                indices[i] = i;

            var texCoords = new Vector2[numPoints];
            for (var i = 0; i < numPoints; ++i)
            {
                texCoords[i].x = (float)(i);// + 0.001f);
            }

            var normals = new Vector3[numPoints];
            for (var i = 0; i < numPoints; ++i)
            {
                normals[i] = new Vector3(0.0f, 1.0f, 0.0f);
            }

            meshes = new Mesh[numTiles];
            meshFilters = new MeshFilter[numTiles];
                meshRenderers = new MeshRenderer[numTiles];

            gameObjects = new GameObject[numTiles];

            for (int i = 0; i < numTiles; i++)
            {
                // id
                for (var texIndex = 0; texIndex < numPoints; ++texIndex)
                {
                    texCoords[texIndex].y = (float)(i);// + .001f);
                }

                gameObjects[i] = new GameObject("Depth SubMesh");
                gameObjects[i].layer = gameObject.layer;

                gameObjects[i].transform.parent = transform;
                gameObjects[i].transform.localPosition = Vector3.zero;
                gameObjects[i].transform.localRotation = Quaternion.identity;
                gameObjects[i].transform.localScale = Vector3.one;

                meshFilters[i] = (MeshFilter)gameObjects[i].AddComponent(typeof(MeshFilter));
                    meshRenderers[i] = (MeshRenderer)gameObjects[i].AddComponent(typeof(MeshRenderer));

                meshes[i] = new Mesh();
                meshes[i].vertices = verts;
                meshes[i].subMeshCount = 1;
                meshes[i].uv = texCoords;
                meshes[i].normals = normals;


                //if (isPoints)
                //    meshes[i].SetIndices(indices, MeshTopology.Points, 0);
                //else
                    meshes[i].SetIndices(indices, MeshTopology.Triangles, 0);

                meshes[i].bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(20000, 20000, 20000));

                meshFilters[i].mesh = meshes[i];
                    meshRenderers[i].enabled = true;

                //materials get updated every frame for every mesh
                meshRenderers[i].material = surfaceMaterial; //default material 
                meshRenderers[i].receiveShadows = ReceiveShadows;
                meshRenderers[i].shadowCastingMode = CastShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                
            }
        }

        public bool LoadDepthToCameraSpaceTable()
        {
            if (kinectClient.depthToCameraSpaceTableUpdateCount>depthTableUpdateCount)
            {
                var depthToCameraSpaceXColors = new Color[512 * 424];
                var depthToCameraSpaceYColors = new Color[512 * 424];

                int i = 0;
                for (int y = 0; y < 424; y++)
                    for (int x = 0; x < 512; x++)
                    {
                        var entry = kinectClient.DepthSpaceToCameraSpaceTable[x, y];
                        var dx = (entry.x + 1.0f) / 2.0f; // put in [0..1)
                        var dy = (entry.y + 1.0f) / 2.0f; // put in [0..1)

                        var encodedX = EncodeFloatRGBA(dx);
                        var encodedY = EncodeFloatRGBA(dy);

                        depthToCameraSpaceXColors[i] = new Color(encodedX[0], encodedX[1], encodedX[2], encodedX[3]);
                        depthToCameraSpaceYColors[i] = new Color(encodedY[0], encodedY[1], encodedY[2], encodedY[3]);
                        i++;
                    }
                depthToCameraSpaceX.SetPixels(depthToCameraSpaceXColors);
                depthToCameraSpaceX.Apply();
                depthToCameraSpaceY.SetPixels(depthToCameraSpaceYColors);
                depthToCameraSpaceY.Apply();
                depthTableUpdateCount = kinectClient.depthToCameraSpaceTableUpdateCount;
                return true;
            }
            return false;
            
        }

        static float[] EncodeFloatRGBA(float val)
        {
            float[] kEncodeMul = new float[] { 1.0f, 255.0f, 65025.0f, 160581375.0f };
            float kEncodeBit = 1.0f / 255.0f;
            for (int i = 0; i < kEncodeMul.Length; ++i)
            {
                kEncodeMul[i] *= val;
                // Frac
                kEncodeMul[i] = (float)(kEncodeMul[i] - Math.Truncate(kEncodeMul[i]));
            }
            // enc -= enc.yzww * kEncodeBit;
            float[] yzww = new float[] { kEncodeMul[1], kEncodeMul[2], kEncodeMul[3], kEncodeMul[3] };
            for (int i = 0; i < kEncodeMul.Length; ++i)
            {
                kEncodeMul[i] -= yzww[i] * kEncodeBit;
            }
            return kEncodeMul;
        }

        public void Update()
        {
            if (!inited)
                return;
            
            if(!meshActive)
            {
                return;
            }


            if(LoadDepthToCameraSpaceTable())
            {
                surfaceMaterial.SetTexture("_DepthToCameraSpaceX", depthToCameraSpaceX);
                surfaceMaterial.SetTexture("_DepthToCameraSpaceY", depthToCameraSpaceY);
            }

            surfaceMaterial.SetMatrix("_IRIntrinsics", kinectClient.IR_Intrinsics); //these can change when the KinectV2Client updates them from default values
            surfaceMaterial.SetMatrix("_RGBIntrinsics", kinectClient.RGB_Intrinsics);
            surfaceMaterial.SetMatrix("_RGBExtrinsics", kinectClient.RGB_Extrinsics);
            surfaceMaterial.SetVector("_RGBDistCoef", kinectClient.RGB_DistCoef);
            surfaceMaterial.SetVector("_IRDistCoef", kinectClient.IR_DistCoef);
            surfaceMaterial.SetFloat("_RGBImageYDirectionFlag", -1); //flip for JPEG encoded images
            
            if (kinectClient != null) // update the Kinect textures mostly for viewing them from the editor
            {
                if (UseReplacementTexture) rgbTexture = replacementTexture; 
                else rgbTexture = kinectClient.RGBTexture;
            }

            if (rgbTexture != null )
            {
                surfaceMaterial.SetTexture("_MainTex", rgbTexture);
                surfaceMaterial.GetTexture("_MainTex").wrapMode = TextureWrapMode.Clamp;
            }

//            surfaceMaterial.SetMatrix("_CamToWorld", kinectClient.localToWorld);
            surfaceMaterial.SetMatrix("_CamToWorld", Matrix4x4.identity);  //Already incorperated in transform
            surfaceMaterial.SetFloat("_RealTime", Time.timeSinceLevelLoad);
        }

        //public void EnableRendering(bool value)
        //{
        //        foreach (Renderer rend in meshRenderers)
        //            rend.enabled = value;
        //}

        //public void SetUserViewParameters(int userId, Vector3 userViewPos, Matrix4x4 cameraProjectionMatrix, Matrix4x4 cameraViewMatrix, Texture texture)
        //{
        //    string userString = "_User" + (userId + 1);
        //}

        //public void UpdateRenderPass1()
        //{
        //    UpdateMaterial(surfaceMaterial, true);
        //}

        private void UpdateMaterial(Material mat, bool updateShadowInformation)
            {
                foreach (Renderer rend in meshRenderers)
                {
                rend.material = mat;
                if (updateShadowInformation)
                {
                    rend.receiveShadows = ReceiveShadows;
                    rend.shadowCastingMode = CastShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        }
    }
}