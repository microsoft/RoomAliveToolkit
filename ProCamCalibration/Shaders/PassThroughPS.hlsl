Texture2D<float4> inputTexture : register(t0);
SamplerState colorSampler : register(s0);

struct PSInput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD;
};

float4 main(PSInput input, float4 screenSpace : SV_Position) : SV_Target0
{
	return inputTexture.Sample(colorSampler, input.tex);
}