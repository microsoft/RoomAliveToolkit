Texture2D<float> depthImage : register(t0);
Texture2D<float2> depthFrameToCameraSpaceTable : register(t1);
// can't create a structured buffer that is also a vertex buffer, so we use byte address buffers:
RWByteAddressBuffer worldCoordinates : register(u0);
RWByteAddressBuffer indices : register(u1);

cbuffer constants : register(b0)
{
	matrix world;
	uint indexOffset;
}

static const int depthImageWidth = 512;
static const float depthDiscontinuity = 100;

struct Quad
{
	float3 upperNormal;
	float upperNormalValid;
	float3 lowerNormal;
	float lowerNormalValid;
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

	// world coordinate
	float2 undistorted00 = depthFrameToCameraSpaceTable[DTid.xy];
	float depthMeters00 = depth00 / 1000; // m
	float4 depthCamera00 = float4(undistorted00*depthMeters00, depthMeters00, 1);
	float4 pos00 = mul(world, depthCamera00);

	float2 undistorted10 = depthFrameToCameraSpaceTable[DTid.xy + uint2(1, 0)];
	float depthMeters10 = depth10 / 1000; // m
	float4 depthCamera10 = float4(undistorted10*depthMeters10, depthMeters10, 1);
	float4 pos10 = mul(world, depthCamera10);

	float2 undistorted01 = depthFrameToCameraSpaceTable[DTid.xy + uint2(0, 1)];
	float depthMeters01 = depth01 / 1000; // m
	float4 depthCamera01 = float4(undistorted01*depthMeters01, depthMeters01, 1);
	float4 pos01 = mul(world, depthCamera01);

	float2 undistorted11 = depthFrameToCameraSpaceTable[DTid.xy + uint2(1, 1)];
	float depthMeters11 = depth11 / 1000; // m
	float4 depthCamera11 = float4(undistorted11*depthMeters11, depthMeters11, 1);
	float4 pos11 = mul(world, depthCamera11);


	// upper and lower normals
	float3 a = pos01.xyz - pos00.xyz;
	float3 b = pos10.xyz - pos00.xyz;
	float3 normal0 = cross(a, b);
	normal0 = normalize(normal0);


	float3 c = pos10.xyz - pos11.xyz;
	float3 d = pos01.xyz - pos11.xyz;
	float3 normal1 = cross(c, d);
	normal1 = normalize(normal1);

	// store world coordinate
	uint index = DTid.y * depthImageWidth + DTid.x;
	worldCoordinates.Store3(index * 24, asuint(pos00.xyz));

	// indices
	uint index2 = index + indexOffset; // each camera is 512*484*6 vertices
	indices.Store3(index * 24, upperValid * uint3(index2, index2 + depthImageWidth, index2 + 1)); // 00, 01, 10
	indices.Store3(index * 24 + 12, lowerValid * uint3(index2 + depthImageWidth + 1, index2 + 1, index2 + depthImageWidth)); // 11, 10, 10


	if (upperValid)
	{
		quadInfo[index].upperNormalValid = 1;
		quadInfo[index].upperNormal = normal0;
	}
	else
	{
		quadInfo[index].upperNormalValid = 0;
		quadInfo[index].upperNormal = 0;
	}

	if (lowerValid)
	{
		quadInfo[index].lowerNormalValid = 1;
		quadInfo[index].lowerNormal = normal1;
	}
	else
	{
		quadInfo[index].lowerNormalValid = 0;
		quadInfo[index].lowerNormal = 0;

	}
}