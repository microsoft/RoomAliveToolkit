struct VSOutput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD;
};

// full screen quad
VSOutput main(uint id : SV_VertexID)
{
	VSOutput output = (VSOutput)0.0f;
	output.tex = float2((id << 1) & 2, id & 2);
	output.pos = float4(output.tex * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), 0.0f, 1.0f);
	return output;
}
