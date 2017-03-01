using UnityEngine;
using System.Collections;


/// <summary>
/// Behavior that can be attached to a camera. This behavior allows for dynamically masking the camera output in the scene. This is an image space operation The basic functionality includes 
/// two functions: masking and feathering. 
/// Masking: selectively displaying the image based on the transparency mask.
/// Feathering: selectively feathering each edge of the camera image based on the percentages provided (0-1)
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("RoomAliveToolkit/RATDynamicMask")]
public class RATDynamicMask : MonoBehaviour {

    public bool invertMask = false;
    private Shader maskShader;

    protected bool isSupported = true;
    /// <summary>
    /// If provided, the mask is applied as the final Unity pass on the camera. 
    /// </summary>
    public Texture2D maskTexture;
    private Material m_MaskMaterial;
    /// <summary>
    /// If set to true, each edge of the final camera image is feathered based on the percentages listed (top, bottom, left, right)
    /// </summary>
    public bool Feather = false;
    [Range(0, 1)]
    public float topFeather = 0f;
    [Range(0, 1)]
    public float bottomFeather = 0f;
    [Range(0, 1)]
    public float leftFeather = 0f;
    [Range(0, 1)]
    public float rightFeather = 0f;

    // Use this for initialization
    void Start()
    {
        maskShader = Shader.Find("RoomAlive/DynamicMaskShader");

        if (CheckSupport())
        {
            m_MaskMaterial = CheckShaderAndCreateMaterial(maskShader, m_MaskMaterial);
        }
    }

    protected Material CheckShaderAndCreateMaterial(Shader s, Material m2Create)
    {
        if (!s)
        {
            Debug.Log("Missing shader in " + ToString());
            enabled = false;
            return null;
        }

        if (s.isSupported && m2Create && m2Create.shader == s)
            return m2Create;

        if (!s.isSupported)
        {
            NotSupported();
            Debug.Log("The shader " + s.ToString() + " on effect " + ToString() + " is not supported on this platform!");
            return null;
        }
        else
        {
            m2Create = new Material(s);
            m2Create.hideFlags = HideFlags.DontSave;
            if (m2Create)
                return m2Create;
            else return null;
        }
    }

    protected bool CheckSupport()
    {
        isSupported = true;

        if (!SystemInfo.supportsImageEffects)
        {
            NotSupported();
            return false;
        }
        return true;
    }

    protected void NotSupported()
    {
        enabled = false;
        isSupported = false;
        return;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!isSupported)
        {
            Graphics.Blit(source, destination);
            return;
        }

        m_MaskMaterial.SetFloat("_Invert", (invertMask? 1.0f : 0f));
        m_MaskMaterial.SetTexture("_MaskTex", maskTexture); // mask texture
        m_MaskMaterial.SetFloat("_Feather", (Feather?1.0f: 0f));
        m_MaskMaterial.SetFloat("_Top", topFeather);
        m_MaskMaterial.SetFloat("_Bottom", bottomFeather); 
        m_MaskMaterial.SetFloat("_Left", leftFeather);
        m_MaskMaterial.SetFloat("_Right", rightFeather);

        source.wrapMode = TextureWrapMode.Clamp;
        Graphics.Blit(source, destination, m_MaskMaterial, 0);

    }

    // Update is called once per frame
    void Update () {
	
	}
}
