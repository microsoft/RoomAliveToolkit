struct VSOutput
{
	float4 pos : SV_Position;
	float3 world : world;
};

cbuffer constants : register(b0)
{
	matrix viewProjection;
}

VSOutput main(float3 world : pos)
{
	// view volume
	float4 world4 = float4(world, 1);
	float4 pos = mul(world4, viewProjection);

	VSOutput output;
	output.pos = pos;
	output.world = world;

	return output;
}