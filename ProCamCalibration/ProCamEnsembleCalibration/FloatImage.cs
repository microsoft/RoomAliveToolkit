using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoomAliveToolkit
{
    unsafe public class FloatImage : UnmanagedImage
    {
        float* data;
        public FloatImage(int width, int height)
            : base(width, height, sizeof(float))
        {
            data = (float*)dataIntPtr.ToPointer();
        }

        public FloatImage(int width, int height, IntPtr dataIntPtr)
            : base(width, height, dataIntPtr, sizeof(float))
        {
            data = (float*)dataIntPtr.ToPointer();
        }

        public float* Data(int x, int y)
        {
            return &data[width * y + x];
        }

        public float this[int x, int y]
        {
            get { return data[y * width + x]; }
            set { data[y * width + x] = value; }
        }

        public double this[double x, double y]
        {
            get
            {
                // bilinear interpolation
                int x1 = (int)x;
                int x2 = (int)y;

                float y1 = data[x2 * width + x1];
                float y2 = data[x2 * width + x1 + 1];
                float y3 = data[(x2 + 1) * width + x1 + 1];
                float y4 = data[(x2 + 1) * width + x1];

                double t = x - x1;
                double u = y - x2;

                return (1 - t) * (1 - u) * y1 + t * (1 - u) * y2 + t * u * y3 + (1 - t) * u * y4;
            }
        }

        public void Copy(ByteImage a)
        {
            byte* pa = a.Data(0, 0);
            float* p = data;
            for (int i = 0; i < width * height; i++)
                *p++ = *pa++;
        }

        public void Copy(ByteImage a, float scale)
        {
            byte* pa = a.Data(0, 0);
            float* p = data;
            for (int i = 0; i < width * height; i++)
                *p++ = *pa++ * scale;
        }

        public float Min()
        {
            float min = Single.MaxValue;
            float* p = data;
            for (int i = 0; i < width * height; i++)
            {
                float value = *p++;
                if (value < min)
                {
                    min = value;
                }
            }
            return min;
        }
        public float Max()
        {
            float max = Single.MinValue;
            float* p = data;
            for (int i = 0; i < width * height; i++)
            {
                float value = *p++;
                if (value > max)
                {
                    max = value;
                }
            }
            return max;
        }


        public void Blur3x3(FloatImage a)
        {
            float* output = data + width + 1;

            float* pb02 = a.data + 2;
            float* pb12 = a.data + width + 2;
            float* pb22 = a.data + 2 * width + 2;

            float s0, s1, s2;
            float h;

            for (int y = 0; y < height - 2; y++)
            {
                h = 0;
                s0 = 0; s1 = 0; s2 = 0;
                for (int x = 0; x < width; x++)
                {
                    h -= s0;

                    s0 = s1;
                    s1 = s2;

                    s2 = *pb02++ + *pb12++ + *pb22++;

                    h += s2;
                    float g = h / 9;

                    *output++ = (float)g;
                }
            }
        }
    }
}