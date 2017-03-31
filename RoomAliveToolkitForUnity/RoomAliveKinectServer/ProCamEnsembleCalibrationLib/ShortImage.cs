using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace RoomAliveToolkit
{
    unsafe public class ShortImage : UnmanagedImage
    {
        protected ushort* data;

        public ShortImage(int width, int height)
            : base(width, height, sizeof(short))
        {
            data = (ushort*)dataIntPtr.ToPointer();
            //Zero();
        }

        public ShortImage(int width, int height, IntPtr dataIntPtr)
            : base(width, height, dataIntPtr, sizeof(short))
        {
            data = (ushort*)dataIntPtr.ToPointer();
        }

        public ushort* Data(int x, int y)
        {
            return &data[width * y + x];
        }

        public ushort this[int x, int y]
        {
            get { return data[y * width + x]; }
            set { data[y * width + x] = value; }
        }

        public ushort this[int i]
        {
            get { return data[i]; }
            set { data[i] = value; }
        }

        public void Copy(FloatImage input)
        {
            float* pIn = input.Data(0, 0);
            ushort* p = data;
            for (int i = 0; i < width * height; i++)
            {
                *p++ = (ushort)(*pIn++);
            }
        }

        public void Add(ByteImage a)
        {
            byte* p_a = a.Data(0, 0);
            ushort* p = data;
            for (int i = 0; i < width * height; i++)
            {
                *p = (ushort)((*p_a) + (*p));
                p++;
                p_a++;
            }
        }
        public void Add(ShortImage a)
        {
            ushort* p_a = a.Data(0, 0);
            ushort* p = data;
            for (int i = 0; i < width * height; i++)
            {
                *p = (ushort)((*p_a) + (*p));
                p++;
                p_a++;
            }
        }
        public void YMirror(ShortImage a)
        {
            ushort* pOut = data;
            ushort* pIn = a.data;
            for (int yy = 0; yy < height; yy++)
            {
                pIn = a.Data(0, height - yy);
                for (int xx = 0; xx < width; xx++)
                {
                    *pOut++ = *pIn++;
                }
            }
        }

        public void XMirror(ShortImage a)
        {
            ushort* pOut = data;
            ushort* pIn = a.data;
            for (int yy = 0; yy < height; yy++)
            {
                pIn = a.Data(width - 1, yy);
                for (int xx = 0; xx < width; xx++)
                {
                    *pOut++ = *pIn--;
                }
            }
        }

        //this is a special function which respects the YUYV ordering when mirroring
        public void XMirror_YUYSpecial(ShortImage a)
        {
            //Y1 U Y2 V  ---> Y2 U Y1 V
            byte* pOut = (byte*)data;
            byte* pIn = (byte*)a.data;
            for (int yy = 0; yy < height; yy++)
            {
                pIn = (byte*)a.Data(width - 2, yy);

                for (int xx = 0; xx < width; xx += 2)
                {
                    *pOut++ = *(pIn + 2);
                    *pOut++ = *(pIn + 1);
                    *pOut++ = *pIn;
                    *pOut++ = *(pIn + 3);
                    pIn -= 4;
                }
            }
        }

        public void Blur3x3NonZero(ShortImage a)
        {
            ushort* input;
            ushort* output;

            ushort* pb02;
            ushort* pb12;
            ushort* pb22;

            int s0, s1, s2; //pixel values
            int c0, c1, c2; //valid pixel counts (where value > 0)
            int h, hc;

            for (int y = 0; y < height - 2; y++)
            {
                input = a.Data(1, y + 1);
                output = this.Data(1, y + 1);

                pb02 = a.Data(2, y);
                pb12 = a.Data(2, y + 1);
                pb22 = a.Data(2, y + 2);

                h = 0;
                hc = 0;

                s0 = 0; s1 = 0; s2 = 0;
                c0 = 0; c1 = 0; c2 = 0;

                for (int x = 0; x < width - 2; x++)
                {
                    h -= s0;
                    hc -= c0;

                    s0 = s1;
                    s1 = s2;

                    c0 = c1;
                    c1 = c2;

                    c2 = (((*pb02) > 0) ? 1 : 0) + (((*pb12) > 0) ? 1 : 0) + (((*pb22) > 0) ? 1 : 0);
                    s2 = *pb02++ + *pb12++ + *pb22++;

                    h += s2;
                    hc += c2;

                    int g = 0;
                    if (hc > 0) g = h / hc;

                    if (g > ushort.MaxValue)
                        g = ushort.MaxValue;

                    if (*input++ != (ushort)0) *output++ = (ushort)g; //comment this check if you want to fill holes and smooth edges
                    else *output++ = (ushort)0;
                }
            }
        }

        public void Blur5x5NonZero(ShortImage a)
        {
            ushort* input;
            ushort* output;

            ushort* pb04;
            ushort* pb14;
            ushort* pb24;
            ushort* pb34;
            ushort* pb44;

            int s0, s1, s2, s3, s4; //pixel values
            int c0, c1, c2, c3, c4; //valid pixel counts (where value > 0)
            int h, hc;

            for (int y = 0; y < height - 4; y++)
            {
                input = a.Data(2, y + 1);
                output = this.Data(2, y + 1);

                pb04 = a.Data(4, y);
                pb14 = a.Data(4, y + 1);
                pb24 = a.Data(4, y + 2);
                pb34 = a.Data(4, y + 3);
                pb44 = a.Data(4, y + 4);

                h = 0;
                hc = 0;

                s0 = 0; s1 = 0; s2 = 0; s3 = 0; s4 = 0;
                c0 = 0; c1 = 0; c2 = 0; c3 = 0; c4 = 0;

                for (int x = 0; x < width - 4; x++)
                {
                    h -= s0;
                    hc -= c0;

                    s0 = s1;
                    s1 = s2;
                    s2 = s3;
                    s3 = s4;

                    c0 = c1;
                    c1 = c2;
                    c2 = c3;
                    c3 = c4;

                    c4 = (((*pb04) > 0) ? 1 : 0) + (((*pb14) > 0) ? 1 : 0) + (((*pb24) > 0) ? 1 : 0) + (((*pb34) > 0) ? 1 : 0) + (((*pb44) > 0) ? 1 : 0);
                    s4 = *pb04++ + *pb14++ + *pb24++ + *pb34++ + *pb44++;

                    h += s4;
                    hc += c4;

                    int g = 0;
                    if (hc > 0) g = h / hc;

                    //if (g > ushort.MaxValue)
                    //    g = ushort.MaxValue;

                    if (*input++ != (ushort)0) *output++ = (ushort)g;
                    else *output++ = (ushort)0;
                }
            }
        }

        /// <summary>
        /// n should be odd otherwise this function behaves as n = n+1
        /// </summary>
        /// <param name="a"></param>
        /// <param name="n"></param>
        public void BlurNxNNonZero(ShortImage a, int n)
        {
            if (n % 2 == 0) n++;

            ushort* input;
            ushort* output;

            ushort*[] pbs = new ushort*[n];
            int[] s = new int[n];
            int[] c = new int[n];

            int h, hc;
            int sumS, sumC;
            int lastElement = n - 1;

            for (int y = 0; y < (height - (n - 1)); y++)
            {
                input = a.Data((int)(n / 2), y + 1);
                output = this.Data((int)(n / 2), y + 1);

                for (int cnt = 0; cnt < n; cnt++)
                {
                    pbs[cnt] = a.Data((n - 1), y + cnt);
                    s[cnt] = 0;
                    c[cnt] = 0;
                }

                h = 0;
                hc = 0;

                for (int x = 0; x < (width - (n - 1)); x++)
                {
                    h -= s[0];
                    hc -= c[0];

                    int i = 0;
                    for (i = 0; i < (n - 1); i++)
                    {
                        s[i] = s[i + 1];
                        c[i] = c[i + 1];
                    }

                    sumS = 0;
                    sumC = 0;
                    for (i = 0; i < n; i++)
                    {
                        ushort bsi = *pbs[i];
                        sumC += ((bsi > 0) ? 1 : 0);
                        sumS += bsi;
                        pbs[i]++;
                    }

                    c[lastElement] = sumC;
                    s[lastElement] = sumS;

                    h += sumS;
                    hc += sumC;

                    int g = 0;
                    if (hc > 0) g = h / hc;

                    //if (g > ushort.MaxValue)
                    //    g = ushort.MaxValue;

                    if (*input++ != (ushort)0)
                        *output++ = (ushort)g;
                    else
                        *output++ = (ushort)0;
                }
            }
        }

        public void SetTo(ushort val)
        {
            ushort* p = data;
            for (int i = 0; i < width * height; i++)
            {
                *p++ = val;
            }
        }
    }
}