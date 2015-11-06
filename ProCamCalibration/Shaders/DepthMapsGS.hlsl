struct GSInput
{
	float4 pos : SV_Position;
	float depth : MYSEMANTIC;
};

struct GSOutput
{
	float4 pos : SV_Position;
	float depth : MYSEMANTIC;
	uint renderTargetIndex : SV_RenderTargetArrayIndex;
};

cbuffer constants : register(b0)
{
	// world is depth camera's pose; view and projection are that of the projector
	matrix projectorWorldViewProjection[16];
}

[maxvertexcount(3)]
void main(triangle GSInput points[3], inout TriangleStream< GSOutput > triangles)
{
	// render all triangles to each render target, each with its own view/projection matrix
	// loop over render targets
	for (int r = 0; r < 3; r++)
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
		for (int vertex = 0; vertex < 3; vertex++)
		{
			GSOutput output;

			// transform point
			float4 pos = mul(points[vertex].pos, projectorWorldViewProjection[r]);
			output.pos = pos * valid;
			output.depth = points[vertex].depth;
			output.renderTargetIndex = r;

			triangles.Append(output);
		}
	}
}


