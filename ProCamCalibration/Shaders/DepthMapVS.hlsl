
cbuffer constants : register(b0)
{
	matrix viewProjection;
}

float4 main(float3 world : pos) : SV_Position
{
	float4 world4 = float4(world, 1);
	return mul(world4, viewProjection);
}