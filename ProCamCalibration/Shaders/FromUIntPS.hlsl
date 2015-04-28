Texture2D<uint> inputTexture : register(t0);

struct PSInput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD;
};

float main(PSInput input, float4 screenSpace : SV_Position) : SV_Target0
{
	float value = inputTexture.Load(int3(screenSpace.x, screenSpace.y, 0));
	return value;
}