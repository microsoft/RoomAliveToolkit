using System;


namespace RoomAliveToolkit.Images
{
    public struct Float2
    {
        public float x;
        public float y;

        public static Float2 ToFloat2(float x, float y)
        {
            Float2 retVal = new Float2();
            retVal.x = x;
            retVal.y = y;
            return retVal;
        }



        public static Float2 Mult(Float2 a, float s) { return ToFloat2(a.x * s, a.y * s); }

        public static float Dot(Float2 a, Float2 b) { return a.x * b.x + a.y * b.y; }

        public static float Norm(Float2 a) { return (float)(Math.Sqrt(Dot(a, a))); }
        public static float NormSq(Float2 a) { return Dot(a, a); }

        public static float Cross(Float2 a, Float2 b) { return a.x * b.y - a.y * b.x; }

        public static Float2 Sub(Float2 a, Float2 b) { return ToFloat2(a.x - b.x, a.y - b.y); }
        public static Float2 Add(Float2 a, Float2 b) { return ToFloat2(a.x + b.x, a.y + b.y); }
        public static Float2 AddScale(Float2 a, Float2 b, float s) { return ToFloat2(a.x + s * b.x, a.y + s * b.y); }

        public static Float2 Scale(Float2 a, float s)
        {
            Float2 retVal = new Float2();
            retVal.x = a.x * s;
            retVal.y = a.y * s;
            return retVal;
        }
    }
    

    unsafe public class Float2Image : UnmanagedImage
    {
        Float2* data;
        public Float2Image(int width, int height)
            : base(width, height, sizeof(Float2))
        {
            data = (Float2*)dataIntPtr.ToPointer();
        }

        public Float2* Data(int x, int y)
        {
            return &data[width * y + x];
        }

        public Float2 this[int x, int y]
        {
            get { return data[y * width + x]; }
            set { data[y * width + x] = value; }
        }

        public void Copy(FloatImage x, FloatImage y)
        {
            Float2* p = data;

            float* px = x.Data(0, 0);
            float* py = y.Data(0, 0);

            for (int i = 0; i < width * height; i++)
            {
                p->x = *px++;
                p->y = *py++;
                p++;
            }
        }
        
      
        public void SetTo(float x, float y)
        {
            Float2* p = data;
            for (int i = 0; i < width * height; i++)
            {
                p->x = x;
                p->y = y;
                p++;
            }
        }

        public Float2Image SubSample(int offset)
        {
            // error checking
            if (offset <= 0)
                throw new ArgumentException("Float2Image offset must be >= 1");

            Float2Image subIm = new Float2Image(width / offset, height / offset);
            for (int r = 0; r < subIm.Height; r++)
            {
                for (int c = 0; c < subIm.Width; c++)
                {
                    subIm.Data(c, r)->x = this[c * offset, r * offset].x;
                    subIm.Data(c, r)->y = this[c * offset, r * offset].y;
                }
            }
            return subIm;
        }

        public float[] ToArray()
        {
            float[] arr = new float[width * height * 2];
            fixed (float* p = arr)
            {
                CopyTo((IntPtr)p);
            }
            return arr;
        }

        public void FromArray(float[] arr)
        {
            fixed (float* p = arr)
            {
                Copy((IntPtr)p);
            }
        }
    }
}