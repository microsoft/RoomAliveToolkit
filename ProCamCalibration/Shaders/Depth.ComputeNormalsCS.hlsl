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
	uint index0 = DTid.y * depthImageWidth + DTid.x;
	normal += quadInfo[index0].upperNormal;
	sum += quadInfo[index0].upperNormalValid;

	// 1
	uint index1 = (DTid.y - 1) * depthImageWidth + DTid.x;
	normal += quadInfo[index1].lowerNormal;
	sum += quadInfo[index1].lowerNormalValid;

	// 2
	uint index2 = (DTid.y - 1) * depthImageWidth + DTid.x;
	normal += quadInfo[index2].upperNormal;
	sum += quadInfo[index2].upperNormalValid;

	// 3
	uint index3 = (DTid.y - 1) * depthImageWidth + DTid.x - 1;
	normal += quadInfo[index3].lowerNormal;
	sum += quadInfo[index3].lowerNormalValid;

	// 4
	uint index4 = DTid.y * depthImageWidth + DTid.x - 1;
	normal += quadInfo[index4].upperNormal;
	sum += quadInfo[index4].upperNormalValid;

	// 5
	uint index5 = DTid.y * depthImageWidth + DTid.x - 1;
	normal += quadInfo[index5].lowerNormal;
	sum += quadInfo[index5].lowerNormalValid;

	normal /= sum;


	worldCoordinates.Store3(index0 * 24 + 12, asuint(normal));
}