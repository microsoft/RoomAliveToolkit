
cbuffer constants : register(b0)
{
	matrix worldToColor;
	float2 f;
	float2 c;
	float k1, k2;
}

float4 Project(float4 x)
{
	float2 xp = x.xy / x.z;
	float rSq = dot(xp, xp);

	float4 output;
	output.xy = f*xp + c;
	output.zw = f*xp*(k1 * rSq + k2 * rSq * rSq); // distortion stored separately
	return output;
}

float4 main(float3 world : pos) : SV_Position
{
	float4 world4 = float4(world, 1);
	float4 colorCamera = mul(world4, worldToColor);

	// color image coords [0,1],[0,1]
	// this includes a flip in y to get to texture coordinates (y down): f_y' = -f_y, c_y' = 1 - c_y 
	float4 p = Project(colorCamera);

	// adding large amounts of distortion can put far away vertices into our frustum, so only add it if small
	float2 tex = p.xy + p.zw * (length(p.zw) < 1);
	
	// project to view volume where x, y in [-1, 1], z in [0, 1], x right, y up, z forward
	// TODO: go directly to view coords
	float z = (colorCamera.z - 0.8) / 8.0;

	return float4((2 * tex.x - 1), -(2 * tex.y - 1), z, 1);
}
