Texture2D<uint> depthTexture : register(t0);

struct VSInput
{
	float4 pos : test; // TODO: consider int2, float2 as a means to avoid ftoi later
};

struct VSOutput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD0;
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

static const int4 offsets[] =
{
	{ 0, 1, 1, 0 },
	{ 1, -1, 0, -1 },
	{ -1, 0, -1, 1 },
	{ 0, -1, -1, 0 },
	{ -1, 1, 0, 1 },
	{ 1, 0, 0, -1 }
};

// Input vertices are setup so that xy are the entries of the table 
// returned by GetDepthFrameToCameraSpaceTable, and zw are the (integer)
// depth image coordinates.

VSOutput main( VSInput input, uint vertexID : SV_VertexID )
{
	VSOutput output;

	// depth
	float depth = depthTexture.Load(int3(input.pos.zw, 0)); // mm

	// test the triangle; avoid dynamic branching
	// A triangle is valid if all its points are nonzero, and each point is close to each other in 
	// depth (i.e., they do not straddle a large depth discontinuity).

	uint whichVertex = vertexID % 6;

	float depth0 = depthTexture.Load(float3(input.pos.zw + offsets[whichVertex].xy, 0)); // mm
	float depth1 = depthTexture.Load(float3(input.pos.zw + offsets[whichVertex].zw, 0)); // mm
	float nonZero = depth * depth0 * depth1;

	float near01 = abs(depth - depth0) < 100;// ? 1 : 0; // mm
	float near02 = abs(depth - depth1) < 100;// ? 1 : 0;
	float near12 = abs(depth0 - depth1) < 100;// ? 1 : 0;

	int valid = nonZero * near01 * near02 * near12;

	// depth camera coords
	// TODO: can remove this by pre dividing depthToColor and changing 1 below to 1000
	float depthf = (float)depth / 1000;
	float4 depthCamera = float4(input.pos.xy*depthf, depthf, 1);

	// color camera coords
	// TODO: is post multiplication faster?
	float4 colorCamera = mul(depthToColor, depthCamera);
	
	// color image coords [0,1],[0,1]
	// TODO: can remove this division if pre-divide f and c
	float2 colorImage = Project(colorCamera) / colorImageDims;
	
	// texture coords
	// TODO: can remove this step by folding into Project?
	float2 tex = float2(colorImage.x, 1 - colorImage.y); // flip y for texture coords

	// view volume
	float4 pos = mul(depthCamera, viewProjection);

	output.pos = pos * valid;
	output.tex = tex;

	return output;
}