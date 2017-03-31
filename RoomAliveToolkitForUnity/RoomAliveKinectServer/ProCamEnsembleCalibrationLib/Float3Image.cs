using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoomAliveToolkit
{
    public struct Float3
    {
        public float x, y, z;
    }
    
    unsafe public class Float3Image : UnmanagedImage
    {
        Float3* data;
        public Float3Image(int width, int height)
            : base(width, height, sizeof(Float3))
        {
            data = (Float3*)dataIntPtr.ToPointer();
        }

        public Float3* Data(int x, int y)
        {
            return &data[width * y + x];
        }

        public Float3 this[int x, int y]
        {
            get { return data[y * width + x]; }
            set { data[y * width + x] = value; }
        }

        public void Copy(FloatImage x, FloatImage y, FloatImage z)
        {
            Float3* p = data;

            float* px = x.Data(0, 0);
            float* py = y.Data(0, 0);
            float* pz = z.Data(0, 0);

            for (int i = 0; i < width * height; i++)
            {
                p->x = *px++;
                p->y = *py++;
                p->z = *pz++;
                p++;
            }
        }

    }
}