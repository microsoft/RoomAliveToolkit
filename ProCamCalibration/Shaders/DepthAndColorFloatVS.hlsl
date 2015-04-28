Texture2D<float> depthTexture : register(t0);

struct VSInput
{
	float4 pos : SV_POSITION;
};

struct VSOutput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
	float depth : MYSEMANTIC;
};

static const float2 colorImageDims = float2(1920, 1080);

cbuffer cbRarelyChanges : register(b0)
{
	matrix depthToColor;
	float2 f;
	float2 c;
	float k1, k2;
	matrix viewProjection;
}


float2 Project(float4 x)
{
	float2 xp = x.xy / x.z;
	float rSq = dot(xp, xp);
	float2 xpp = xp * (1 + k1 * rSq + k2 * rSq * rSq);
	return f*xpp + c;
}

// Input vertices are setup so that xy are the entries of the table 
// returned by GetDepthFrameToCameraSpaceTable, and zw are the (integer)
// depth image coordinates.

VSOutput main( VSInput input )
{
	VSOutput output = (VSOutput)0;

	// depth
	float depth = depthTexture.Load(int3(input.pos.zw, 0)) / 1000.0; // m

	// depth camera coords
	float4 depthCamera = float4(input.pos.xy*depth, depth, 1);

	// color camera coords
	float4 colorCamera = mul(depthToColor, depthCamera);

	// color image coords [0,1],[0,1]
	float2 colorImage = Project(colorCamera) / colorImageDims;

	// texture coords
	float2 tex = float2(colorImage.x, 1 - colorImage.y); // flip y for texture coords

	// view volume
	float4 pos = mul(depthCamera, viewProjection);
	pos /= pos.w;

	output.pos = pos;
	output.tex = tex;
	output.depth = depth;

	return output;
}