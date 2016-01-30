Texture2DArray<float> depthMaps : register(t0);
Texture2DArray<float4> colorTextures : register(t1);
Texture2DArray<float> colorDepthMaps : register(t2);
Texture2DArray<float> zBuffers : register(t3);

SamplerState colorSampler : register(s0);

struct PSInput
{
	float4 pos : SV_Position; // view space coords
	float3 world : world; // world coords
	float3 normal : NORMAL;
	float3 lightDir : LIGHTDIR;
};

struct Camera
{
	matrix worldToColor;
	float2 f;
	float2 c;
	float k1, k2;
};

struct Projector
{
	float invertedFocalLength;
	matrix viewProjection;
	float4 projectorColor;
};

cbuffer constants : register(b0)
{
	uint numProjectors;
	Projector projectors[8];
	uint numCameras;
	Camera cameras[8];
}

float2 Project(Camera camera, float4 x)
{
	float2 xp = x.xy / x.z;
	float rSq = dot(xp, xp);
	float2 xpp = xp * (1 + camera.k1 * rSq + camera.k2 * rSq * rSq);
	return camera.f*xpp + camera.c;
}

float4 main(PSInput input) : SV_Target0
{
	float4 world4 = float4(input.world, 1);
	float4 tintColor = float4(1, 1, 1, 1);
	float minPixelSize = 1E10;

	for (uint i = 0; i < numProjectors; i++)
	{
		// transform world coordinate point to projector view space
		float4 projector = mul(world4, projectors[i].viewProjection);

		// do projector coords lie within its view?
		float w = projector.w;
		if ((projector.x >= -w) && (projector.x <= w)
			&& (projector.y >= -w) && (projector.y <= w)
			&& (projector.z >= 0) && (projector.z <= w))
		{
			projector /= w;

			// predict distance to projector by looking at depth map
			float2 tex = float2((projector.x + 1.0) / 2.0, 1 - (projector.y + 1.0) / 2.0);
			//float depth0 = depthMaps.Load(int4(tex.x * 1024, tex.y * 768, i, 0)); // depth map dimensions

			float depth0 = zBuffers.Load(int4(tex.x * 1024, tex.y * 768, i, 0)); // depth map dimensions


			// depth of our vertex
			float depth1 = projector.z;

			// if very different than projector coord then projector can't "see" the point
			if (abs(depth0 - depth1) < 0.01) // note this is in far/near normalized distance, not m
			{
				// take projector with highest dpi at this distance; size of a pixel is z/f
				float pixelSize = depth1 * projectors[i].invertedFocalLength;
				if (pixelSize < minPixelSize)
				{
					minPixelSize = pixelSize;
					tintColor = projectors[i].projectorColor;
				}
			}
		}
	}

	float num = 0;
	float4 color = 0;
	for (int i = 0; i < numCameras; i++)
	{
		// color camera coords
		float4 colorCamera = mul(world4, cameras[i].worldToColor);

		// color image coords [0,1],[0,1]
		// this includes a flip in y to get to texture coordinates (y down): f_y' = -f_y, c_y' = 1 - c_y 
		float2 tex = Project(cameras[i], colorCamera);

		float depth0 = colorDepthMaps.Load(int4(tex.x * 1024, tex.y * 768, i, 0)); // depth map dimensions
		float depth1 = (colorCamera.z - 0.8) / 8.0;

		float4 thisColor = colorTextures.Sample(colorSampler, float3(tex, i));

		//float4 thisColor = depth0 * float4(1, 1, 1, 1);
		//thisColor.w = 1;

		if (abs(depth0 - depth1) < 0.01)
		if (colorCamera.z > 0.8) // necessary? (even for > 0 we get strange banding on the sides)
		{
			color += thisColor;
			// sampler border color == 0; easier than checking tex coord bounds manually
			num += thisColor.w;
		}
	}

	color = num > 0 ? color / num : float4(1, 1, 1, 1) / 4;

	return color * tintColor;
}
