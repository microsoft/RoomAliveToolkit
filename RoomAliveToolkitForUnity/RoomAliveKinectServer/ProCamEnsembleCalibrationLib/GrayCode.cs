using System;
using RoomAliveToolkit;

namespace RoomAliveToolkit
{
    public class GrayCode
    {
        public int width, height;
        public int numXBits, numYBits;

        public GrayCode(int width, int height)
        {
            this.width = width;
            this.height = height;

            numXBits = (int)Math.Ceiling(Math.Log((double)width, 2.0));
            numYBits = (int)Math.Ceiling(Math.Log((double)height, 2.0));
        }

        public ARGBImage[] Generate()
        {
            var images = new ARGBImage[2 * (numXBits + numYBits)];
            int j = 0;
            for (int i = 0; i < numXBits; i++)
            {
                var image = new ARGBImage(width, height);

                // present most significant bit first
                images[j++] = GenerateX(image, numXBits - i - 1);

                var invertedImage = new ARGBImage(width, height);
                invertedImage.Copy(image);
                invertedImage.InverseRGB();
                images[j++] = invertedImage;
            }

            for (int i = 0; i < numYBits; i++)
            {
                var image = new ARGBImage(width, height);

                // present most significant bit first
                images[j++] = GenerateY(image, numYBits - i - 1);

                var invertedImage = new ARGBImage(width, height);
                invertedImage.Copy(image);
                invertedImage.InverseRGB();
                images[j++] = invertedImage;
            }
            return images;
        }

        public unsafe ARGBImage GenerateX(ARGBImage image, int i)
        {
            ARGB32* p = image.Data(0, 0);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Gray code changes only one bit from one column/row to the next column/row
                    int grayCode = x ^ (x >> 1);
                    // pick out the bit for this image
                    int bit = (grayCode & (1 << i)) >> i;

                    p->R = (byte)(255 * bit);
                    p->G = (byte)(255 * bit);
                    p->B = (byte)(255 * bit);
                    p->A = (byte)(255);
                    p++;
                }
            }
            return image;
        }

        public unsafe ARGBImage GenerateY(ARGBImage image, int i)
        {
            ARGB32* p = image.Data(0, 0);
            for (int y = 0; y < height; y++)
            {
                // Gray code changes only one bit from one column/row to the next column/row
                int grayCode = y ^ (y >> 1);
                // pick out the bit for this image
                int bit = (grayCode & (1 << i)) >> i;

                for (int x = 0; x < width; x++)
                {
                    p->R = (byte)(255 * bit);
                    p->G = (byte)(255 * bit);
                    p->B = (byte)(255 * bit);
                    p->A = (byte)(255);
                    p++;
                }
            }
            return image;
        }


        public unsafe void Decode(ByteImage[] capturedImages, ShortImage decodedX, ShortImage decodedY, ByteImage mask)
        {
            mask.Set(255); // cumulative across X and Y

            Decode(capturedImages, decodedX, mask, numXBits, width);

            // TODO: this is a little awkward
            var Yimages = new ByteImage[numYBits*2];
            for (int i = 0; i < numYBits*2; i++)
                Yimages[i] = capturedImages[numXBits * 2 + i];

            Decode(Yimages, decodedY, mask, numYBits, height);
        }


        // BEWARE: threshold on absdiff, and mask level settings*
        public unsafe void Decode(ByteImage[] capturedImages, ShortImage decoded, ByteImage mask, int nBits, int max)
        {
            decoded.Zero();

            int capturedWidth = decoded.Width;
            int capturedHeight = decoded.Height;

            // stores decoded bit from previous level
            var bits = new ByteImage(capturedWidth, capturedHeight);

            for (int i = 0; i < nBits; i++)
            {
                var capturedImage = capturedImages[2 * i];
                var invertedCapturedImage = capturedImages[2 * i + 1];

                int bitValue = (int)Math.Pow(2.0, nBits - i - 1);

                ushort* decodedp = decoded.Data(0, 0);
                byte* capturedp = capturedImage.Data(0, 0);
                byte* invertedp = invertedCapturedImage.Data(0, 0);
                byte* maskp = mask.Data(0, 0);
                byte* bitsp = bits.Data(0, 0);

                for (int y = 0; y < capturedHeight; y++)
                    for (int x = 0; x < capturedWidth; x++)
                    {
                        // a bit is considered valid if the diff is greater than some threshold; this value is tricky to set given AGC
                        byte valid = (Math.Abs(*capturedp - *invertedp) > 10) ? (byte)255 : (byte)0;
                        byte bit = (*capturedp >= *invertedp) ? (byte)255 : (byte)0;
                        // Gray code bit
                        *bitsp = (byte)(bit ^ *bitsp);
                        if (*bitsp == 255)
                            *decodedp = (ushort)(*decodedp + bitValue);

                        // stop updating the mask for the least significant levels (but continue decoding)
                        // *FIX: this is pretty fragile, perhaps better to record how many bits of rows and column have been recorded and walk back from that
                        if (i < nBits - 4)
                            *maskp = (byte)(valid & (*maskp));

                        decodedp++;
                        capturedp++;
                        invertedp++;
                        maskp++;
                        bitsp++;
                    }
            }
            bits.Dispose();

            // check that decoded values are within range
            for (int y = 0; y < capturedHeight; y++)
                for (int x = 0; x < capturedWidth; x++)
                {
                    int d = decoded[x, y]; // can this be negative?
                    if ((d >= max) || (d < 0))
                        mask[x, y] = 0;
                }
        }


    }
}
