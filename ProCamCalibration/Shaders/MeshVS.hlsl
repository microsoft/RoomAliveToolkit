struct VSInput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
	float3 normal : NORMAL0;
};

cbuffer constants : register(b0)
{
	matrix world;
	matrix viewProjection;
	float3 lightPosition;
}

struct VSOutput
{
	float4 pos : SV_POSITION;
	float3 normal : NORMAL0;
	float3 lightDir : LIGHTDIR;
	float2 tex : TEXCOORD0;
};

VSOutput main(VSInput input)
{
	VSOutput output = (VSOutput)0;
	float4 worldPos = mul(input.pos, world);
	output.normal = mul(input.normal, (float3x3)world);
	output.lightDir = lightPosition - worldPos.xyz;
	output.pos = mul(worldPos, viewProjection);
	output.tex = input.tex;
	return output;
}