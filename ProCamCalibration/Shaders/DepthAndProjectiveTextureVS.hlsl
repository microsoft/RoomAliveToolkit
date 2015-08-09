Texture2D<float> depthTexture : register(t0);

struct VSInput
{
	float4 pos : SV_POSITION; // better to split this into int2, float2?
};

struct VSOutput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
	float depth : MYSEMANTIC;
};

cbuffer constants : register(b0)
{
	// world is depth camera's pose; view and projection are that of the user
	// also includes adjustment to go from view volume to texture coordinates
	matrix userWorldViewProjection;
	// world is depth camera's pose; view and projection are that of the projector
	matrix projectorWorldViewProjection;
}

// Input vertices are setup so that xy are the entries of the table 
// returned by GetDepthFrameToCameraSpaceTable, and zw are the (integer)
// depth image coordinates.

// User view texture is rendered once; this shader is called once per depth camera, for each projector

VSOutput main(VSInput input)
{
	VSOutput output;

	// depth
	// vertex buffer is configured so that zw are the integer depth image coords for this vertex
	float depth = depthTexture.Load(int3(input.pos.zw, 0)) / 1000.0; // m

	// depth camera coords
	// vertex buffer is configured so that xy are the entries of GetDepthFrameToCameraSpaceTable
	// and can be used in the same way to transform a point in the depth image to a 3D point in the
	// depth camera's cordinate system
	float4 depthCamera = float4(input.pos.xy*depth, depth, 1.0);

	// projector's view volume
	float4 pos = mul(depthCamera, projectorWorldViewProjection);
	pos /= pos.w; // we need to do this because geometry shader comapares distances among points to reject triangles
		
	// user view texture coords
	// converts to texture coords
	float4 userViewTex = mul(depthCamera, userWorldViewProjection);
	userViewTex /= userViewTex.w;

	output.pos = pos;
	output.tex = userViewTex.xy;
	output.depth = depth; // passed to geometry shader to cull invalid triangles

	return output;
}
