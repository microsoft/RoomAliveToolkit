RWByteAddressBuffer worldCoordinates : register(u0);

static const int depthImageWidth = 512;

struct Quad
{
	float3 upperNormal;
	float3 lowerNormal;
};

RWStructuredBuffer<Quad> quadInfo : register(u1);

[numthreads(32, 22, 1)]
void main(uint3 DTid : SV_DispatchThreadID)
{
	float3 normal = 0;

	normal += quadInfo[DTid.y * depthImageWidth + DTid.x].upperNormal;
	normal += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x].lowerNormal;
	normal += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x].upperNormal;
	normal += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x - 1].lowerNormal;
	normal += quadInfo[DTid.y * depthImageWidth + DTid.x - 1].upperNormal;
	normal += quadInfo[DTid.y * depthImageWidth + DTid.x - 1].lowerNormal;

	normal = normalize(normal);

	uint index = DTid.y * depthImageWidth + DTid.x;
	worldCoordinates.Store3(index * 24 + 12, asuint(normal));
}