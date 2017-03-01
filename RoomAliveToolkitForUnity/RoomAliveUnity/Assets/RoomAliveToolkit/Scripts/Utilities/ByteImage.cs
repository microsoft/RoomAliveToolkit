using System;

namespace RoomAliveToolkit.Images
{
    unsafe public class ByteImage : UnmanagedImage
    {
        protected byte* data;

        public ByteImage(int width, int height)
            : base(width, height, sizeof(byte))
        {
            data = (byte*)dataIntPtr.ToPointer();
            Zero();
        }

        public ByteImage(int width, int height, IntPtr dataIntPtr)
            : base(width, height, dataIntPtr, sizeof(byte))
        {
            data = (byte*)dataIntPtr.ToPointer();
        }

        public byte* Data(int x, int y)
        {
            return &data[width * y + x];
        }

        public byte this[int x, int y]
        {
            get { return data[y * width + x]; }
            set { data[y * width + x] = value; }
        }

        //public Bitmap Bitmap()
        //{
        //    Bitmap bitmap = new Bitmap(width, height, width, PixelFormat.Format8bppIndexed, DataIntPtr);
        //    ColorPalette grayScalePalette = bitmap.Palette;
        //    for (int i = 0; i < 255; i++)
        //        grayScalePalette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
        //    bitmap.Palette = grayScalePalette;
        //    return bitmap;
        //}

        public double this[double x, double y]
        {
            get
            {
                // bilinear interpolation
                int x1 = (int)x;
                int x2 = (int)y;

                byte y1 = data[x2 * width + x1];
                byte y2 = data[x2 * width + x1 + 1];
                byte y3 = data[(x2 + 1) * width + x1 + 1];
                byte y4 = data[(x2 + 1) * width + x1];

                double t = x - x1;
                double u = y - x2;

                return (1 - t) * (1 - u) * y1 + t * (1 - u) * y2 + t * u * y3 + (1 - t) * u * y4;
            }
        }

        public void Decimate(ByteImage a, int factor)
        {
            byte* output = data;

            for (int y = 0; y < height; y++)
            {
                byte* pa = a.Data(0, y * factor);
                for (int x = 0; x < width; x++)
                {
                    *output++ = *pa;
                    pa += factor;
                }
            }
        }

        //public void DecimateAndReduce(RGBImage a, int factor)
        //{
        //    byte* output = data;

        //    for (int y = 0; y < height; y++)
        //    {
        //        RGB24* pa = a.Data(0, y * factor);
        //        for (int x = 0; x < width; x++)
        //        {
        //            *output++ = (*pa).R;
        //            pa += factor;
        //        }
        //    }
        //}

        //public void DecimateAndReduce(ARGBImage a, int factor)
        //{
        //    byte* output = data;

        //    for (int y = 0; y < height; y++)
        //    {
        //        ARGB32* pa = a.Data(0, y * factor);
        //        for (int x = 0; x < width; x++)
        //        {
        //            *output++ = (*pa).R;
        //            pa += factor;
        //        }
        //    }
        //}

        public void Threshold(ByteImage a, byte threshold)
        {
            byte* pa = a.data;
            byte* p = data;
            for (int i = 0; i < a.width * a.height; i++)
            {
                if (*pa++ > threshold)
                    *p++ = 255;
                else
                    *p++ = 0;
            }
        }

        public void Threshold(FloatImage a, float threshold)
        {
            float* pa = a.Data(0, 0);
            byte* p = data;
            for (int i = 0; i < width * height; i++)
            {
                if (*pa++ > threshold)
                    *p++ = 255;
                else
                    *p++ = 0;
            }
        }

        public void Threshold(ShortImage a, ushort threshold)
        {
            ushort* pa = a.Data(0, 0);
            byte* p = data;
            for (int i = 0; i < width * height; i++)
            {
                if (*pa++ > threshold)
                    *p++ = 255;
                else
                    *p++ = 0;
            }
        }

        public void ThresholdHighPass(ByteImage a, byte threshold)
        {
            byte* pa = a.data;
            byte* p = data;
            for (int i = 0; i < a.width * a.height; i++)
            {
                if (*pa > threshold)
                    *p++ = *pa;
                else
                    *p++ = 0;
                pa++;
            }
        }

        public void Copy(FloatImage a, float min, float max)
        {
            float* pa = a.Data(0, 0);
            byte* p = data;
            float s = 255.0f / (max - min);

            for (int i = 0; i < width * height; i++)
            {
                int value = (int)(s * (*pa++ - min));

                if (value < 0)
                    *p++ = 0;
                else if (value > 255)
                    *p++ = (byte)255;
                else
                    *p++ = (byte)value;
            }
        }

        public void Copy(ShortImage a, int shift)
        {
            ushort* pa = a.Data(0, 0);
            byte* p = data;

            for (int i = 0; i < width * height; i++)
                *p++ = (byte)(*pa++ >> shift);
        }

        public void Invert(ByteImage a)
        {
            byte* p0 = a.data;
            byte* p1 = data;
            for (int i = 0; i < width * height; i++)
            {
                *p1++ = (byte)(255 - *p0++);
            }
        }

        public void Invert()
        {
            this.Invert(this);
        }

        public void Set(byte val)
        {
            byte* p = data;
            for (int i = 0; i < width * height; i++)
            {
                *p++ = val;
            }
        }


    }
}