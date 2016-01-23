struct VSInput
{
	float3 world : pos;
	float3 normal : NORMAL;
};

struct VSOutput
{
	float4 pos : SV_Position;
	float3 world : world;
	float3 normal : NORMAL;
};

cbuffer constants : register(b0)
{
	matrix viewProjection;
}

VSOutput main(VSInput input)
{
	// view volume
	float4 world4 = float4(input.world, 1);
	float4 pos = mul(world4, viewProjection);
	
	VSOutput output;
	output.pos = pos;
	output.world = input.world;
	output.normal = input.normal;

	return output;
}