Texture2D<float> inputTexture : register(t0);

struct PSInput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD;
};

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

float main(PSInput input, float4 screenSpace : SV_Position) : SV_Target0
{
	float sum = 0;
	float sumWeights = 0;

	// SV_Position semantic is screenspace coords with 0.5 offset, truncating to int3 will remove this effect
	float x = screenSpace.x;
	float y = screenSpace.y;

	float value0 = inputTexture.Load(int3(x, y, 0));

	for (float dy = -halfWidth; dy <= halfWidth; dy++)
		for (float dx = -halfWidth; dx <= halfWidth; dx++)
		{
			float value = inputTexture.Load(int3(x + dx, y + dy, 0));
			// TODO: gaussians can be combined to avoid a call to exp (lift dy first?): exp(x)*exp(y) = exp(x+y)
			float weight = Gaussian(dx, spatialSigma) * Gaussian(dy, spatialSigma) * Gaussian(value - value0, intensitySigma);
			sum += weight * value;
			sumWeights += weight;
		}
	sum /= sumWeights;

	return sum;
}