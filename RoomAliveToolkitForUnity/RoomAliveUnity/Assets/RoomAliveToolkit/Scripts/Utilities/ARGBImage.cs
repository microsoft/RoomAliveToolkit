using System;

namespace RoomAliveToolkit.Images
{
    public struct ARGB32
    {
        public byte B, G, R, A;
    }

    unsafe public class ARGBImage : UnmanagedImage
    {
        protected ARGB32* data;

        public ARGBImage(int width, int height)
            : base(width, height, sizeof(ARGB32))
        {
            data = (ARGB32*)dataIntPtr.ToPointer();
        }

        public ARGBImage(int width, int height, IntPtr dataIntPtr)
            : base(width, height, dataIntPtr, sizeof(ARGB32))
        {
            data = (ARGB32*)dataIntPtr.ToPointer();
        }

        public ARGB32* Data(int x, int y)
        {
            return &data[width * y + x];
        }

        public ARGB32 this[int x, int y]
        {
            get { return data[y * width + x]; }
            set { data[y * width + x] = value; }
        }

        public void Copy(ByteImage byteImage)
        {
            byte* p = byteImage.Data(0, 0);
            ARGB32* pOut = data;
            for (int i = 0; i < width * height; i++)
            {
                pOut->A = 255;
                pOut->R = pOut->G = pOut++->B = *p++;
            }
        }

        public void CopyRectangle(ByteImage byteImage, int startX, int startY, int w, int h)
        {
            byte* pOrig = byteImage.Data(0, 0);
            ARGB32* pOutOrig = data;
            byte* p;
            ARGB32* pOut;

            for (int j = startY; j < h; j++)
            {
                for (int i = startX; i < w; i++)
                {
                    p = pOrig + j * byteImage.Width + i;
                    pOut = pOutOrig + j * width + i;

                    pOut->A = 255;
                    pOut->R = pOut->G = pOut->B = *p;
                }
            }
        }

        public void CopyRectangle(ARGBImage argbImage, int startX, int startY, int w, int h)
        {
            ARGB32* pOrig = argbImage.Data(0, 0);
            ARGB32* pOutOrig = data;
            ARGB32* p;
            ARGB32* pOut;

            for (int j = startY; j < h; j++)
            {
                for (int i = startX; i < w; i++)
                {
                    p = pOrig + j * argbImage.Width + i;
                    pOut = pOutOrig + j * width + i;

                    *pOut = *p;
                }
            }
        }

        public void CopyRectangle(RGBImage rgbImage, int startX, int startY, int w, int h)
        {
            RGB24* pOrig = rgbImage.Data(0, 0);
            ARGB32* pOutOrig = data;
            RGB24* p;
            ARGB32* pOut;

            for (int j = startY; j < h; j++)
            {
                for (int i = startX; i < w; i++)
                {
                    p = pOrig + j * rgbImage.Width + i;
                    pOut = pOutOrig + j * width + i;

                    pOut->A = 255;
                    pOut->R = p->R;
                    pOut->G = p->G;
                    pOut->B = p->B;
                }
            }
        }

        public void Copy(RGBImage rgbImage)
        {
            RGB24* p = rgbImage.Data(0, 0);
            ARGB32* pOut = data;
            for (int i = 0; i < width * height; i++)
            {
                pOut->A = 255;
                pOut->R = p->R;
                pOut->G = p->G;
                pOut++->B = p++->B;
            }
        }

        public void Copy(ShortImage shortImage)
        {
            ushort* p = shortImage.Data(0, 0);
            ARGB32* pOut = data;
            for (int i = 0; i < width * height; i++)
            {
                pOut->A = 255;
                pOut->R = (byte)*p;
                pOut->G = (byte)*p;
                pOut++->B = (byte)*p++;
            }
        }

        public void InverseRGB()
        {
            ARGB32* p = data;
            for (int i = 0; i < width * height; i++)
            {
                p->A = p->A;
                p->R = (byte)(255 - p->R);
                p->G = (byte)(255 - p->G);
                p->B = (byte)(255 - p->B);
                p++;
            }
        }
    }
}