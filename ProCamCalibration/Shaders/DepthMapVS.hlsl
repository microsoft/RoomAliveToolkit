struct VSInput
{
	float3 pos : pos;
};

struct VSOutput
{
	float4 pos : SV_Position;
};

cbuffer constants : register(b0)
{
	matrix viewProjection;
}

VSOutput main(VSInput input)
{
	float4 world = float4(input.pos, 1);

	// view volume
	VSOutput output;
	output.pos = mul(world, viewProjection);

	return output;
}