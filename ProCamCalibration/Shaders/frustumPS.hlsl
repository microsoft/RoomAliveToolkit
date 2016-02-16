struct PSInput
{
	float4 pos : SV_POSITION;
};

cbuffer constants : register(b0)
{
	float3 color;
}

float4 main(PSInput input) : SV_TARGET0
{
	return float4(color, 1.0);
}
