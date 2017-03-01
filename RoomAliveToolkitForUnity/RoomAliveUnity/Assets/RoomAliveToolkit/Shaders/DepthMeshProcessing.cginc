//functions that are needed for DepthMeshProcessing for KinectV2
float width;
float height;

sampler2D _KinectDepthSource;
sampler2D _DepthToCameraSpaceX;
sampler2D _DepthToCameraSpaceY;
        
uniform float4x4 _CamToWorld;
uniform float4x4 _IRIntrinsics;
uniform float4x4 _RGBIntrinsics;	
uniform float4x4 _RGBExtrinsics;
uniform float4 _RGBDistCoef;
uniform float4 _IRDistCoef;

inline float2 Project_RGB(float3 pos) 
{ 
	//in the right handed coordinate system since all the camera calibration matrices are from the Kinect world
	float xp = pos[0] / pos[2];
	float yp = pos[1] / pos[2];
		 
	float fx = _RGBIntrinsics[0][0];
	float fy = _RGBIntrinsics[1][1];
	float cx = _RGBIntrinsics[2][0];
	float cy = _RGBIntrinsics[2][1];
	float k1 = _RGBDistCoef.x; //0.0f;
	float k2 = _RGBDistCoef.y; //0.0f;  
		    						
	// compute f(xp, yp)
	float rSquared = xp * xp + yp * yp;
	float xpp = xp * (1 + k1 * rSquared + k2 * rSquared * rSquared);
	float ypp = yp * (1 + k1 * rSquared + k2 * rSquared * rSquared);
	float u = fx * xpp + cx;
	float v = fy * ypp + cy;
		
	u /= 1920;
	v /= 1080;

	//Why are we inverting coordinates here? (Benko)
	//It has nothing to do with camera math, but how the images are laid out in memory. 
	//In the image space x goes right, y goes top to bottom (i.e. down)
	//However in the Kinect camera space x goes left, y goes up and z goes forward (right handed coordinate system).
	//So to perform image lookup, we need to flip x and y. No other reason. 
	//u = 1 - u; //However, since kinect depth image is the flipped version of RGB, we would need to flip u one more time, so we instead don't flip it at all.  
	v = 1 - v; 
		
	return float2(u,v);
}

inline float2 Project_IR(float3 pos)
{
	float xp = pos[0] / pos[2];
	float yp = pos[1] / pos[2];
		
	float fx = _IRIntrinsics[0][0];
	float fy = _IRIntrinsics[1][1];
	float cx = _IRIntrinsics[2][0];
	float cy = _IRIntrinsics[2][1];
	float k1 = _IRDistCoef.x; //0.0f;
	float k2 = _IRDistCoef.y; //0.0f; 
		    						
	// compute f(xp, yp)
	float rSquared = xp * xp + yp * yp;
	float xpp = xp * (1 + k1 * rSquared + k2 * rSquared * rSquared);
	float ypp = yp * (1 + k1 * rSquared + k2 * rSquared * rSquared);
	float u = fx * xpp + cx;
	float v = fy * ypp + cy;
		
	u /= 512;
	v /= 424;
	
	//see why we need to flip them in the function above
	u = 1 - u;
	v = 1 - v;	
	
	return float2(u,v);
}

//x and y is in pixels [0-511] and [0-423]
inline float GetDepth(uint x, uint y)
{
	float4 c = tex2Dlod(_KinectDepthSource, float4(x/(width - 1), y/(height - 1), 0, 0));
	float depth_l, depth_h;
	if ((x % 2) == 0)
	{
		depth_l = c.a;
		depth_h = c.r;
	}
	else
	{
		depth_l = c.g;
		depth_h = c.b;
	}
	float depth = depth_l * 255 + depth_h * 255 * 256;
	return depth / 1000.0f;
}	

