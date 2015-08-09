Texture2D<float4> inputTexture : register(t0);
SamplerState colorSampler : register(s0);

struct PSInput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD;
};

cbuffer constants : register(b0)
{
	float alpha;
}


float4 HSV_to_RGB(float4 hsv)
{
	float4 color = 0;
		float f, p, q, t;
	float h, s, v;
	float r = 0, g = 0, b = 0;
	float i;
	if (hsv[1] == 0)
	{
		if (hsv[2] != 0)
		{
			color = hsv[2];
		}
	}
	else
	{
		h = hsv.x * 360.0;
		s = hsv.y;
		v = hsv.z;
		if (h == 360.0)
		{
			h = 0;
		}
		h /= 60;
		i = floor(h);
		f = h - i;
		p = v * (1.0 - s);
		q = v * (1.0 - (s * f));
		t = v * (1.0 - (s * (1.0 - f)));
		if (i == 0)
		{
			r = v;
			g = t;
			b = p;
		}
		else if (i == 1)
		{
			r = q;
			g = v;
			b = p;
		}
		else if (i == 2)
		{
			r = p;
			g = v;
			b = t;
		}
		else if (i == 3)
		{
			r = p;
			g = q;
			b = v;
		}
		else if (i == 4)
		{
			r = t;
			g = p;
			b = v;
		}
		else if (i == 5)
		{
			r = v;
			g = p;
			b = q;
		}
		color.r = r;
		color.g = g;
		color.b = b;
	}
	return color;
}

float4 RGB_to_HSV(float4 color)
{
	float r, g, b, delta;
	float colorMax, colorMin;
	float h = 0, s = 0, v = 0;
	float4 hsv = 0;
		r = color[0];
	g = color[1];
	b = color[2];
	colorMax = max(r, g);
	colorMax = max(colorMax, b);
	colorMin = min(r, g);
	colorMin = min(colorMin, b);
	v = colorMax; // this is value
	if (colorMax != 0)
	{
		s = (colorMax - colorMin) / colorMax;
	}
	if (s != 0) // if not achromatic
	{
		delta = colorMax - colorMin;
		if (r == colorMax)
		{
			h = (g - b) / delta;
		}
		else if (g == colorMax)
		{
			h = 2.0 + (b - r) / delta;
		}
		else // b is max
		{
			h = 4.0 + (r - g) / delta;
		}
		h *= 60;
		if (h < 0)
		{
			h += 360;
		}
		hsv[0] = h / 360.0; // moving h to be between 0 and 1.
		hsv[1] = s;
		hsv[2] = v;
	}
	return hsv;
}

#define PI 3.14159265358979323846

float4 main(PSInput input, float4 screenSpace : SV_Position) : SV_Target0
{
	{
		float2 center = float2(0.5, 0.5);
		float radius = 0.1;
		float offsetScale = 0.05;
		float phaseScale = 1.0;
		float phase = phaseScale * alpha * 2 * PI;
		float dist = distance(center, input.tex);
		float angle = dist / radius * 2 * PI + phase;
		float sinVal = (1.0 - cos(angle)) / 2.0;
		float scale = sin(alpha * PI);
		float offset = sinVal * offsetScale * scale;
		float2 toCenter = center - input.tex;
			normalize(toCenter);
		input.tex += offset * toCenter;

	}

	{
		// cartoon shader
		float4 color = inputTexture.Sample(colorSampler, input.tex);

		float4 hsv = RGB_to_HSV(color);

		// boost saturation - alot
		hsv[1] = hsv[1] * 2.0;
		hsv[1] = min(hsv[1], 1.0);

		// boost contrast (range of value)
		// scale v [0,1] -> v [-0.5, 1.5]
		hsv[2] = (hsv[2] * 2.0) - 0.5;
		hsv[2] = min(max(hsv[2], 0.0), 1.0);

		float4 boost_color = HSV_to_RGB(hsv);

		float offset = 0.0002; // 0.001;

		float4 color_x = inputTexture.Sample(colorSampler, float2(input.tex.x + offset, input.tex.y));
		float4 edge_x = abs(color - color_x);

		float4 color_y = inputTexture.Sample(colorSampler, float2(input.tex.x, input.tex.y + offset));
		float4 edge_y = abs(color - color_y);

		float4 new_color = (edge_x * 0.5 + edge_y * 0.5);
		float maxVal = max(new_color.r, max(new_color.b, new_color.g));
		maxVal = (maxVal * maxVal) * 600;
		maxVal = min(max(maxVal, 0.0), 1.0);

		new_color = boost_color - float4(maxVal, maxVal, maxVal, 1.0);
		new_color = min(max(new_color, 0.0), 1.0);

		return new_color;
	}

//	return inputTexture.Sample(colorSampler, input.tex);

}