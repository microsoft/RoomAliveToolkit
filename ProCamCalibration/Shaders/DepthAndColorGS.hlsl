struct GSInput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
	float depth : MYSEMANTIC;
};

struct GSOutput
{
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
	float depth : MYSEMANTIC;
};

[maxvertexcount(3)]
void main(triangle GSInput points[3], inout TriangleStream< GSOutput > output)
{
	// test the triangle; avoid dynamic branching

	// A triangle is valid if all its points are nonzero, and each point is close to each other in 
	// depth (i.e., they do not straddle a large depth discontinuity).

	float nonZero = (points[0].depth * points[1].depth * points[2].depth) > 0 ? 1 : 0;

	float near01 = abs(points[0].depth - points[1].depth) < 0.1 ? 1 : 0;
	float near02 = abs(points[0].depth - points[2].depth) < 0.1 ? 1 : 0;
	float near12 = abs(points[1].depth - points[2].depth) < 0.1 ? 1 : 0;

   	float valid = nonZero * near01 * near02 * near12;

	// place invalid triangles at the origin
	points[0].pos *= valid;
	points[1].pos *= valid;
	points[2].pos *= valid;

	output.Append(points[0]);
	output.Append(points[1]);
	output.Append(points[2]);
}
