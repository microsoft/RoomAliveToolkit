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
	float3 lightDirection : LIGHTDIR;
};

cbuffer constants : register(b0)
{
	matrix viewProjection;
	float3 lightPosition;
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
	output.lightDirection = lightPosition - input.world;

	return output;
}