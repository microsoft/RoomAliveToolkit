using System;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Threading;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RoomAliveToolkit;
using System.IO;
using SharpDX.WIC;

/*
Generate a client with 
"C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\x64\SvcUtil.exe" /noConfig /out:KinectClient.cs http://localhost:8733/Design_Time_Addresses/KinectServer2/Service1/ /reference:..\bin\Debug\Vision.dll /reference:..\bin\Debug\Kinect2.dll
*/

namespace RoomAliveToolkit
{
    /// <summary>
    /// A singleton which does heavy lifting on Kinect stream
    /// </summary>
    public class KinectHandler
    {
        public static KinectHandler instance;
        KinectSensor kinectSensor;

        DepthFrameReader depthFrameReader;
        public ushort[] depthShortBuffer = new ushort[Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight];
        public byte[] depthByteBuffer = new byte[Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 2];
        public List<AutoResetEvent> depthFrameReady = new List<AutoResetEvent>();

        ColorFrameReader colorFrameReader;
        public byte[] yuvByteBuffer = new byte[Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 2];
        public List<AutoResetEvent> yuvFrameReady = new List<AutoResetEvent>();
        public byte[] rgbByteBuffer = new byte[Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 4];
        public List<AutoResetEvent> rgbFrameReady = new List<AutoResetEvent>();
        public byte[] jpegByteBuffer = new byte[Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 4];
        public List<AutoResetEvent> jpegFrameReady = new List<AutoResetEvent>();
        public int nJpegBytes = 0;

        public float lastColorGain;
        public long lastColorExposureTimeTicks;

        BodyFrameReader bodyFrameReader;

        AudioBeamFrameReader audioBeamFrameReader;

        public Kinect2Calibration kinect2Calibration;
        public ManualResetEvent kinect2CalibrationReady = new ManualResetEvent(false);

        ImagingFactory imagingFactory = new ImagingFactory();
        Stopwatch stopWatch = new Stopwatch();

        public KinectHandler()
        {
            instance = this;
            kinectSensor = KinectSensor.GetDefault();
            kinectSensor.CoordinateMapper.CoordinateMappingChanged += CoordinateMapper_CoordinateMappingChanged;
            kinectSensor.Open();
        }

        void CoordinateMapper_CoordinateMappingChanged(object sender, CoordinateMappingChangedEventArgs e)
        {
            kinect2Calibration = new RoomAliveToolkit.Kinect2Calibration();
            kinect2Calibration.RecoverCalibrationFromSensor(kinectSensor);
            kinect2CalibrationReady.Set();

            depthFrameReader = kinectSensor.DepthFrameSource.OpenReader();
            depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;

            colorFrameReader = kinectSensor.ColorFrameSource.OpenReader();
            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;

            bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();
            bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

            audioBeamFrameReader = kinectSensor.AudioSource.OpenReader();
            audioBeamFrameReader.FrameArrived += audioBeamFrameReader_FrameArrived;

            audioBeamFrameReader.AudioSource.AudioBeams[0].AudioBeamMode = AudioBeamMode.Manual;
            audioBeamFrameReader.AudioSource.AudioBeams[0].BeamAngle = 0;
        }


        void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            var depthFrame = e.FrameReference.AcquireFrame();
            if (depthFrame != null)
            {
                using (depthFrame)
                {
                    if (depthFrameReady.Count > 0)
                    {
                        lock (depthShortBuffer)
                            depthFrame.CopyFrameDataToArray(depthShortBuffer);
                        lock (depthFrameReady)
                            foreach (var autoResetEvent in depthFrameReady)
                                autoResetEvent.Set();
                    }
                }
            }
        }

