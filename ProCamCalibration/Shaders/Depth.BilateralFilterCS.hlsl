Texture2D<uint> input : register(t0);
RWTexture2D<float> output : register(u0);
Texture2D<float2> depthFrameToCameraSpaceTable : register(t1);
// can't create a structured buffer that is also a vertex buffer, so we use byte address buffers:
RWByteAddressBuffer worldCoordinates : register(u1);

cbuffer constants : register(b0)
{
	matrix world;
	float spatialSigmaSq; // pixels, 1/sigma
	float intensitySigmaSq; // m, 1/sigma
}

#define PI 3.14159265358979323846

static const int depthImageWidth = 512;
#define halfWidth 3

[numthreads(32, 22, 1)]
void main( uint3 DTid : SV_DispatchThreadID )
{
	float sum = 0;
	float sumWeights = 0;

	float depth0 = input[DTid.xy];

	for (int dy = -halfWidth; dy <= halfWidth; dy++)
		for (int dx = -halfWidth; dx <= halfWidth; dx++)
		{
			float depth = input[DTid.xy + int2(dx, dy)];

			float dDepth = (depth - depth0);
			float weight = exp(-0.5*(spatialSigmaSq*(dx*dx + dy*dy) + intensitySigmaSq*dDepth*dDepth));

			sum += weight * depth;
			sumWeights += weight;
		}

	float filteredDepth = sum / sumWeights;

	output[DTid.xy] = filteredDepth;

	// world coordinate
	float2 undistorted = depthFrameToCameraSpaceTable[DTid.xy];
	float depthMeters = filteredDepth / 1000; // m
	float4 depthCamera = float4(undistorted*depthMeters, depthMeters, 1);
	float4 pos = mul(world, depthCamera);

	uint index = DTid.y * depthImageWidth + DTid.x;
	worldCoordinates.Store3(index * 24, asuint(pos.xyz));
}