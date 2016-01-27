Texture2D<uint> depthImage : register(t0);
RWTexture2D<float> floatDepthImage : register(u0);

[numthreads(32, 22, 1)]
void main( uint3 DTid : SV_DispatchThreadID )
{
	floatDepthImage[DTid.xy] = depthImage[DTid.xy];
}