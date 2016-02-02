Texture2D<float4> colorTexture : register(t0);
SamplerState colorSampler : register(s0);

struct PSInput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
	float depth : MYSEMANTIC;
};

float4 main(PSInput input) : SV_TARGET0
{
	return colorTexture.Sample(colorSampler, input.tex);
}
