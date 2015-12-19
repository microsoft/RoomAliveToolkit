struct PSInput
{
	float4 pos : SV_Position;
};

float main(PSInput input) : SV_Target0
{
	return input.pos.z;
}
