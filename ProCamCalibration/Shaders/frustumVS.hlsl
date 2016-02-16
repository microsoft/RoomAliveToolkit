struct VSInput
{
	float4 pos : SV_POSITION;
};

cbuffer constants : register(b0)
{
	matrix world;
	matrix viewProjection;
	matrix frustumProjection;
}

struct VSOutput
{
	float4 pos : SV_POSITION;
};

VSOutput main(VSInput input)
{
	VSOutput output = (VSOutput)0;

	float4 invPos = mul(input.pos, frustumProjection);
	float4 worldPos = mul(invPos, world);
	output.pos = mul(worldPos, viewProjection);
	return output;
}