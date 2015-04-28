using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace RoomAliveToolkit
{
    unsafe public class UnmanagedImage
    {
        // generics would be nice here, but can't even do sizeof on
        // generic T even when using 'struct' constraint.

        protected int width, height;
        protected int bytesPerPixel;
        protected IntPtr dataIntPtr;
        protected bool freeOnDispose; // did we allocate the memory?
        protected bool disposed = false;

        public UnmanagedImage(int width, int height, int bytesPerPixel)
        {
            this.width = width;
            this.height = height;
            this.bytesPerPixel = bytesPerPixel;
            int nbytes = width * height * bytesPerPixel;
            if ((nbytes % 16) != 0)
                nbytes += 16 - nbytes % 16;
            dataIntPtr = Win32._aligned_malloc((UIntPtr)nbytes, (UIntPtr)16);
            Win32.ZeroMemory(dataIntPtr, (UIntPtr)nbytes);
            freeOnDispose = true;
        }

        public UnmanagedImage(int width, int height, IntPtr dataIntPtr, int bytesPerPixel)
        {
            this.width = width;
            this.height = height;
            this.bytesPerPixel = bytesPerPixel;
            this.dataIntPtr = dataIntPtr;
            freeOnDispose = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // dispose of managed objects here
                    // e.g memberVariable.Dispose();

                    // *there are none*
                }

                // Dispose managed resources.
                if (freeOnDispose)
                    Win32._aligned_free(dataIntPtr);

                // Note disposing has been done.
                disposed = true;
            }
        }

        ~UnmanagedImage()
        {
            Dispose(false);
        }

        public IntPtr DataIntPtr
        {
            get { return dataIntPtr; }
            //set 
            //{ 
            //    dataIntPtr = value;
            //    //data = (byte*)dataIntPtr.ToPointer();
            //}
        }
        
        public void Copy(UnmanagedImage a)
        {
            Win32.CopyMemory(dataIntPtr, a.dataIntPtr, (UIntPtr)(width * height * bytesPerPixel));
        }

        public void Zero()
        {
            Win32.ZeroMemory(dataIntPtr, (UIntPtr)(width * height * bytesPerPixel));
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }

        public void SaveToFile(string filename)
        {
            FileStream fileStream = File.Create(filename);
            BinaryWriter binaryWriter = new BinaryWriter(fileStream);

            int nbytes = width * height * bytesPerPixel;
            byte[] buffer = new byte[nbytes];

            Marshal.Copy(dataIntPtr, buffer, 0, nbytes);

            binaryWriter.Write(buffer);

            binaryWriter.Close();
            fileStream.Close();
        }

        public void LoadFromFile(string filename)
        {
            FileStream fileStream = File.OpenRead(filename);
            BinaryReader binaryReader = new BinaryReader(fileStream);

            int nbytes = width * height * bytesPerPixel;
            byte[] buffer = new byte[nbytes];

            binaryReader.Read(buffer, 0, nbytes);

            Marshal.Copy(buffer, 0, dataIntPtr, nbytes);

            binaryReader.Close();
            fileStream.Close();
        }

    }



}
