using System;
using System.Diagnostics;

namespace RoomAliveToolkit
{
    public class FrameRate
    {
        public FrameRate(long intervalMilliseconds) : this(intervalMilliseconds, "Framerate = {0:0.00}") {}

        public FrameRate(long intervalMilliseconds, string formatString)
        {
            this.intervalMilliseconds = intervalMilliseconds;
            this.formatString = formatString;
        }

        public bool Tick()
        {
            ticks++;
            if (!stopwatch.IsRunning)
                stopwatch.Start();
            long nowMilliseconds = stopwatch.ElapsedMilliseconds;
            if ((nowMilliseconds - startMilliseconds) > intervalMilliseconds)
            {
                frameRate = (float)ticks / (float)(nowMilliseconds - startMilliseconds) * 1000.0f;
                ticks = 0;
                startMilliseconds = nowMilliseconds;
                updated = true;
                return true;
            }
            return false;
        }

        public void PrintMessage()
        {
            if (updated)
            {
                Console.WriteLine(this);
                updated = false;
            }
        }

        public override string ToString()
        {
            return String.Format(formatString, frameRate);
        }

        public float frameRate = 0;
        Stopwatch stopwatch = new Stopwatch();
        int ticks = 0;
        long intervalMilliseconds;
        bool updated = false;
        long startMilliseconds = 0;
        string formatString;
    }
}
