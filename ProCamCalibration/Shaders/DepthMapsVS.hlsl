Texture2D<uint> depthTexture : register(t0);

struct VSInput
{
	float4 pos : SV_Position; // better to split this into int2, float2?
};

struct VSOutput
{
	float4 pos : SV_Position;
	float depth : MYSEMANTIC;
};

// Input vertices are setup so that xy are the entries of the table 
// returned by GetDepthFrameToCameraSpaceTable, and zw are the (integer)
// depth image coordinates.
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

	output.pos = depthCamera;
	output.depth = depth; // passed to geometry shader to cull invalid triangles

	return output;
}
