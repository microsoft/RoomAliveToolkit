Texture2D<float> depthImage : register(t0);
Texture2D<float2> depthFrameToCameraSpaceTable : register(t1);
// can't create a structured buffer that is also a vertex buffer, so we use byte address buffers:
RWByteAddressBuffer worldCoordinates : register(u0);
RWByteAddressBuffer indices : register(u1);

cbuffer constants : register(b0)
{
	uint indexOffset;
}

static const int depthImageWidth = 512;
static const float depthDiscontinuity = 100;

struct Quad
{
	float3 upperNormal;
	float3 lowerNormal;
};

RWStructuredBuffer<Quad> quadInfo : register(u2);

[numthreads(32, 22, 1)]
void main(uint3 DTid : SV_DispatchThreadID)
{
	// test upper and lower triangles in this quad; avoid dynamic branching
	// A triangle is valid if all its points are nonzero, and each point is close to each other in 
	// depth (i.e., they do not straddle a large depth discontinuity).
	float depth00 = depthImage[DTid.xy];
	float depth10 = depthImage[DTid.xy + uint2(1, 0)];
	float depth01 = depthImage[DTid.xy + uint2(0, 1)];
	float depth11 = depthImage[DTid.xy + uint2(1, 1)];

	uint upperNonzero = (depth00 * depth10 * depth01) > 0;
	uint lowerNonzero = (depth11 * depth10 * depth01) > 0;

	uint near01 = abs(depth00 - depth01) < depthDiscontinuity;
	uint near02 = abs(depth00 - depth10) < depthDiscontinuity;
	uint near12 = abs(depth01 - depth10) < depthDiscontinuity;
	uint upperValid = upperNonzero * near01 * near02 * near12;

	uint near43 = abs(depth10 - depth11) < depthDiscontinuity;
	uint near53 = abs(depth01 - depth11) < depthDiscontinuity;
	uint near45 = near12;
	uint lowerValid = lowerNonzero * near43 * near53 * near45;

	// upper and lower triangle normals
	uint index00 = DTid.y * depthImageWidth + DTid.x;
	float3 pos00 = asfloat(worldCoordinates.Load3(index00 * 24));

	uint index10 = DTid.y * depthImageWidth + DTid.x + 1;
	float3 pos10 = asfloat(worldCoordinates.Load3(index10 * 24));

	uint index01 = (DTid.y + 1) * depthImageWidth + DTid.x;
	float3 pos01 = asfloat(worldCoordinates.Load3(index01 * 24));

	uint index11 = (DTid.y + 1) * depthImageWidth + DTid.x + 1;
	float3 pos11 = asfloat(worldCoordinates.Load3(index11 * 24));

	float3 upperNormal = normalize(cross(pos01 - pos00, pos10 - pos00));
	float3 lowerNormal = normalize(cross(pos10 - pos11, pos01 - pos11));

	// store nornmals
	quadInfo[index00].upperNormal = upperValid > 0 ? upperNormal : 0;
	quadInfo[index00].lowerNormal = lowerValid > 0 ? lowerNormal : 0;

	// indices
	uint index2 = index00 + indexOffset; // each camera is 512*484*6 vertices
	indices.Store3(index00 * 24, index2 + upperValid * uint3(0, depthImageWidth, 1)); // 00, 01, 10
	indices.Store3(index00 * 24 + 12, index2 + lowerValid * uint3(depthImageWidth + 1, 1, depthImageWidth)); // 11, 10, 01
}