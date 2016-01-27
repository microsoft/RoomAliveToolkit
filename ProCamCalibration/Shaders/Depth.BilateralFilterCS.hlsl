Texture2D<float> input : register(t0);
RWTexture2D<float> output : register(u0);

cbuffer constants : register(b0)
{
	float spatialSigma; // pixels, 1/sigma
	float intensitySigma; // m, 1/sigma
}

#define PI 3.14159265358979323846

float Gaussian(float x, float sigma)
{
	float y = x * sigma; // 1/sigma
	return sigma / (sqrt(2 * PI)) * exp(-0.5*y*y); // lift exp(-0.5)?
}

#define halfWidth 4

[numthreads(32, 22, 1)]
void main( uint3 DTid : SV_DispatchThreadID )
{
	float sum = 0;
	float sumWeights = 0;

	float value0 = input[DTid.xy];

	for (int dy = -halfWidth; dy <= halfWidth; dy++)
		for (int dx = -halfWidth; dx <= halfWidth; dx++)
		{
			float value = input[DTid.xy + int2(dx, dy)];
			// TODO: gaussians can be combined to avoid a call to exp (lift dy first?): exp(x)*exp(y) = exp(x+y)
			float weight = Gaussian(dx, spatialSigma) * Gaussian(dy, spatialSigma) * Gaussian(value - value0, intensitySigma);
			sum += weight * value;
			sumWeights += weight;
		}
	sum /= sumWeights;

	output[DTid.xy] = sum;
}