        void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            var colorFrame = e.FrameReference.AcquireFrame();
            if (colorFrame != null)
            {
                using (colorFrame)
                {
                    lastColorGain = colorFrame.ColorCameraSettings.Gain;
                    lastColorExposureTimeTicks = colorFrame.ColorCameraSettings.ExposureTime.Ticks;

                    if (yuvFrameReady.Count > 0)
                    {
                        lock (yuvByteBuffer)
                            colorFrame.CopyRawFrameDataToArray(yuvByteBuffer);
                        lock (yuvFrameReady)
                            foreach (var autoResetEvent in yuvFrameReady)
                                autoResetEvent.Set();
                    }

                    if ((rgbFrameReady.Count > 0) || (jpegFrameReady.Count > 0))
                    {
                        lock (rgbByteBuffer)
                            colorFrame.CopyConvertedFrameDataToArray(rgbByteBuffer, ColorImageFormat.Bgra);
                        lock (rgbFrameReady)
                            foreach (var autoResetEvent in rgbFrameReady)
                                autoResetEvent.Set();
                    }

                    if (jpegFrameReady.Count > 0)
                    {
                        // should be put in a separate thread?

                        stopWatch.Restart();

                        var bitmapSource = new Bitmap(imagingFactory, Kinect2Calibration.colorImageWidth, Kinect2Calibration.colorImageHeight, SharpDX.WIC.PixelFormat.Format32bppBGR, BitmapCreateCacheOption.CacheOnLoad);
                        var bitmapLock = bitmapSource.Lock(BitmapLockFlags.Write);
                        Marshal.Copy(rgbByteBuffer, 0, bitmapLock.Data.DataPointer, Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 4);
                        bitmapLock.Dispose();

                        var memoryStream = new MemoryStream();

                        //var fileStream = new FileStream("test" + frame++ + ".jpg", FileMode.Create);
                        //var stream = new WICStream(imagingFactory, "test" + frame++ + ".jpg", SharpDX.IO.NativeFileAccess.Write);

                        var stream = new WICStream(imagingFactory, memoryStream);

                        var jpegBitmapEncoder = new JpegBitmapEncoder(imagingFactory);
                        jpegBitmapEncoder.Initialize(stream);

                        var bitmapFrameEncode = new BitmapFrameEncode(jpegBitmapEncoder);
                        bitmapFrameEncode.Options.ImageQuality = 0.5f;
                        bitmapFrameEncode.Initialize();
                        bitmapFrameEncode.SetSize(Kinect2Calibration.colorImageWidth, Kinect2Calibration.colorImageHeight);
                        var pixelFormatGuid = PixelFormat.FormatDontCare;
                        bitmapFrameEncode.SetPixelFormat(ref pixelFormatGuid);
                        bitmapFrameEncode.WriteSource(bitmapSource);

                        bitmapFrameEncode.Commit();
                        jpegBitmapEncoder.Commit();

                        //fileStream.Close();
                        //fileStream.Dispose();

                        //Console.WriteLine(stopWatch.ElapsedMilliseconds + "ms " + memoryStream.Length + " bytes");

                        lock (jpegByteBuffer)
                        {
                            nJpegBytes = (int)memoryStream.Length;
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            memoryStream.Read(jpegByteBuffer, 0, nJpegBytes);
                        }
                        lock (jpegFrameReady)
                            foreach (var autoResetEvent in jpegFrameReady)
                                autoResetEvent.Set();

                        //var file = new FileStream("test" + frame++ + ".jpg", FileMode.Create);
                        //file.Write(jpegByteBuffer, 0, nJpegBytes);
                        //file.Close();

                        bitmapSource.Dispose();
                        memoryStream.Close();
                        memoryStream.Dispose();
                        stream.Dispose();
                        jpegBitmapEncoder.Dispose();
                        bitmapFrameEncode.Dispose();
                    }
                }
            }
        }

        private Body[] bodies = null;

        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            var bodyFrame = e.FrameReference.AcquireFrame();
            if (bodyFrame != null)
            {
                using (bodyFrame)
                {
                    if (bodies == null)
                        bodies = new Body[bodyFrame.BodyCount];
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    //if (bodyFrame.BodyCount > 0)
                    //{
                    //    var serializer = new XmlSerializer(typeof(Body));
                    //    var writer = new StringWriter();
                    //    serializer.Serialize(writer, bodies[0]);
                    //    writer.Close();

                    //    Console.WriteLine(writer);
                    //}
                }
            }
        }

        public List<AutoResetEvent> audioFrameReady = new List<AutoResetEvent>();
        public List<Queue<byte[]>> audioFrameQueues = new List<Queue<byte[]>>();

        void audioBeamFrameReader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            var audioBeamFrames = e.FrameReference.AcquireBeamFrames();
            if (audioBeamFrames != null)
            {
                var audioBeamFrame = audioBeamFrames[0];

                foreach(var subFrame in audioBeamFrame.SubFrames)
                {
                    var buffer = new byte[subFrame.FrameLengthInBytes];

                    subFrame.CopyFrameDataToArray(buffer);

                    lock (audioFrameQueues)
                    {
                        foreach (var queue in audioFrameQueues)
                        {
                            if (queue.Count > 10)
                                queue.Dequeue();
                            queue.Enqueue(buffer);
                        }
                    }

                    lock (audioFrameReady)
                        foreach (var autoResetEvent in audioFrameReady)
                            autoResetEvent.Set();

                    //Console.WriteLine("subframe " + audioSubFrames++ + "\t" + subFrame.FrameLengthInBytes + "\t" + audioBeamFrame.SubFrames.Count);
                    subFrame.Dispose();
                }

                audioBeamFrame.Dispose();
                audioBeamFrames.Dispose();

            }
        }

    }

    /// <summary>
    /// Created on each session.
    /// </summary>
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    [ServiceContract]
    public class KinectServer2
    {
        byte[] depthByteBuffer = new byte[Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 2];
        byte[] yuvByteBuffer = new byte[Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 2];
        byte[] rgbByteBuffer = new byte[Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 4];

        AutoResetEvent depthFrameReady = new AutoResetEvent(false);
        AutoResetEvent yuvFrameReady = new AutoResetEvent(false);
        AutoResetEvent rgbFrameReady = new AutoResetEvent(false);
        AutoResetEvent jpegFrameReady = new AutoResetEvent(false);
        AutoResetEvent audioFrameReady = new AutoResetEvent(false);

        Queue<byte[]> audioFrameQueue = new Queue<byte[]>();

        public KinectServer2()
        {
            lock (KinectHandler.instance.depthFrameReady) // overkill?
                KinectHandler.instance.depthFrameReady.Add(depthFrameReady);
            lock (KinectHandler.instance.yuvFrameReady)
                KinectHandler.instance.yuvFrameReady.Add(yuvFrameReady);
            lock (KinectHandler.instance.rgbFrameReady)
                KinectHandler.instance.rgbFrameReady.Add(rgbFrameReady);
            lock (KinectHandler.instance.jpegFrameReady)
                KinectHandler.instance.jpegFrameReady.Add(jpegFrameReady);
            lock (KinectHandler.instance.audioFrameReady)
                KinectHandler.instance.audioFrameReady.Add(audioFrameReady);
            lock (KinectHandler.instance.audioFrameQueues)
                KinectHandler.instance.audioFrameQueues.Add(audioFrameQueue);


            OperationContext.Current.Channel.Closed += ClientClosed;
        }

        public void ClientClosed(object sender, EventArgs e)
        {
            //Console.WriteLine("ClientClosed");

            // remove ourselves from the singleton
            lock (KinectHandler.instance.depthFrameReady) // overkill?
                KinectHandler.instance.depthFrameReady.Remove(depthFrameReady);
            lock (KinectHandler.instance.yuvFrameReady)
                KinectHandler.instance.yuvFrameReady.Remove(yuvFrameReady);
            lock (KinectHandler.instance.rgbFrameReady)
                KinectHandler.instance.rgbFrameReady.Remove(rgbFrameReady);
            lock (KinectHandler.instance.jpegFrameReady)
                KinectHandler.instance.jpegFrameReady.Remove(jpegFrameReady);
            lock (KinectHandler.instance.audioFrameReady)
                KinectHandler.instance.audioFrameReady.Remove(audioFrameReady);
            lock (KinectHandler.instance.audioFrameQueues)
                KinectHandler.instance.audioFrameQueues.Remove(audioFrameQueue);
        }

        // Returns immediately if a frame has been made available since the last time this was called on this client;
        // otherwise blocks until one is available.
        [OperationContract]
        public byte[] LatestDepthImage()
        {
            depthFrameReady.WaitOne();
            // Is this copy really necessary?:
            lock (KinectHandler.instance.depthShortBuffer)
                Buffer.BlockCopy(KinectHandler.instance.depthShortBuffer, 0, depthByteBuffer, 0, Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 2);
            return depthByteBuffer;
        }

        [OperationContract]
        public byte[] LatestYUVImage()
        {
            yuvFrameReady.WaitOne();
            lock (KinectHandler.instance.yuvByteBuffer)
                Buffer.BlockCopy(KinectHandler.instance.yuvByteBuffer, 0, yuvByteBuffer, 0, Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 2);
            return yuvByteBuffer;
        }

        [OperationContract]
        public byte[] LatestRGBImage()
        {
            rgbFrameReady.WaitOne();
            lock (KinectHandler.instance.rgbByteBuffer)
                Buffer.BlockCopy(KinectHandler.instance.rgbByteBuffer, 0, rgbByteBuffer, 0, Kinect2Calibration.colorImageWidth * Kinect2Calibration.colorImageHeight * 4);
            return rgbByteBuffer;
        }

        [OperationContract]
        public byte[] LatestJPEGImage()
        {
            jpegFrameReady.WaitOne();
            byte[] jpegByteBuffer;
            lock (KinectHandler.instance.jpegByteBuffer)
            {
                jpegByteBuffer = new byte[KinectHandler.instance.nJpegBytes];
                Buffer.BlockCopy(KinectHandler.instance.jpegByteBuffer, 0, jpegByteBuffer, 0, KinectHandler.instance.nJpegBytes);
            }
            return jpegByteBuffer;
        }
        
        [OperationContract]
        public byte[] LatestAudio()
        {
            audioFrameReady.WaitOne();
            lock (KinectHandler.instance.audioFrameQueues) // overkill?
            {
                var buffer = new byte[audioFrameQueue.Count * 1024];
                int count = audioFrameQueue.Count;
                for (int i = 0; i < count; i++)
                {
                    var thisBuffer = audioFrameQueue.Dequeue();
                    Array.Copy(thisBuffer, 0, buffer, 1024 * i, 1024);
                }
                return buffer;
            }
        }

        [OperationContract]
        public float LastColorGain()
        {
            return KinectHandler.instance.lastColorGain;
        }

        [OperationContract]
        public long LastColorExposureTimeTicks()
        {
            return KinectHandler.instance.lastColorExposureTimeTicks;
        }
        [OperationContract]
        public Kinect2Calibration GetCalibration()
        {
            KinectHandler.instance.kinect2CalibrationReady.WaitOne();
            return KinectHandler.instance.kinect2Calibration;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            new KinectHandler();
            var serviceHost = new ServiceHost(typeof(KinectServer2));

            // discovery
            serviceHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
            serviceHost.AddServiceEndpoint(new UdpDiscoveryEndpoint());

            serviceHost.Open();
            Console.ReadLine();
        }
    }
}
