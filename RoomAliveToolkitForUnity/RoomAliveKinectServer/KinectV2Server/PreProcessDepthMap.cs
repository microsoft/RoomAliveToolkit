using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using RoomAliveToolkit;

namespace KinectV2Server
{
    public class PreProcessDepthMap_Conservative
    {
        // accumulator depth image
        private FloatImage mAcc32;
        // number of valid points for this depth
        private ShortImage mCount16;
        // a mask computed each frame
        private ByteImage mMask;

        private int mNumFrames;
        private int mFrameIter;
        private float mValidDelta;

        public PreProcessDepthMap_Conservative(int width, int height, int numFrames, float validDelta)
        {
            mAcc32 = new FloatImage(width, height);
            mMask = new ByteImage(width, height);
            mMask.SetTo(1);
            mCount16 = new ShortImage(width, height);
            mCount16.SetTo(0);

            mValidDelta = validDelta;

            if (numFrames < 1 || numFrames >= 655536)
                throw new System.ArgumentException("PreProcessDepthMap numFrames must be >= 1 && < 655536");
            mNumFrames = numFrames;

            mFrameIter = 0;
        }

        public bool Update(ShortImage depth)
        {
            // compute the raw valid pixels for this frame
            ByteImage maskCur = Mask1(depth);

            // initialize the acc image if we need to
            if (mFrameIter == 0)
            {
                mAcc32.Add(depth);
                mCount16.Add(maskCur);
                mMask.Copy(maskCur);
                mFrameIter++;
                return mFrameIter >= mNumFrames;
            }

            // compute the pixels that don't differ from previous values
            ByteImage diffMask = DiffMask(depth, mAcc32, mFrameIter, mValidDelta * 1000.0f);

            // update the rolling depthMask
            mMask.And(maskCur);
            mMask.And(diffMask);
            mCount16.Add(maskCur);
            mAcc32.Add(depth);
            mFrameIter++;

            maskCur.Dispose();
            diffMask.Dispose();

            return mFrameIter >= mNumFrames;
        }


        /// <summary>
        /// Compute a mask representing pixels that are similiar to the current average pixels computed
        /// with acc32/count
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="acc32"></param>
        /// <param name="count32"></param>
        /// <returns></returns>
        public static ByteImage DiffMask(ShortImage depthIm, FloatImage acc32, int count, float delta)
        {
            ByteImage depthMask = new ByteImage(depthIm.Width, depthIm.Height);
            // all values zero by default

            for (int r = 0; r < depthIm.Height; r++)
            {
                for (int c = 0; c < depthIm.Width; c++)
                {
                    ushort depthVal = depthIm[c, r];
                    float depthAvgVal = acc32[c, r] / count;
                    float diff = Math.Abs(depthVal - depthAvgVal);

                    if (diff < delta)
                        depthMask[c, r] = 1;
                }
            }

            return depthMask;
        }

        /// <summary>
        /// Return 1 for valid 0 for invalid pts
        /// NOTE: NOT 0/255
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static ByteImage Mask1(ShortImage depth)
        {
            // now erode the results (to correct for any errors in Kinect Depth -> RGB calibration
            // create a mask to erode
            ByteImage depthMask = new ByteImage(depth.Width, depth.Height);
            // intialize the depth mask to be the valid Kinect data
            for (int r = 0; r < depth.Height; r++)
            {
                for (int c = 0; c < depth.Width; c++)
                {
                    if (depth[c, r] > 0.0f)
                        depthMask[c, r] = 1;
                    else
                        depthMask[c, r] = 0;
                }
            }

            return depthMask;
        }

        public void Compute(out FloatImage avgDepthIm, out ByteImage mask)
        {
            avgDepthIm = new FloatImage(mAcc32.Width, mAcc32.Height);
            avgDepthIm.Copy(mAcc32);

            for (int r = 0; r < avgDepthIm.Height; r++)
            {
                for (int c = 0; c < avgDepthIm.Width; c++)
                {
                    if (mCount16[c, r] >= 1)
                        avgDepthIm[c, r] = (float)(avgDepthIm[c, r]) / mCount16[c, r];
                    else
                        avgDepthIm[c, r] = 0.0f;
                    // brett temp debugging
                    //if (avgDepthIm[c, r] == 0 && mMask[c, r] != 0)
                    //    throw new Exception("Should not happen");
                }
            }

            // convert the mask to [0,255]
            mMask.Mult(255);
            mask = mMask;
        }

        public void Reset()
        {
            mAcc32.SetTo(0.0f);
            mMask.SetTo(1);
            mFrameIter = 0;
            mCount16.SetTo(0);
        }
    }

    public class PreProcessDepthMap_Variance
    {
        FloatImage sum;
        FloatImage sumSquare;
        ShortImage n;


        public PreProcessDepthMap_Variance(int width, int height)
        {
            sum = new FloatImage(width, height);
            sum.SetTo(0);
            sumSquare = new FloatImage(width, height);
            sumSquare.SetTo(0);
            n = new ShortImage(width, height);
            n.SetTo(0);
        }

        public void Update(ShortImage depth)
        {
            for (int r = 0; r < depth.Height; r++)
            {
                for (int c = 0; c < depth.Width; c++)
                {
                    float d = (float)depth[c, r] / 1000f; // meters
                    if (d != 0)
                    {
                        sum[c, r] += d;
                        sumSquare[c, r] += d * d;
                        n[c, r]++;
                    }
                }
            }
        }

        public void Compute(out FloatImage avgDepthIm, out FloatImage varDepthIm, out ByteImage mask)
        {
            avgDepthIm = new FloatImage(sum.Width, sum.Height);
            varDepthIm = new FloatImage(sum.Width, sum.Height);
            mask = new ByteImage(sum.Width, sum.Height);

            for (int r = 0; r < sum.Height; r++)
            {
                for (int c = 0; c < sum.Width; c++)
                {
                    if (n[c, r] > 10) // need at least a few samples
                    {
                        float mean = sum[c, r] / n[c, r];
                        float variance = sumSquare[c, r] / n[c, r] - mean * mean;

                        varDepthIm[c, r] = variance;

                        if (variance < 0.1 * 0.1)
                        {
                            mask[c, r] = 255;
                            avgDepthIm[c, r] = mean;
                        }
                        else
                        {
                            mask[c, r] = 0;
                            avgDepthIm[c, r] = 0;
                        }
                    }
                    else
                    {
                        mask[c, r] = 0;
                    }
                }
            }
        }

        public void Reset()
        {
            sum.SetTo(0);
            sumSquare.SetTo(0);
            n.SetTo(0);
        }

    }

}

