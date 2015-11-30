Texture2D<uint> depthImage : register(t0);
Texture2D<float2> depthFrameToCameraSpaceTable : register(t1);
// can't create a structured buffer that is also a vertex buffer, so we use byte address buffers:
RWByteAddressBuffer worldCoordinates : register(u0);
RWByteAddressBuffer indices : register(u1);

cbuffer constants : register(b0)
{
	matrix world;
}

static const int depthImageWidth = 512;

[numthreads(32, 22, 1)]
void main( uint3 DTid : SV_DispatchThreadID )
{
	// test upper and lower triangles in this quad; avoid dynamic branching
	// A triangle is valid if all its points are nonzero, and each point is close to each other in 
	// depth (i.e., they do not straddle a large depth discontinuity).
	uint depth00 = depthImage[DTid.xy];
	uint depth10 = depthImage[DTid.xy + uint2(1, 0)];
	uint depth01 = depthImage[DTid.xy + uint2(0, 1)];
	uint depth11 = depthImage[DTid.xy + uint2(1, 1)];

	uint upperNonzero = (depth00 * depth10 * depth01) > 0;
	uint lowerNonzero = (depth11 * depth10 * depth01) > 0;

	uint near01 = abs((int)depth00 - (int)depth01) < 100;
	uint near02 = abs((int)depth00 - (int)depth10) < 100;
	uint near12 = abs((int)depth01 - (int)depth10) < 100;

	uint near43 = abs((int)depth10 - (int)depth11) < 100;
	uint near53 = abs((int)depth01 - (int)depth11) < 100;
	uint near45 = near12;

	uint upperValid = upperNonzero * near01 * near02 * near12;
	uint lowerValid = lowerNonzero * near43 * near53 * near45;

	// world coordinate
	float2 distorted = depthFrameToCameraSpaceTable[DTid.xy];
	float depth = (float)depth00 / 1000; // m
	float4 depthCamera = float4(distorted*depth, depth, 1);
	float4 pos = mul(depthCamera, world);

	uint index = DTid.y * depthImageWidth + DTid.x;
	worldCoordinates.Store3(index * 12, asuint(pos.xyz));

	// indices
	indices.Store3(index * 24, upperValid * uint3(index, index + depthImageWidth, index + 1)); // 00, 01, 10
	indices.Store3(index * 24 + 12, lowerValid * uint3(index + depthImageWidth + 1, index + 1, index + depthImageWidth)); // 11, 10, 10
}