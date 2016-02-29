Texture2DArray<float> depthMaps : register(t0);

struct PSInput
{
	float4 pos : SV_Position; // view space coords
	float3 world : world; // world coords
	float3 normal : NORMAL;
};

struct Projector
{
	float focalLength;
	matrix viewProjection;
	float3 position;
};

cbuffer constants : register(b0)
{
	uint numProjectors;
	uint thisProjector;
	Projector projectors[8];
}

float4 main(PSInput input) : SV_Target0
{
	float4 world4 = float4(input.world, 1);

	float blend[8];
	float sum;

	for (uint i = 0; i < numProjectors; i++)
	{
		// transform world coordinate point to projector view space
		float4 projector = mul(world4, projectors[i].viewProjection);

		blend[i] = 0;

		// do projector coords lie within its view?
		float w = projector.w;
		if ((projector.x >= -w) && (projector.x <= w)
			&& (projector.y >= -w) && (projector.y <= w)
			&& (projector.z >= 0) && (projector.z <= w))
		{
			projector /= w;

			// predict distance to projector by looking at depth map
			float2 tex = float2((projector.x + 1.0) / 2.0, 1 - (projector.y + 1.0) / 2.0);
			float depth0 = depthMaps.Load(int4(tex.x * 1024, tex.y * 768, i, 0)); // depth map dimensions

			// depth of our vertex
			float depth1 = projector.z;

			// if very different than projector coord then projector can't "see" the point
			if (abs(depth0 - depth1) < 0.01) // note this is in far/near normalized distance, not m
			{
				// normal calculation
				float3 normal = normalize(input.normal);
				float3 projectorDirection = normalize(projectors[i].position - input.world);
				blend[i] = pow(saturate(dot(normal, projectorDirection)), 5);

				//// size of a pixel is z/f
				//float pixelSize = projectors[i].focalLength / depth1 / 10000000;
				//blend[i] = pow(pixelSize, 19.0);

				//float distance = sqrt(projector.x*projector.x + projector.y*projector.y);
				//blend[i] = pow(1.0 / distance, 20);

				float distancex = (projector.x > 0) ? (1 - projector.x) : (projector.x + 1);
				float distancey = (projector.y > 0) ? (1 - projector.y) : (projector.y + 1);

				blend[i] = pow(distancex*distancey, 5) + 0.01;


			}
		}

		sum += blend[i];
	}

	float myBlend = blend[thisProjector] / sum;

	//uint max = 0;
	//uint maxi = 0;
	//for (uint i = 0; i < numProjectors; i++)
	//{
	//	if (blend[i] > max)
	//	{
	//		max = blend[i];
	//		maxi = i;
	//	}
	//	blend[i] /= sum;
	//}

	//float myBlend = 0.1*blend[thisProjector] + 0.9 * (thisProjector == maxi ? 1.0 : 0);

	float4 color = 0;
	
	if (sum > 0)
		color = myBlend * float4(1, 1, 1, 1);
	else if (sum == 0)
		color = float4(1, 0, 0, 0);

	return color;
}
