// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "RoomAlive/DynamicMaskShader" {
	Properties{
		_MainTex("Base", 2D) = "white" {}
		_MaskTex("Mask", 2D) = "white" {}
	}

		CGINCLUDE
		//roughly based on Hidden/VignettingShader.shader part of the StandardAssets/ImageEffects
#include "UnityCG.cginc"

	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
		float2 uv2 : TEXCOORD1;
	};

	sampler2D _MainTex;
	sampler2D _MaskTex;

	half _Invert;
	half _Feather;
	half _Top;
	half _Bottom;
	half _Left; 
	half _Right;

	half4 _black = half4(0, 0, 0, 1);

	float4 _MainTex_TexelSize;

	v2f vert(appdata_img v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		o.uv2 = v.texcoord.xy;

#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv2.y = 1.0 - o.uv2.y;
#endif

		return o;
	}

	half4 frag(v2f i) : SV_Target{
		half2 coords = i.uv;
		half2 uv = i.uv;

		half4 color = tex2D(_MainTex, uv);
		half4 colorMask = tex2D(_MaskTex, i.uv2);

		float interp = (_Invert == 1) ? colorMask.r : 1.0 - colorMask.r;

		float vBlend = 1.0f;
		float blend = 1.0f;

		if (_Feather) // disable for performance reasons
		{
			if (coords.x < _Left)
			{
				blend = (coords.x) / _Left;
				vBlend = vBlend * blend;
			}
			if (coords.x > 1.0 - _Right)
			{
				blend = (1.0 - coords.x) / _Right;
				vBlend = vBlend * blend;
			}
			if (coords.y < _Top)
			{
				blend = (coords.y) / _Top;
				vBlend = vBlend * blend;
			}
			if (coords.y > 1.0 - _Bottom)
			{
				blend = (1.0 - coords.y) / _Bottom;
				vBlend = vBlend * blend;
			}

			float b = 0.0f;
			float c = 0.3f;
			float gauss = 1.0 - exp(-(vBlend - b)*(vBlend - b) / (2 * c*c));
			float interp2 = 1.0 - pow(gauss, 0.5);

			if (interp2 > interp) interp = interp2;
		}
		

		color = lerp(color, _black, interp);

		return color;
	}

		ENDCG

		Subshader {
		Pass{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
			ENDCG
		}
	}

	Fallback off
}