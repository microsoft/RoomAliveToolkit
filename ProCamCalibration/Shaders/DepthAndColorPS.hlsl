
struct PSInput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
	float depth : MYSEMANTIC;
	float4 worldPos : TEXCOORD1;
};

cbuffer constants : register(b0)
{
	// world is depth camera's pose; view and projection are that of the user
	// also includes adjustment to go from view volume to texture coordinates
	matrix userWorldViewProjection;
	// world is depth camera's pose; view and projection are that of the projector
	matrix projectorWorldViewProjection;

	float globalTime;
}


// Based on shaders:
// Template - 3D 			https://www.shadertoy.com/view/ldfSWs
// Xor - Triangle Grid, 	https://www.shadertoy.com/view/4tSGWz

#define pi 3.14159265358979
#define size 0.5
#define reciproce_sqrt3 0.57735026918962576450914878050196
#define lineThickness 0.01

float planeDistance = 0.2;
float offset;

float r(float n)
{
	return frac(abs(sin(n*55.753)*367.34));
}

float r(float2 n)
{
	return r(dot(n, float2(2.46, -1.21)));
}

float3 smallTrianglesColor(float3 pos)
{
	float a = (radians(60.0));
	float zoom = 0.125;
	float2 c = (pos.xy + float2(0.0, pos.z)) * float2(sin(a), 1.0);//scaled coordinates
	c = ((c + float2(c.y, 0.0)*cos(a)) / zoom) + float2(floor((c.x - c.y*cos(a)) / zoom*4.0) / 4.0, 0.0);//Add rotations
	float type = (r(floor(c*4.0))*0.2 + r(floor(c*2.0))*0.3 + r(floor(c))*0.5);//Randomize type
	type += 0.3 * sin(globalTime*5.0*type);

	float l = min(min((1.0 - (2.0 * abs(frac((c.x - c.y)*4.0) - 0.5))),
		(1.0 - (2.0 * abs(frac(c.y * 4.0) - 0.5)))),
		(1.0 - (2.0 * abs(frac(c.x * 4.0) - 0.5))));
	l = smoothstep(0.06, 0.04, l);

	return lerp(type, l, 0.3);
}

float3 largeTrianglesColor(float3 pos)
{
	float a = (radians(60.0));
	float zoom = 0.5;
	float2 c = (pos.xy + float2(0.0, pos.z)) * float2(sin(a), 1.0);//scaled coordinates
	c = ((c + float2(c.y, 0.0)*cos(a)) / zoom) + float2(floor((c.x - c.y*cos(a)) / zoom*4.0) / 4.0, 0.0);//Add rotations

	float l = min(min((1.0 - (2.0 * abs(frac((c.x - c.y)*4.0) - 0.5))),
		(1.0 - (2.0 * abs(frac(c.y * 4.0) - 0.5)))),
		(1.0 - (2.0 * abs(frac(c.x * 4.0) - 0.5))));
	l = smoothstep(0.03, 0.02, l);

	return lerp(0.01, l, 1.0);
}

//---------------------------------------------------------------
// Material 
//
// Defines the material (colors, shading, pattern, texturing) of the model
// at every point based on its position and normal. In this case, it simply
// returns a constant yellow color.
//------------------------------------------------------------------------
float3 doMaterial(in float3 pos, in float3 midPos)
{
	float d = length(pos.xz - midPos.xz) + pos.y - midPos.y;
	
	float border = fmod(globalTime, 5.0);

	float3 c1 = largeTrianglesColor(pos);
	float3 c2 = smallTrianglesColor(pos);

	// Small rim
	float3 c = float3(1.0, 1.0, 1.0) * smoothstep(border - 0.2, border, d);
	// Large Triangles to all
	c += c1;
	// Small triangle slightly after front
	c += c2 * smoothstep(border - 0.4, border - 0.6, d);
	// Cutoff front
	c *= smoothstep(border, border - 0.05, d);
	// Cutoff back
	c *= smoothstep(border - 3.0, border - 0.5, d);
	// fade out
	c *= smoothstep(5.0, 4.0, border);

	return c * float3(0.2, 0.5, 1.0);
}


float4 main(PSInput input) : SV_TARGET0
{
	return float4(doMaterial(input.worldPos.xyz, float3(0.0, -1.0, 3.0)), 1.0);
}