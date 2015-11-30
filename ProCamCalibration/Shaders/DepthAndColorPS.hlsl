Texture2D<float4> colorTexture : register(t0);
SamplerState colorSampler : register(s0);

struct PSInput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD0;
};

float4 main(PSInput input) : SV_Target0
{
	return colorTexture.Sample(colorSampler, input.tex);
}
