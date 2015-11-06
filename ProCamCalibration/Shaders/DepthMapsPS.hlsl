struct PSInput
{
	float4 pos : SV_Position;
	float depth : MYSEMANTIC; // ??
	uint renderTargetIndex : SV_RenderTargetArrayIndex;
};

float4 main(PSInput input) : SV_Target0
{
	return input.depth;
}