//used to be used for Kinect V1
inline float GetDepth_Old(float2 uv)
{
	// we are passing in the packed ushort representation from the kinect
	// see (http://msdn.microsoft.com/en-us/library/jj131028.aspx#PlayerIndex)
	// as of v1.6 of the Kinect SDK the lower 3 bits are player indicies
	// and the upper 13 bits are the depth
	// this is passed in as an ARGB32 texture with the ushort split across
	// the r and g channels with nothing in the b ana a channels
	// get r channel - lower order bits

	float4 c = tex2Dlod(_KinectDepthSource, float4(uv, 0, 0));

	float depth_l = c.a;
	float depth_h = c.r;
	float depth = depth_l * 255 + depth_h * 255 * 256;

	depth /= 1000.0f;

	return depth;
}				

inline float3 Unproject_IR(float3 image)
{
	float4 uv = float4(image.x/512, image.y/424, 0, 0);

	float dx = DecodeFloatRGBA( tex2Dlod(_DepthToCameraSpaceX, uv) );
	float dy = DecodeFloatRGBA( tex2Dlod(_DepthToCameraSpaceY, uv) );

	dx = 2*dx - 1; // bring from [0..1) to original range
	dy = 2*dy - 1;

	float z = image.z;

	return float3(-z*dx, z*dy, z);
}
		
inline float3 CameraToWorld(float3 pos)
{
	float4 depthPt = float4(pos.x, pos.y, pos.z, 1);
	float4 worldPos = mul(_CamToWorld, depthPt);
	float x = worldPos.x / worldPos.w;
	float y = worldPos.y / worldPos.w;
	float z = worldPos.z / worldPos.w;
	return float3(x, y, z);
}

inline float3 ComputeFace(in float4 texCoord, out float3 posA, out float3 posB)
{
		uint tile = (uint) texCoord.y;
		uint id = (uint) texCoord.x;
		id = id + (511 * 8 * 6) * tile;
		  	
		// each quad is 6 vertices
		uint q = id / 6;
		
		// position of quad
		uint qx = q % (511);
		uint qy = q / (511);
		
		// vertex in quad
		uint v = id % 6;
		
		// position of vertex in quad
		uint vx, vy;
		
		// position of other vertices on the triangle, used for computing normals and 
		// testing that all depth values in the triangle are valid
		// assign a and b according to right hand rule, so that a x b gives us our normal
		uint ax, ay, bx, by;

		if(v == 0)
		{
			vx = 0; vy = 0;
		
			ax = 1; ay = 0; // 1
			bx = 0; by = 1; // 2
		}
			
		if(v == 1)
		{
			vx = 1; vy = 0;
		
			ax = 0; ay = 1; // 2
			bx = 0; by = 0; // 0
		}
			
		if(v == 2)
		{
			vx = 0; vy = 1;
		
			ax = 0; ay = 0; // 0
			bx = 1; by = 0; // 1
		}

		if(v == 3)
		{
			vx = 1; vy = 1;
		
			ax = 0; ay = 1; // 4
			bx = 1; by = 0; // 5
		} 
			
		if(v == 4)
		{ 
			vx = 0; vy = 1;
		
			ax = 1; ay = 0; // 5
			bx = 1; by = 1; // 3
		}
			
		if(v == 5)
		{
			vx = 1; vy = 0;
		
			ax = 1; ay = 1; // 3
			bx = 0; by = 1; // 4
		}
			
		//flip x because the depth images and the depthToCameraTable are flipped horizontally
		float x = 511 - (qx + vx);
		float xa = 511 - (qx + ax);
		float xb = 511 - (qx + bx);

		//if the depth images are not flipped
		//float x = qx + vx;
		//float xa = qx + ax;
		//float xb = qx + bx;

		float depth = GetDepth(x, qy + vy);
		float3 pos = Unproject_IR(float3(x, qy + vy, depth));

		float depthA = GetDepth( xa, qy + ay );
		posA = Unproject_IR(float3(xa, qy + ay, depthA));

		float depthB = GetDepth( xb, qy + by );
		posB = Unproject_IR(float3(xb, qy + by, depthB));

		return pos;
}