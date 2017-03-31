using System;
using System.Runtime.InteropServices;

namespace RoomAliveToolkit
{
    public class FrameRate
    {
        public FrameRate(float i)
        {
            QueryPerformanceFrequency(ref freq);
            QueryPerformanceCounter(ref startTime);
            interval = (long)(i * freq);
        }

        public bool Tick()
        {
            ticks++;

            long timeNow = 0;
            QueryPerformanceCounter(ref timeNow);

            if ((timeNow - startTime) > interval)
            {
                frameRate = (float)ticks / (float)(timeNow - startTime) * freq;
                frameRate = ((float)((int)(frameRate * 100.0f))) / 100.0f;
                ticks = 0;
                startTime = timeNow;
                updated = true;
                return true;
            }
            return false;
        }

        public void PrintMessage(string type)
        {
            if (updated)
            {
                Console.WriteLine("{0} framerate = {1}", type, frameRate);
                updated = false;
            }
        }

        long startTime = 0;
        int ticks = 0;
        long interval;
        bool updated = false;
        double frameRate;

        public double Framerate
        {
            get { return frameRate; }
        }

        long freq = 0;

        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceCounter(ref long x);

        [DllImport("kernel32.dll")]
        extern static short QueryPerformanceFrequency(ref long x);
    }
}