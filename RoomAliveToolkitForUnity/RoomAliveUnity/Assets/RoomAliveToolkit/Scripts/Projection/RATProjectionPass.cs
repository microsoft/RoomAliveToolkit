using UnityEngine;
using System.Collections;

namespace RoomAliveToolkit
{
    /// <summary>
    /// A behavior to be added to each RATUserViewCamera which specifies what shaders will be used for rendering which layer of the scene. 
    /// Since projection mapping has two stages (user view + projection), each projection pass specifies the shader to use for each stage for the selected layer.
    /// </summary>
    [AddComponentMenu("RoomAliveToolkit/RATProjectionPass")]
    [RequireComponent(typeof(RATUserViewCamera))]
    public class RATProjectionPass : MonoBehaviour
    {
        /// <summary>
        /// Layer mask for selecting which layer this Projection pass is used for rendering
        /// </summary>
        public LayerMask targetSurfaceLayers = 1;

        /// <summary>
        /// Enables rendering of this layer in user view pass
        /// </summary>
        public bool renderUserView = true;
        /// <summary>
        /// The name of the shader that will be used to render selected layers in the user view pass.
        /// </summary>
        public Shader userViewShader;
        
        /// <summary>
        /// Enables rendering of this layer in the projection mapping pass
        /// </summary>
        public bool renderProjectionPass = true;
        /// <summary>
        /// The name of the shader that will be used to render selected layers in the projection pass (from the viewpoint of the projector).
        /// </summary>
        public Shader projectionShader;

        private string userViewPassShaderName;
        private string projectionPassShaderName;

        private static string static_userViewPassShader = "RoomAlive/ReplacementColorUnlitShader";
        private static string static_projPassShader = "RoomAlive/ProjectionMappingMinimal";

        private static string dynamic_userViewPassShader = "RoomAlive/ReplacementColorDepthMeshSurfaceShader";
        private static string dynamic_projPassShader = "RoomAlive/ProjectionMappingDepthMeshMinimal";

        internal void Init()
        {
        }
        private void SetupShaders()
        { 
            if (!userViewPassShaderName.Equals(""))
            {
                userViewShader = Shader.Find(userViewPassShaderName);
                if (userViewShader == null)
                    Debug.LogError("User view shader not found: " + userViewPassShaderName);
            }
            else
                userViewShader = null;
            if (!projectionPassShaderName.Equals(""))
            {
                projectionShader = Shader.Find(projectionPassShaderName);
                if (projectionShader == null)
                    Debug.LogError("Projection shader not found: " + projectionPassShaderName);
            }
            else
                projectionShader = null;

        }

        public void SetDefaultStaticShaders()
        {
            projectionPassShaderName = static_projPassShader;
            userViewPassShaderName = static_userViewPassShader;
            SetupShaders();
        }

        public void SetDefaultDynamicShaders()
        {
            projectionPassShaderName = dynamic_projPassShader;
            userViewPassShaderName = dynamic_userViewPassShader;
            SetupShaders();
        }

        void Update()
        {

        }
    }
}
