RWByteAddressBuffer worldCoordinates : register(u0);

static const int depthImageWidth = 512;

struct Quad
{
	float3 upperNormal;
	float upperNormalValid;
	float3 lowerNormal;
	float lowerNormalValid;
};

RWStructuredBuffer<Quad> quadInfo : register(u1);

[numthreads(32, 22, 1)]
void main(uint3 DTid : SV_DispatchThreadID)
{
	float3 normal = 0;
	float sum = 0;

	// 0
	normal += quadInfo[DTid.y * depthImageWidth + DTid.x].upperNormal;
	sum += quadInfo[DTid.y * depthImageWidth + DTid.x].upperNormalValid;

	// 1
	normal += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x].lowerNormal;
	sum += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x].lowerNormalValid;

	// 2
	normal += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x].upperNormal;
	sum += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x].upperNormalValid;

	// 3
	normal += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x - 1].lowerNormal;
	sum += quadInfo[(DTid.y - 1) * depthImageWidth + DTid.x - 1].lowerNormalValid;

	// 4
	normal += quadInfo[DTid.y * depthImageWidth + DTid.x - 1].upperNormal;
	sum += quadInfo[DTid.y * depthImageWidth + DTid.x - 1].upperNormalValid;

	// 5
	normal += quadInfo[DTid.y * depthImageWidth + DTid.x - 1].lowerNormal;
	sum += quadInfo[DTid.y * depthImageWidth + DTid.x - 1].lowerNormalValid;

	normal /= sum;


	uint index = DTid.y * depthImageWidth + DTid.x;
	worldCoordinates.Store3(index * 24 + 12, asuint(normal));
}