using System;

namespace RoomAliveToolkit.Images
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
    }
}