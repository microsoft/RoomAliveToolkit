struct VSInput
{
	float3 pos : pos;
};

struct VSOutput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD0;
};

cbuffer constants : register(b0)
{
	matrix worldToColor;
	matrix viewProjection;
	float2 f;
	float2 c;
	float k1, k2;
}

float2 Project(float4 x)
{
	float2 xp = x.xy / x.z;
	float rSq = dot(xp, xp);
	float2 xpp = xp * (1 + k1 * rSq + k2 * rSq * rSq);
	return f*xpp + c;
}

VSOutput main(VSInput input)
{
	float4 world = float4(input.pos, 1);

	// view volume
	float4 pos = mul(world, viewProjection);

	// color camera coords
	float4 colorCamera = mul(world, worldToColor);

	// color image coords [0,1],[0,1]
	// this includes a flip in y to get to texture coordinates (y down): f_y' = -f_y, c_y' = 1 - c_y 
	float2 tex = Project(colorCamera);

	VSOutput output;
	output.pos = pos;
	output.tex = tex;

	return output;
}