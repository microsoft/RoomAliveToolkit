// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "RoomAlive/ProjectionMappingMinimal" {

	Properties {
		_UserViewPointRGB ("User View RGB", 2D) = "" {}
	}
	
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Pass
		{
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			LOD 200
			ZWrite On
			Offset -1,-2
			Fog { Mode Off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#include "UnityCG.cginc"

 			sampler2D _UserViewPointRGB;

			uniform float4x4 _UserVP;

			struct v2f {
				float4 pos : POSITION;
				float4 worldPos : TEX0;
				float customColor : COLOR;
			};

			void vert(appdata_full inputV, out v2f o)
			{
				UNITY_INITIALIZE_OUTPUT(v2f,o);
        
				float4 objPt = inputV.vertex;

				o.worldPos = mul(unity_ObjectToWorld, objPt);
				o.customColor = inputV.color;
				o.pos = UnityObjectToClipPos(objPt);
			}	

			half4 frag(v2f IN) : COLOR
			{
				float4 userProj = mul(_UserVP, IN.worldPos);
				float2 userTexCoord = userProj.xy / userProj.w;
				userTexCoord = userTexCoord / 2.0 + 0.5;
				if (userTexCoord.x < 0 || userTexCoord.x>1 || userTexCoord.y < 0 || userTexCoord.y>1 || (userProj.z)<0)
					discard;

				float4 userViewColor = tex2Dlod (_UserViewPointRGB, float4(userTexCoord.x, userTexCoord.y, 0, 0));
				//userViewColor *= IN.customColor;

				return userViewColor;
			}

			ENDCG
		}
	} 
}