Texture2D<float4> colorTexture : register(t0);
SamplerState colorSampler : register(s0);

struct PSInput
{
	float4 pos : SV_POSITION;
	float3 normal : NORMAL0;
	float3 lightDir : LIGHTDIR;
	float2 tex : TEXCOORD0;
};

cbuffer constants : register(b0)
{
	float3 cameraPosition;
	float3 Ia, Id, Is; // light ambient, diffuse, specular
	float3 Ka, Kd, Ks; // material ambient, diffiuse, specular
	float Ns; // material specular power
}

float4 main(PSInput input) : SV_TARGET0
{
	float3 lightDir = normalize(input.lightDir);
	float3 normal = normalize(input.normal);
	float3 Kd = colorTexture.Sample(colorSampler, input.tex).xyz;
	float3 color = Ka * Ia + Kd * saturate(dot(lightDir, normal)) * Id;
	return float4(color, 1);
}
