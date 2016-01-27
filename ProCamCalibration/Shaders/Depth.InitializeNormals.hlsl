RWByteAddressBuffer worldCoordinates : register(u0); 

static const int depthImageWidth = 512;

[numthreads(32, 22, 1)]
void main( uint3 DTid : SV_DispatchThreadID )
{
	uint index = DTid.y * depthImageWidth + DTid.x;
	worldCoordinates.Store3(index * 24 + 12, uint3(0,0,0));
}