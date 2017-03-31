// The only need for this shader is to replace the background texture of the depth mesh with a solid unlit color 

Shader "RoomAlive/ReplacementColorDepthMeshSurfaceShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "black" {}
		_KinectDepthSource ("Kinect Depth Source", 2D) = "black" {}
		_DepthToCameraSpaceX ("Depth to Camera Space X", 2D) = "black" {}
		_DepthToCameraSpaceY ("Depth to Camera Space Y", 2D) = "black" {}
         
        width ("Width", Float) = 512
        height ("Height", Float) = 424

		_RGBImageXDirectionFlag("RGBImageXDirectionFlag", Float) = 1 //set it to -1 to flip RGB image vertically 
		_RGBImageYDirectionFlag("RGBImageYDirectionFlag", Float) = 1 //set it to -1 to flip RGB image horizontally

		_DepthCuttofThreshold ("Depth Cuttof Threshold", Range(0,2)) = 0.1
		_ReplacementColor ("Replacement Color", Color) = (0,0,1,1)
	}
	
	SubShader {
		Tags { "RenderType"="Opaque"}
		Cull Off
		//Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		//#pragma surface surf SimpleLambert addshadow fullforwardshadows vertex:disp nolightmap
		#pragma surface surf SimpleLambert vertex:disp nolightmap
        #pragma target 5.0			
		#include "UnityCG.cginc"
		
		sampler2D _MainTex;
		float _DepthCuttofThreshold;
		float _RGBImageXDirectionFlag;
		float _RGBImageYDirectionFlag;
		half4 _ReplacementColor;
		float _DoReplacement;
		//all other relevant variables are included in DepthMeshProcessing.cginc 
		#include "Assets/RoomAliveToolkit/Shaders/DepthMeshProcessing.cginc"

		struct Input 
		{
			float2 MainTexUV;
			float3 worldPt;
		};						
		

        void disp(inout appdata_base inputV, out Input o)
        {
        	UNITY_INITIALIZE_OUTPUT(Input,o);

			float3 posA = float3(0,0,0);
			float3 posB =  float3(0,0,0); 
			float3 pos = ComputeFace(inputV.texcoord, posA, posB);

			// ***************************
			// Get RGB uvs
			// ***************************
			float4 colorPos = mul(_RGBExtrinsics, float4(-pos.x, pos.y, pos.z, 1.0)); //RGBExtrinsics are in right hand coordinates, so we need to -x 
			colorPos.xyz /= colorPos.w;
			float2 rgbUV = Project_RGB(float3(colorPos.x, colorPos.y, colorPos.z));

			if (_RGBImageXDirectionFlag<0) rgbUV.x = 1 - rgbUV.x;
			if (_RGBImageYDirectionFlag<0) rgbUV.y = 1 - rgbUV.y;

			// ***************************
			// Point depth data into world coordinate system
			// ***************************			
			pos = CameraToWorld(pos);
			posA = CameraToWorld(posA);
			posB = CameraToWorld(posB);
			
			// ***************************
			// Compute normals
			// ***************************
			float3 norm = normalize(cross(posA - pos, posB - pos));
			
			// ***************************
			// Eliminate triangles that straddle big jumps in depth
			// ***************************
			float d = _DepthCuttofThreshold;//0.1;
			if (length(posA - pos) > d || length(posB - pos) > d || length(posA - posB) > d)
				pos = 0;		

			// ***************************
			// Return values
			// ***************************
			float4 toMult = float4(pos.x, pos.y, pos.z, 1);
			o.worldPt = mul(unity_ObjectToWorld, toMult); // inputV.vertex gets transformed by this matrix automatically, we need to manually transform this one
			o.MainTexUV = rgbUV;

        	inputV.vertex = float4(pos.x, pos.y, pos.z, 1);
            inputV.normal = norm;
            inputV.texcoord = float4(rgbUV.x, rgbUV.y, 0, 0);

        }
					
		half4 LightingSimpleLambert (SurfaceOutput s, half3 lightDir, half atten) {
			//half NdotL = dot(normalize(s.Normal), normalize(lightDir));
			half4 c;
			c.rgb = s.Albedo;// * _LightColor0.rgb * (NdotL * atten * 2);
			c.a = s.Alpha;
			return c;
		}
		
		half4 LightingSimpleLambert_PrePass (SurfaceOutput s, half4 light)
		{
			half4 c;
			c.rgb = s.Albedo;// * light;
			c.a = s.Alpha;
			return c;
		}

		void surf (Input IN, inout SurfaceOutput o) 
		{
			float2 uv = IN.MainTexUV;

			half4 c;
			c = _ReplacementColor;
			
			o.Albedo = c.rgb;
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
