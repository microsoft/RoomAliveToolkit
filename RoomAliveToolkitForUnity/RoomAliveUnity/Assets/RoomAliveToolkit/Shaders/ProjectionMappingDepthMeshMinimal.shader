// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "RoomAlive/ProjectionMappingDepthMeshMinimal"
{
	Properties{
		_KinectDepthSource("Kinect Depth Source", 2D) = "black" {}
		_DepthToCameraSpaceX("Depth to Camera Space X", 2D) = "black" {}
		_DepthToCameraSpaceY("Depth to Camera Space Y", 2D) = "black" {}
		_UserViewPointRGB("User View RGB", 2D) = "" {}
		//_UserViewPointDepth("User View Depth", 2D) = "black" {}

		width("Width", Float) = 512
		height("Height", Float) = 424
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Pass
		{
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			LOD 200

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0			
			#include "UnityCG.cginc"

			sampler2D _UserViewPointRGB;
			uniform float4x4 _UserVP;

			//all other relevant variables are included in DepthMeshProcessing.cginc 
			#include "Assets/RoomAliveToolkit/Shaders/DepthMeshProcessing.cginc"

			struct v2f {
				float4 pos : POSITION;
				float4 userProj : USER_VIEW0;
				//float customColor : SPECIAL_COLOR;
			};

			void vert(appdata_full inputV, out v2f o)
			{
				UNITY_INITIALIZE_OUTPUT(v2f,o);

				float3 posA = float3(0,0,0);
				float3 posB = float3(0,0,0);
				float3 pos = ComputeFace(inputV.texcoord, posA, posB);

				// ***************************
				// Get RGB uvs
				// ***************************
				float4 colorPos = mul(_RGBExtrinsics, float4(-pos.x, pos.y, pos.z, 1.0));
				colorPos.xyz /= colorPos.w;
				float2 rgbUV = Project_RGB(float3(colorPos.x, colorPos.y, colorPos.z));

				// ***************************
				// Point depth data into world coordinate system
				// ***************************

				pos = CameraToWorld(pos);
				posA = CameraToWorld(posA);
				posB = CameraToWorld(posB);

				//triagles normals all point to the camera
				float3 norm = _WorldSpaceCameraPos.xyz - pos;
				norm = normalize(norm);

				// ***************************
				// Eliminate triangles that straddle big jumps in depth
				// ***************************
				float d = 0.1;
				if (length(posA - pos) > d || length(posB - pos) > d || length(posA - posB) > d)
				{
					pos = 0;
				}

				float4 worldPt = float4(pos.x, pos.y, pos.z, 1);
				o.pos = mul(UNITY_MATRIX_MVP, worldPt);

				// ***************************
				// User viewpoint texture pos
				// ***************************

				float4 userPt = mul(unity_ObjectToWorld, worldPt);
				userPt = mul(_UserVP, userPt);
				userPt = userPt / userPt.w;
				o.userProj = userPt;
			}


			half4 frag(v2f IN) : COLOR
			{
				float4 userScreenPos = ComputeScreenPos(IN.userProj);
				float2 screenPos = userScreenPos.xy / userScreenPos.w;

				float4 userViewColor = tex2Dlod(_UserViewPointRGB, float4(screenPos.x, screenPos.y, 0, 0));

				//clamp
				if (screenPos.x < 0 || screenPos.x>1 || screenPos.y < 0 || screenPos.y>1)
					discard;

				return userViewColor;
			}
			ENDCG
		}
	}
}
