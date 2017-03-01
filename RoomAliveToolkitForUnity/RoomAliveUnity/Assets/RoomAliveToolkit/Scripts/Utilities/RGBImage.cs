using System;


namespace RoomAliveToolkit.Images
{
    public struct RGB24
    {
        public byte B, G, R;
    }

    unsafe public class RGBImage : UnmanagedImage
    {
        protected RGB24* data;

        public RGBImage(int width, int height)
            : base(width, height, sizeof(RGB24))
        {
            data = (RGB24*)dataIntPtr.ToPointer();
        }

        public RGBImage(int width, int height, IntPtr dataIntPtr)
            : base(width, height, dataIntPtr, sizeof(RGB24))
        {
            data = (RGB24*)dataIntPtr.ToPointer();
        }

        public RGB24* Data(int x, int y)
        {
            return &data[width * y + x];
        }

        public RGB24 this[int x, int y]
        {
            get { return data[y * width + x]; }
            set { data[y * width + x] = value; }
        }

        public void Copy(ByteImage byteImage)
        {
            byte* p = byteImage.Data(0, 0);
            RGB24* pOut = data;
            for (int i = 0; i < width * height; i++)
            {
                pOut->R = pOut->G = pOut++->B = *p++;
            }
        }

    }
}
