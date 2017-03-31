using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.ServiceModel;
using System.Xml.Serialization;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Multimedia;
using SharpDX.DirectSound;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using SharpDX.DXGI;
using SharpDX.DirectWrite;
using RoomAliveToolkit;


namespace KinectV2Server
{
    public partial class MainForm : Form
    {

        #region Variables
        public static MainForm instance;
        public bool ShowTimingInformation = false;
        public const string version_number = "Release 1.0.0";

        enum DisplayType { Depth, Color, InfraRed, DepthForeground, DepthBackground, BodyIndex }
        DisplayType display = DisplayType.Depth;

        public enum MessageTypes { DEPTH, IR, BODYINDEX, DEPTH_FOREGROUND, COLOR_NONE, COLOR_JPEG, COLOR_H264, COLOR_RAWYUY, AUDIO} 

        private KinectServerSettings settings;
        private string settingsFileName = "KinectServerSettings.xml";

        private TCPNetworkStreamer depthServer;
        private TCPNetworkStreamer colorServer;
        private TCPNetworkStreamer audioServer;
        private TCPNetworkStreamer skeletonServer;
        private TCPNetworkStreamer infraredServer;
        private TCPNetworkStreamer configurationServer;     
   
        Thread processColorImageThread = null;

        AutoResetEvent nextColorFrameReadyForProcess = new AutoResetEvent(false);

        // Display variables
        private SharpDX.Direct2D1.Factory D2DFactory;
        private SharpDX.DirectWrite.TextFormat textFormat;
        private RenderTarget depthRenderTarget;
        private SharpDX.Direct2D1.Bitmap colorBitmap, depthBitmap;

        // easy access variables
        public const int depthImageWidth = 512;
        public const int depthImageHeight = 424;
        public const int colorImageWidth = 1920;
        public const int colorImageHeight = 1080;
        public const float depthToColorWidthRatio = depthImageWidth / colorImageWidth;
        public float lastColorGain;
        public long lastColorExposureTimeTicks;

        private ShortImage mDepthImage, mInfraredImage, mDepthForegroundImage, mDepthPlayerIndexImage, mDepthBackgroundImage, mSmoothedDepthForegroundImage;
        private ShortImage mDepthImageM, mInfraredImageM;
        private ByteImage mBodyIndexImage, mBodyIndexImageM;
        private ARGBImage mDepthDisplayImage, mColorImageM, mColorImage;
        private ShortImage mColorImageYUY, mColorImageYUYM; // YUY is 16 bits/pixel

        private static System.Object mDepthFrameDataLock = new System.Object();
        private ushort[] mDepthFrameData;
        private DepthSpacePoint[] mDepthSpacePoints;
        public float[] mDepthSpacePointsInFloats;
        private int bodyCount = 0;
        private Body[] mBodies = null;

        public Float2Image depthFrameToCameraSpaceTable, depthFrameToCameraSpaceTableFlipped;

        bool mRunningKinect = false;

        string backgroundFileName = "Background";
        int acquireBackgroundCounter = 0;
        PreProcessDepthMap_Variance preProcess;

        //Kinect stuff
        public KinectSensor mKinectSensor = null;
        private Stopwatch kinectTimer = new Stopwatch();
        private CoordinateMapper coordinateMapper = null;
        private MultiSourceFrameReader reader = null;
        private ColorFrameReader readerColor = null;
        //private AudioBeamFrameReader audioBeamFrameReader = null;

        public Microsoft.Kinect.Vector4 lastKinectAccReading = new Microsoft.Kinect.Vector4();
        private byte[] nextFrameDepth = new byte[1];
        public byte[] currentFrameDepth = new byte[1];
        private byte[] nextFrameIr = new byte[1];
        public byte[] currentFrameIr = new byte[1];
        private byte[] nextFrameColor = new byte[1];
        public byte[] currentFrameColor = new byte[1];
        private byte[] nextFrameColorRaw = new byte[1];
        public byte[] currentFrameColorRaw = new byte[1];
        private byte[] nextFrameSkeleton = new byte[1];
        public byte[] currentFrameSkeleton = new byte[1];
        private byte[] nextFrameAudio = new byte[1]; 
        public byte[] currentFrameAudio = new byte[1];


        AutoResetEvent nextFrameReady = new AutoResetEvent(false);
        AutoResetEvent nextColorFrameReady = new AutoResetEvent(false);
        AutoResetEvent nextAudioFrameReady = new AutoResetEvent(false);
        AutoResetEvent processColorThreadCompleted = new AutoResetEvent(false);
        bool applicationRunning = true;

        FrameRate fpsRendering = new FrameRate(1);
        FrameRate fpsServerDepth = new FrameRate(1);
        FrameRate fpsServerColor = new FrameRate(1);
        FrameRate fpsServerAudio = new FrameRate(1);
        FrameRate fpsKinectDepth = new FrameRate(1);
        FrameRate fpsKinectColor = new FrameRate(1);
        FrameRate fpsServerSkeleton = new FrameRate(1);


        //Skeleton / Body Handling
        Dictionary<ulong, BodyContainer> skeletons = new Dictionary<ulong, BodyContainer>(6);
        public class BodyContainer
        {
            public bool updated = false;
            public ulong ID;
            public CameraSpacePoint[] jointPositions = new CameraSpacePoint[25];
            public byte[] jointStates = new byte[25];
            public byte HandLeftConfidence, HandRightConfidence, HandLeftState, HandRightState;

            // convert the joint points to depth (display) space - for rendering only
            public Dictionary<JointType, SharpDX.Point> jointPoints = new Dictionary<JointType, SharpDX.Point>();

            //face information
            public CameraSpacePoint[] facePointsWorld = new CameraSpacePoint[5]; //world coordinates
            //for rendering
            public System.Drawing.PointF[] facePointsInInfraredSpace = new System.Drawing.PointF[5];
            public System.Drawing.PointF[] facePointsInColorSpace = new System.Drawing.PointF[5];
            public SharpDX.Mathematics.Interop.RawRectangleF faceRectInInfraredSpace = new SharpDX.Mathematics.Interop.RawRectangleF();
            public SharpDX.Mathematics.Interop.RawRectangleF faceRectInColorSpace = new SharpDX.Mathematics.Interop.RawRectangleF();
            public Microsoft.Kinect.Vector4 faceRotationQ;
            public Vector3 faceRotationYPR;

            public byte leftEyeClosed, rightEyeClosed, mouthOpen, mouthMoved, lookingAway, happy, engaged, glasses;
        }

        // Audio capture
        public DirectSound mDirectSound = null;
        private DirectSoundCapture audioCapture = null;
        private CaptureBuffer audioCaptureBuffer = null;
        NotificationPosition[] audioCaptureNotificationPositions = null;

        //face tracking
        private FaceFrameSource[] faceFrameSources = null;/// Face frame sources
        private FaceFrameReader[] faceFrameReaders = null; /// Face frame readers
        private FaceFrameResult[] faceFrameResults = null;/// Storage for face frame results
        private const double FaceRotationIncrementInDegrees = 5.0;

        WaitHandle[] framesReady = null;
        DateTime startTime;

        public Kinect2Calibration kinect2Calibration;

#endregion

        #region Initialization
        static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public MainForm(string[] args)
        {
            instance = this;

            settings = KinectServerSettings.Load(settingsFileName);

            InitializeComponent();


        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text = "KinectV2Server " + version_number;
            InitBuffers();
            startTime = DateTime.Now;

            //Start Kinect on new thread
            Thread t0 = new Thread(InitKinect);
            t0.Priority = ThreadPriority.AboveNormal;
            t0.Start();
            Thread.Sleep(0);

            // Initialize audio capture
            InitAudioCapture();
            checkBoxStreamAudio.Checked = settings.StreamAudio;

            // Create DX Textures
            D2DFactory = new SharpDX.Direct2D1.Factory();

            HwndRenderTargetProperties hwndProperties = new HwndRenderTargetProperties();
            hwndProperties.Hwnd = panelDisplay.Handle;
            hwndProperties.PixelSize = new SharpDX.Size2(panelDisplay.Width, panelDisplay.Height);
            hwndProperties.PresentOptions = PresentOptions.Immediately;

            RenderTargetProperties rndTargProperties = new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Ignore));

            depthRenderTarget = new WindowRenderTarget(D2DFactory, rndTargProperties, hwndProperties);

            depthBitmap = new SharpDX.Direct2D1.Bitmap(depthRenderTarget, new Size2(depthImageWidth, depthImageHeight), new BitmapProperties(depthRenderTarget.PixelFormat, 96f, 96f));
            colorBitmap = new SharpDX.Direct2D1.Bitmap(depthRenderTarget, new Size2(colorImageWidth, colorImageHeight), new BitmapProperties(new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Ignore), 96, 96));

            textFormat = new TextFormat(new SharpDX.DirectWrite.Factory(), "Arial", SharpDX.DirectWrite.FontWeight.Bold, SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, 12f); //used to have "en-us"

            // Create an array of wait handles for frame events
            framesReady = new WaitHandle[3];
            framesReady[0] = nextFrameReady;
            framesReady[1] = nextColorFrameReady;
            framesReady[2] = audioCaptureNotificationPositions[0].WaitHandle;


            //starting server
            depthServer = new TCPNetworkStreamer(true, settings.depthPort, "Depth");
            colorServer = new TCPNetworkStreamer(true, settings.colorPort, "Color");
            audioServer = new TCPNetworkStreamer(true, settings.audioPort, "Audio");
            skeletonServer = new TCPNetworkStreamer(true, settings.skeletonPort, "Skeleton");
            infraredServer = new TCPNetworkStreamer(true, settings.infraredPort, "Infrared");
            configurationServer = new TCPNetworkStreamer(true, settings.configurationPort, "Configuration");
            configurationServer.ReceivedMessage += new TCPNetworkStreamer.ReceivedMessageEventHandler(ReceiveConfigurationRequest);

            Thread t1 = new Thread(ProcessingThread);
            t1.Start();

            Thread.Sleep(0);

            checkBoxStreamColor.Checked = settings.StreamColor;
            checkBoxSkeleton.Checked = settings.RenderSkeleton;
            checkBoxFlip.Checked = settings.FlipImages;

            trackBarThreshold.Value = settings.ThresholdNoise;

            comboBoxColorCompression.SelectedText = settings.colorCompression == KinectServerSettings.ColorCompressionType.JPEG ? "JPEG" : "NONE";
            comboBoxStreamType.SelectedText = settings.streamType == KinectServerSettings.StreamType.All ? "Full Depth" : (settings.streamType == KinectServerSettings.StreamType.Foreground ? "Foreground Depth" : "Body Index Depth");

            Console.WriteLine("KinectV2Server initialized!");
        }

        private unsafe void InitBuffers()
        {
            // Create Images
            mDepthDisplayImage = new ARGBImage(depthImageWidth, depthImageHeight);
            mInfraredImage = new ShortImage(depthImageWidth, depthImageHeight);
            mDepthImage = new ShortImage(depthImageWidth, depthImageHeight);
            mBodyIndexImage = new ByteImage(depthImageWidth, depthImageHeight);
            mInfraredImageM = new ShortImage(depthImageWidth, depthImageHeight);
            mDepthImageM = new ShortImage(depthImageWidth, depthImageHeight);
            mBodyIndexImageM = new ByteImage(depthImageWidth, depthImageHeight);
            mColorImage = new ARGBImage(colorImageWidth, colorImageHeight);
            mColorImageM = new ARGBImage(colorImageWidth, colorImageHeight);
            mColorImageYUY = new ShortImage(colorImageWidth, colorImageHeight);
            mColorImageYUYM = new ShortImage(colorImageWidth, colorImageHeight);
            mDepthForegroundImage = new ShortImage(depthImageWidth, depthImageHeight);
            mSmoothedDepthForegroundImage = new ShortImage(depthImageWidth, depthImageHeight);
            mDepthBackgroundImage = new ShortImage(depthImageWidth, depthImageHeight);
            mDepthPlayerIndexImage = new ShortImage(depthImageWidth, depthImageHeight);

            mDepthFrameData = new ushort[depthImageWidth * depthImageHeight];
            mDepthSpacePoints = new DepthSpacePoint[colorImageWidth * colorImageHeight];
            mDepthSpacePointsInFloats = new float[colorImageWidth * colorImageHeight * 2];


            if (File.Exists(backgroundFileName + ".bin") && File.Exists(backgroundFileName + ".jpg"))
            {
                Console.WriteLine("Loading background image from file " + backgroundFileName + ".bin");
                mDepthBackgroundImage.LoadFromFile(backgroundFileName + ".bin");
            }
            else
            {
                Console.WriteLine("Starting background acquisition...");

                acquireBackgroundCounter = 100;
            }

            preProcess = new PreProcessDepthMap_Variance(depthImageWidth, depthImageHeight);
        }

        private void InitKinect()
        {
            Console.WriteLine("Initializing Kinect.");
            this.mKinectSensor = KinectSensor.GetDefault();

            if (this.mKinectSensor != null)
            {

                FrameDescription depthFrameDescription = this.mKinectSensor.DepthFrameSource.FrameDescription;
                bodyCount = this.mKinectSensor.BodyFrameSource.BodyCount;
                mBodies = new Body[bodyCount];

                this.mKinectSensor.CoordinateMapper.CoordinateMappingChanged += CoordinateMapper_CoordinateMappingChanged;



                // open the sensor
                this.mKinectSensor.Open();
            }
        }

        unsafe void CoordinateMapper_CoordinateMappingChanged(object sender, CoordinateMappingChangedEventArgs e)
        {
            if (!mRunningKinect)
            {
                mRunningKinect = true;

                // get the coordinate mapper
                this.coordinateMapper = this.mKinectSensor.CoordinateMapper;

                Console.WriteLine("Saving Kinect2Calibration to file: kinect2calibration.xml");
                kinect2Calibration = new Kinect2Calibration();
                kinect2Calibration.silent = true;
                kinect2Calibration.RecoverCalibrationFromSensor(this.mKinectSensor);

                Console.WriteLine("Saving DepthFrameToCameraSpaceTable to files: mDepthToCameraSpace.bin and mDepthToCameraSpaceFlipped.bin");
                Microsoft.Kinect.PointF[] depthToCameraTable = new Microsoft.Kinect.PointF[depthImageWidth * depthImageHeight];
                depthToCameraTable = mKinectSensor.CoordinateMapper.GetDepthFrameToCameraSpaceTable(); 
                
                var buffer = new float[depthImageWidth * depthImageHeight * 2];
                var fileStream = File.Create("mDepthToCameraSpace.bin");
                var binaryWriter = new BinaryWriter(fileStream);

                var bufferFlipped = new float[depthImageWidth * depthImageHeight * 2];
                var fileStreamFlipped = File.Create("mDepthToCameraSpaceFlipped.bin");
                var binaryWriterFlipped = new BinaryWriter(fileStreamFlipped);

                int j = 0;
                for (int r = 0; r < 424; r++)
                    for (int c = 0; c < 512; c++)
                    {
                        var ray = depthToCameraTable[r * 512 + c];
                        buffer[j] = ray.X;
                        buffer[j+1] = ray.Y;
                        binaryWriter.Write(ray.X);
                        binaryWriter.Write(ray.Y);

                        var rayFlipped = depthToCameraTable[r * 512 + (512 - 1 - c)]; //depth to camera table needs to be flipped in X since all the images are also flipped in X 
                        bufferFlipped[j] = rayFlipped.X;
                        bufferFlipped[j+1] = rayFlipped.Y;
                        binaryWriterFlipped.Write(rayFlipped.X);
                        binaryWriterFlipped.Write(rayFlipped.Y);

                        j += 2;
                    }
                binaryWriter.Close();
                fileStream.Close();

                binaryWriterFlipped.Close();
                fileStreamFlipped.Close();

                depthFrameToCameraSpaceTable = new Float2Image(depthImageWidth, depthImageHeight);
                depthFrameToCameraSpaceTable.FromArray(buffer);

                depthFrameToCameraSpaceTableFlipped = new Float2Image(depthImageWidth, depthImageHeight);
                depthFrameToCameraSpaceTableFlipped.FromArray(bufferFlipped);

                // For Face tracking
                // specify the required face frame results
                FaceFrameFeatures faceFrameFeatures =
                    FaceFrameFeatures.BoundingBoxInInfraredSpace
                    | FaceFrameFeatures.PointsInInfraredSpace
                    | FaceFrameFeatures.BoundingBoxInColorSpace
                    | FaceFrameFeatures.PointsInColorSpace
                    | FaceFrameFeatures.RotationOrientation
                    | FaceFrameFeatures.FaceEngagement
                    | FaceFrameFeatures.Glasses
                    | FaceFrameFeatures.Happy
                    | FaceFrameFeatures.LeftEyeClosed
                    | FaceFrameFeatures.RightEyeClosed
                    | FaceFrameFeatures.LookingAway
                    | FaceFrameFeatures.MouthMoved
                    | FaceFrameFeatures.MouthOpen
                   ;

                // create a face frame source + reader to track each face in the FOV
                this.faceFrameSources = new FaceFrameSource[bodyCount];
                this.faceFrameReaders = new FaceFrameReader[bodyCount];
                for (int i = 0; i < bodyCount; i++)
                {
                    // create the face frame source with the required face frame features and an initial tracking Id of 0
                    this.faceFrameSources[i] = new FaceFrameSource(this.mKinectSensor, 0, faceFrameFeatures);

                    // open the corresponding reader
                    this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
                }

                // allocate storage to store face frame results for each face in the FOV
                this.faceFrameResults = new FaceFrameResult[bodyCount];

                //FaceTracking
                for (int i = 0; i < this.bodyCount; i++)
                {
                    if (this.faceFrameReaders[i] != null)
                    {
                        // wire handler for face frame arrival
                        this.faceFrameReaders[i].FrameArrived += this.Reader_FaceFrameArrived;
                    }
                }

                this.readerColor = this.mKinectSensor.ColorFrameSource.OpenReader(); //do this separately to make sure you don't slow down the depth stream

                this.reader = this.mKinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Body | FrameSourceTypes.Infrared | FrameSourceTypes.BodyIndex);

                kinectTimer.Start();

                this.reader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;
                this.readerColor.FrameArrived += this.Reader_ColorSourceFrameArrived;
                
               

                //this.audioBeamFrameReader = this.mKinectSensor.AudioSource.OpenReader();
                //this.audioBeamFrameReader.FrameArrived += audioBeamFrameReader_FrameArrived;
            }
        }

        Dictionary<int, long> clientsToClose = new Dictionary<int, long>();
        public void ReceiveConfigurationRequest(object sender, ReceivedMessageEventArgs e)
        {
            byte[] request = e.data;
            byte requestType = e.data[0];
            int id = ((ClientState)sender).ID;

            switch(requestType)
            { 
                case 1:    //send the depthFrameToCameraSpaceTable 

                    var res1 = this.checkBoxFlip.Checked ? depthFrameToCameraSpaceTableFlipped.ToByteArray() : depthFrameToCameraSpaceTable.ToByteArray();
                    
                    Thread.Sleep(1000); //hack, otherwise Unity crashes

                    byte[] message1 = new byte[res1.Length + 1];//res2.Length + 1];
                    message1[0] = (byte)1; //message type
                    Array.Copy(res1, 0, message1, 1, res1.Length);
                    //Array.Copy(res2, 0, message1, res1.Length + 1, res2.Length);

                    Console.WriteLine("Configuration Server received a request from " + id + " request:" + request[0] + " responseLen:" + message1.Length);
                    configurationServer.SendMessageToClient(message1, id);
                    break;
                case 2: //send the serialized Kinect2Calibration

                    XmlSerializer serializer = new XmlSerializer(typeof(Kinect2Calibration));
                    MemoryStream ms = new MemoryStream();
                    var writer = new StreamWriter(ms);
                    serializer.Serialize(writer, kinect2Calibration);
                    byte[] res2 = ms.ToArray();
                    writer.Close();

                    Thread.Sleep(1000);//hack, otherwise Unity crashes

                    byte[] message2 = new byte[res2.Length + 1];
                    message2[0] = (byte)2; //message type
                    Array.Copy(res2, 0, message2, 1, res2.Length);
                    Console.WriteLine("Configuration Server received a request from " + id + " request:" + request[0] + " responseLen:" + message2.Length);
                    configurationServer.SendMessageToClient(message2, id);
                    break;
                case 0:
                    Console.WriteLine("Configuration Server received a request to terminate client " + id + " connection.");
                    //add the client to the soft close list
                    //need to explicitly close the client since no other messages will get otherwise subsequent connections will get messed up. 
                    clientsToClose.Add(id, DateTime.Now.Ticks+10000000); //wait 1 second to close this client
                    break;
            }
        }

        private void InitAudioCapture()
        {
            // Create audio format
            WaveFormat format = new WaveFormat(44100,16,1);

            // Create audio capture buffer
            CaptureBufferDescription audioCaptureBufferDesc = new CaptureBufferDescription()
            {
                Format = format,
                BufferBytes = format.AverageBytesPerSecond,
                Flags = CaptureBufferCapabilitiesFlags.WaveMapped
            };

            audioCapture = new DirectSoundCapture();
            audioCaptureBuffer = new CaptureBuffer(audioCapture, audioCaptureBufferDesc);

            // Subscribe to audio capture notifications
            audioCaptureNotificationPositions = new NotificationPosition[1];
            audioCaptureNotificationPositions[0] = new NotificationPosition()
            {
                Offset = audioCaptureBuffer.Capabilities.BufferBytes - 1, //SizeInBytes
                WaitHandle = new AutoResetEvent(false)
            };

            audioCaptureBuffer.SetNotificationPositions(audioCaptureNotificationPositions);
            audioCaptureBuffer.Start(true);
        }

        private void Stop()
        {
            applicationRunning = false;
            nextFrameReady.Set();
            if (reader != null)
            {
                this.reader.MultiSourceFrameArrived -= this.Reader_MultiSourceFrameArrived;
                reader.Dispose();
                reader = null;
            }
            if (readerColor != null)
            {
                this.readerColor.FrameArrived -= this.Reader_ColorSourceFrameArrived;
                readerColor.Dispose();
                readerColor = null;
            }

            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameReaders[i] != null)
                {
                    // FaceFrameReader is IDisposable
                    this.faceFrameReaders[i].Dispose();
                    this.faceFrameReaders[i] = null;
                }

                if (this.faceFrameSources[i] != null)
                {
                    // FaceFrameSource is IDisposable
                    this.faceFrameSources[i].Dispose();
                    this.faceFrameSources[i] = null;
                }
            }

            if (this.mKinectSensor != null)
            {
                this.mKinectSensor.Close();
                this.mKinectSensor = null;
            }

            if (depthServer != null)
            {
                depthServer.Close();
                depthServer = null;
            }
            if (colorServer != null)
            {
                colorServer.Close();
                colorServer = null;
            }
            if (skeletonServer != null)
            {
                skeletonServer.Close();
                skeletonServer = null;
            }
            if (audioServer != null)
            {
                audioServer.Close();
                audioServer = null;
            }
            if (infraredServer != null)
            {
                infraredServer.Close();
                infraredServer = null;
            }
            if (configurationServer != null)
            {
                configurationServer.Close();
                configurationServer = null;
            }        
        }
#endregion

#region Processing Thread

        System.Diagnostics.Stopwatch stopwatch3 = new System.Diagnostics.Stopwatch();

        // Send messages to clients
        private void ProcessingThread()
        {
            while (applicationRunning)
            {
                stopwatch3.Reset();
                stopwatch3.Start();


                if(clientsToClose.Count>0) //house cleaning
                {
                    long now = DateTime.Now.Ticks;
                    int[] list = new int[clientsToClose.Count];
                    clientsToClose.Keys.CopyTo(list,0);
                    foreach (int id in list)
                    {
                        if(now > clientsToClose[id] )
                        {
                            configurationServer.CloseClient(id);
                            clientsToClose.Remove(id);
                        }
                    }
                }

                if (killAllClients)
                {
                    if (depthServer != null) depthServer.CloseAllClients();
                    if (colorServer != null) colorServer.CloseAllClients();
                    if (audioServer != null) audioServer.CloseAllClients();
                    if (skeletonServer != null) skeletonServer.CloseAllClients();
                    if (configurationServer != null) configurationServer.CloseAllClients();
                    killAllClients = false;
                }

                int signalIndex = WaitHandle.WaitAny(framesReady);

                if (signalIndex == 0) // depth, flow  and skeleton
                {          
                    //don't do the work if there are no clients connected...
                    if (depthServer.GetClientCount() > 0)
                    { 
                        depthServer.SendMessageToAllClients(currentFrameDepth); 
                        fpsServerDepth.Tick(); 
                    }
                    if (skeletonServer.GetClientCount() > 0)
                    {
                        skeletonServer.SendMessageToAllClients(currentFrameSkeleton);
                        fpsServerSkeleton.Tick();
                    }

                    if (this.WindowState != FormWindowState.Minimized) Render();
                    //if (stopwatch3.ElapsedMilliseconds > 60) Console.WriteLine("Processing thread glitch: " + stopwatch3.ElapsedMilliseconds + " ms");
                }
                else if (settings.StreamColor && signalIndex == 1) // color
                {                 
                    if (colorServer.GetClientCount() > 0 && currentFrameColor.Length > 1) //don't do the work if there are no clients connected... have nothing to send
                    {
                        colorServer.SendMessageToAllClients(currentFrameColor);
                      
                        fpsServerColor.Tick();
                    }
                }
                else if (settings.StreamAudio && signalIndex == 2) // audio
                {
                    // Get audio frame from the capture buffer and send it to clients

                    long timeStampAudio = kinectTimer.ElapsedMilliseconds;
                    //AssembleAudioPacket(timeStampAudio);
                    AssembleAudioPacketDirectCapture(timeStampAudio, audioCaptureBuffer);
                    lock (nextFrameAudio)
                    {
                        Swap<byte[]>(ref nextFrameAudio, ref currentFrameAudio);
                    }

                    if (audioServer.GetClientCount() > 0)
                    {
                        fpsServerAudio.Tick();
                        audioServer.SendMessageToAllClients(currentFrameAudio);
                    }

                }
                Thread.Sleep(0); //makes sure that UI thread is not starved
            }
        }
#endregion

#region Rendering

        const int verticalOffsetColor = 68; 
        void Render()
        {

            fpsRendering.Tick();
            this.BeginInvoke(new InvokeDelegate(UpdateLabels));
            switch (display)
            {

                case DisplayType.Depth:
                    lock (mDepthImage)
                    {
                        mDepthDisplayImage.CopyShortImageForGrayscaleDisplay(mDepthImage, 8000);
                    }
                    break;
                case DisplayType.Color:
                    // do nothing here
                    break;
                case DisplayType.InfraRed:
                    lock (mInfraredImage)
                    {
                        mDepthDisplayImage.CopyShortImageForGrayscaleDisplay(mInfraredImage, ushort.MaxValue);
                    }
                    break;
                case DisplayType.DepthForeground:
                    lock (mDepthForegroundImage)
                    {
                        if(settings.BlurDepthImages) mDepthDisplayImage.CopyShortImageForGrayscaleDisplay(mSmoothedDepthForegroundImage, 8000);
                        else mDepthDisplayImage.CopyShortImageForGrayscaleDisplay(mDepthForegroundImage, 8000);
                    }
                    break;
                case DisplayType.DepthBackground:
                    lock (mDepthBackgroundImage)
                    {
                        mDepthDisplayImage.CopyShortImageForGrayscaleDisplay(mDepthBackgroundImage, 8000);
                    }
                    break;
                case DisplayType.BodyIndex:
                    lock (mDepthForegroundImage)
                    {
                        mDepthDisplayImage.Copy(mBodyIndexImage);
                        
                    }
                    break;
            }
            depthRenderTarget.BeginDraw();
            depthRenderTarget.Clear(SharpDX.Color.Gray);
            if (display != DisplayType.Color)
            {
                depthBitmap.CopyFromMemory(mDepthDisplayImage.DataIntPtr, depthImageWidth * 4);
                depthRenderTarget.DrawBitmap(depthBitmap, 1f, BitmapInterpolationMode.Linear);
            }
            else
            {
                colorBitmap.CopyFromMemory(mColorImage.DataIntPtr, colorImageWidth * 4);
                depthRenderTarget.DrawBitmap(colorBitmap,
                    new SharpDX.Mathematics.Interop.RawRectangleF(0f, verticalOffsetColor, depthImageWidth, verticalOffsetColor + (float)(depthImageWidth * colorImageHeight / colorImageWidth)), //destination
                    1.0f,
                    BitmapInterpolationMode.Linear,
                    new SharpDX.Mathematics.Interop.RawRectangleF(0f, 0f, colorImageWidth, colorImageHeight) //source
                );
            }

            SharpDX.Direct2D1.Brush brush = new SolidColorBrush(depthRenderTarget, SharpDX.Color.Red);
            SharpDX.Direct2D1.Brush brushG = new SolidColorBrush(depthRenderTarget, SharpDX.Color.Green);
            SharpDX.Direct2D1.Brush brushY = new SolidColorBrush(depthRenderTarget, SharpDX.Color.Yellow);

            if (settings.RenderSkeleton && display != DisplayType.Color)
            {
                foreach (BodyContainer skeleton in skeletons.Values)
                {
                    if (skeleton.jointPoints.Count == 25)
                    {
                        float x = settings.FlipImages ? depthImageWidth - skeleton.jointPoints[JointType.Head].X : skeleton.jointPoints[JointType.Head].X;
                        depthRenderTarget.DrawText(skeleton.ID.ToString(), textFormat,
                            new SharpDX.Mathematics.Interop.RawRectangleF(x - 3f, skeleton.jointPoints[JointType.Head].Y - 15f, x + 150f, skeleton.jointPoints[JointType.Head].Y + 50f), brushG);
                        //torso
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.Head], skeleton.jointPoints[JointType.Neck]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.Neck], skeleton.jointPoints[JointType.SpineShoulder]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.SpineShoulder], skeleton.jointPoints[JointType.SpineMid]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.SpineMid], skeleton.jointPoints[JointType.SpineBase]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.SpineShoulder], skeleton.jointPoints[JointType.ShoulderRight]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.SpineShoulder], skeleton.jointPoints[JointType.ShoulderLeft]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.SpineBase], skeleton.jointPoints[JointType.HipRight]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.SpineBase], skeleton.jointPoints[JointType.HipLeft]);

                        //rigth arm
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.ShoulderRight], skeleton.jointPoints[JointType.ElbowRight]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.ElbowRight], skeleton.jointPoints[JointType.WristRight]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.WristRight], skeleton.jointPoints[JointType.HandRight]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.HandRight], skeleton.jointPoints[JointType.HandTipRight]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.WristRight], skeleton.jointPoints[JointType.ThumbRight]);

                        //left arm
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.ShoulderLeft], skeleton.jointPoints[JointType.ElbowLeft]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.ElbowLeft], skeleton.jointPoints[JointType.WristLeft]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.WristLeft], skeleton.jointPoints[JointType.HandLeft]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.HandLeft], skeleton.jointPoints[JointType.HandTipLeft]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.WristLeft], skeleton.jointPoints[JointType.ThumbLeft]);

                        //right leg
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.HipRight], skeleton.jointPoints[JointType.KneeRight]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.KneeRight], skeleton.jointPoints[JointType.AnkleRight]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.AnkleRight], skeleton.jointPoints[JointType.FootRight]);

                        //left leg
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.HipLeft], skeleton.jointPoints[JointType.KneeLeft]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.KneeLeft], skeleton.jointPoints[JointType.AnkleLeft]);
                        DrawBone(brush, brushG, skeleton.jointPoints[JointType.AnkleLeft], skeleton.jointPoints[JointType.FootLeft]);
                    }
                }
            }

            if (settings.RenderFaces)
            {
                float scaleFactor = (float)depthImageWidth / (float)colorImageWidth;

                foreach(BodyContainer skeleton in skeletons.Values)
                {
                    SharpDX.Mathematics.Interop.RawRectangleF faceBox;
                    List<System.Drawing.PointF> facePoints = new List<System.Drawing.PointF>();
                    if (display != DisplayType.Color)
                    {
                        faceBox = skeleton.faceRectInInfraredSpace;
                        foreach (System.Drawing.PointF pointF in skeleton.facePointsInInfraredSpace)
                        {
                            facePoints.Add(pointF);
                        }
                    }
                    else
                    {
                        faceBox = skeleton.faceRectInColorSpace;
                        faceBox.Left = faceBox.Left * scaleFactor;
                        faceBox.Right = faceBox.Right * scaleFactor;
                        faceBox.Top = faceBox.Top * scaleFactor + verticalOffsetColor;
                        faceBox.Bottom = faceBox.Bottom * scaleFactor + verticalOffsetColor;

                        foreach (System.Drawing.PointF pointF in skeleton.facePointsInColorSpace)
                        {
                            System.Drawing.PointF newP = new System.Drawing.PointF();
                            newP.X = pointF.X * scaleFactor;
                            newP.Y = pointF.Y * scaleFactor + verticalOffsetColor;
                            facePoints.Add(newP);
                        }
                    }
                    //draw the faces
                    depthRenderTarget.DrawRectangle(faceBox, brushY);
                    depthRenderTarget.DrawText("Yaw:" + skeleton.faceRotationYPR.X.ToString(), textFormat, new SharpDX.Mathematics.Interop.RawRectangleF(faceBox.Left, faceBox.Top - 20f, faceBox.Left + 150f, faceBox.Top + 10f), brushY);
                    foreach (System.Drawing.PointF pointF in facePoints)
                    {
                        depthRenderTarget.DrawEllipse(new Ellipse(new SharpDX.Mathematics.Interop.RawVector2(pointF.X, pointF.Y), 1f, 1f), brushY);
                    }
                }
            }

            depthRenderTarget.EndDraw();
            brush.Dispose();
            brushG.Dispose();
        }

        public void DrawBone(SharpDX.Direct2D1.Brush rawBrush, SharpDX.Direct2D1.Brush predcitedBrush, SharpDX.Point pt1, SharpDX.Point pt2)
        {
            float x1 = settings.FlipImages ? depthImageWidth - pt1.X : pt1.X;
            float x2 = settings.FlipImages ? depthImageWidth - pt2.X : pt2.X;
            
            depthRenderTarget.DrawLine(new SharpDX.Mathematics.Interop.RawVector2(x1, pt1.Y), new SharpDX.Mathematics.Interop.RawVector2(x2, pt2.Y), rawBrush, 2f);

        }


        public delegate void InvokeDelegate();
        void UpdateLabels()
        {
            if (!applicationRunning) return;

            checkBoxStreamColor.Checked = settings.StreamColor;
            TimeSpan upTime = DateTime.Now - startTime;
            labelUpTime.Text = upTime.ToString();//"[-][d’.’]hh’:’mm’:’ss");
            if (acquireBackgroundCounter <= 0) acquireBackgroundToolStripMenuItem.Enabled = true;
            labelDepthFPS.Text = Math.Round(fpsKinectDepth.Framerate, 2).ToString() + " Hz";
            labelColorFPS.Text = Math.Round(fpsKinectColor.Framerate, 2).ToString() + " Hz";

            labelDepth.Text = depthServer.GetClientCount().ToString() + "  ("+ Math.Round(fpsServerDepth.Framerate, 2).ToString() + " Hz)";
            labelColor.Text = colorServer.GetClientCount().ToString() + "  (" + Math.Round(fpsServerColor.Framerate, 2).ToString() + " Hz)";
            labelSkeleton.Text = skeletonServer.GetClientCount().ToString() + "  (" + Math.Round(fpsServerSkeleton.Framerate, 2).ToString() + " Hz)";
            labelAudio.Text = audioServer.GetClientCount().ToString() + "  (" + Math.Round(fpsServerAudio.Framerate, 2).ToString() + " Hz)";
            labelConfig.Text = configurationServer.GetClientCount().ToString();

            labelBodies.Text = skeletons.Count.ToString();
            labelThreshold.Text = settings.ThresholdNoise.ToString();
        }
#endregion

#region Audio
        //private List<AutoResetEvent> audioFrameReady = new List<AutoResetEvent>();
        //public List<Queue<byte[]>> audioFrameQueues = new List<Queue<byte[]>>();
        Queue<byte[]> audioFrameQueue = new Queue<byte[]>();

        void audioBeamFrameReader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            var audioBeamFrames = e.FrameReference.AcquireBeamFrames();
            if (audioBeamFrames != null)
            {
                var audioBeamFrame = audioBeamFrames[0];

                foreach (var subFrame in audioBeamFrame.SubFrames)
                {
                    var buffer = new byte[subFrame.FrameLengthInBytes];

                    subFrame.CopyFrameDataToArray(buffer);

                    lock (audioFrameQueue)
                    {
                        audioFrameQueue.Enqueue(buffer);
                    }

                    nextAudioFrameReady.Set();

                    subFrame.Dispose();
                }

                audioBeamFrame.Dispose();
                audioBeamFrames.Dispose();

            }
        }

        public byte[] GetLatestAudio()
        {
            lock (audioFrameQueue) 
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

#endregion Audio 

#region Color Processing
        /// <summary>
        /// Handles the color frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private unsafe void Reader_ColorSourceFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            fpsKinectColor.Tick();
            ColorFrame colorFrame = e.FrameReference.AcquireFrame();
            try
            {
                if (colorFrame != null)
                {
                    lastColorGain = colorFrame.ColorCameraSettings.Gain;
                    lastColorExposureTimeTicks = colorFrame.ColorCameraSettings.ExposureTime.Ticks;
                    lock (mColorImage)
                    {

                        uint capacity = (uint)(colorImageWidth * colorImageHeight * 2);
                        colorFrame.CopyRawFrameDataToIntPtr(mColorImageYUYM.DataIntPtr, capacity);
                        if (settings.FlipImages)
                            mColorImageYUY.XMirror_YUYSpecial(mColorImageYUYM); //this is a special function which respects the YUYV ordering when mirroring
                        else
                            Swap<ShortImage>(ref mColorImageYUY, ref mColorImageYUYM);
                        //for display only
                        colorFrame.CopyConvertedFrameDataToIntPtr(mColorImageM.DataIntPtr, (uint)(colorImageWidth * colorImageHeight * 4), ColorImageFormat.Bgra);

                        if (settings.FlipImages)
                            mColorImage.XMirror(mColorImageM);
                        else
                            Swap<ARGBImage>(ref mColorImage, ref mColorImageM);
                    }
                    if(killProcessColorThread)
                    {
                        if (processColorImageThread != null)
                        {
                            processColorImageThread.Abort();
                            processColorImageThread = null;
                        }
                        killProcessColorThread = false;
                    }
                    if (settings.StreamColor && processColorImageThread == null)
                    {
                        // Start color frame processing thread
                        processColorImageThread = new Thread(ProcessColorImage);
                        processColorImageThread.Start();
                    }

                    // Notify color frame processing thread that a new color frame should be created
                    nextColorFrameReadyForProcess.Set();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught: " + ex.ToString());
                // ignore if the frame is no longer available
            }
            finally
            {
                if (colorFrame != null) colorFrame.Dispose();
            }
        }

        bool killProcessColorThread = false;
        private void ProcessColorImage()
        {
            while (settings.StreamColor) 
            {
                bool ok = nextColorFrameReadyForProcess.WaitOne(10000);
                if (!ok) break;
                long ticks = kinectTimer.ElapsedMilliseconds;

                lock (nextFrameColor)
                { 
                    switch(settings.colorCompression)
                    {
                        case KinectServerSettings.ColorCompressionType.NONE:
                            nextFrameColor = AssembleGenericImagePacket(ticks, mColorImage.DataIntPtr, colorImageWidth * colorImageHeight * 4, (int)MessageTypes.COLOR_NONE);
                            break;
                        case KinectServerSettings.ColorCompressionType.JPEG:
                            nextFrameColor = AssembleColorPacketJPEG(ticks, mColorImage.DataIntPtr, System.Windows.Media.PixelFormats.Bgra32, colorImageWidth, colorImageHeight);
                            break;
                    }
                    
                    //colorFrameQueue.Enqueue(nextFrameColor);
                    Swap<byte[]>(ref nextFrameColor, ref currentFrameColor);
                }

                if (settings.ProcessColorRAW)
                { 
                    lock(nextFrameColorRaw)
                    {
                        nextFrameColorRaw = AssembleGenericImagePacket(ticks, mColorImageYUY.DataIntPtr, colorImageWidth*colorImageHeight*2, (int)MessageTypes.COLOR_RAWYUY);
                        Swap<byte[]>(ref nextFrameColorRaw, ref currentFrameColorRaw);  
                    }
                }
                nextColorFrameReady.Set();
            }

            killProcessColorThread = true;

            processColorThreadCompleted.Set();
        }

        /// <summary>
        /// Very primitive way of resetting the streaming of encoded color to ensure that all clients get the correct first frame. 
        /// </summary>
        public void ResetColorStreaming(KinectServerSettings.ColorCompressionType newCompressionType)
        {
            //Console.WriteLine("Resetting color stream!");
            settings.StreamColor = false;
            bool success = processColorThreadCompleted.WaitOne(300);
            Console.WriteLine("Reset color streaming success = " + success);
            settings.colorCompression = newCompressionType;
            settings.StreamColor = true;
        }
        public void ResetColorStreaming()
        {
            ResetColorStreaming(settings.colorCompression);
        }

        /// <summary>
        /// Assembles a generic image packet with a timestamp (long) followed by a size of the image (int) and the image data
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <param name="imageDataPtr"></param>
        /// <param name="imageSize"></param>
        /// <returns></returns>
        private unsafe byte[] AssembleGenericImagePacket(long timeStamp, IntPtr imageDataPtr, int imageSizeBytes, int packetType)
        {
            //if you want to just return the image without the timestamp:
            //return imageDataYUY.ToByteArray();
            stopwatch4.Reset();
            stopwatch4.Start();
            int preambleLength = 8 + 4;
            //if timestamp is wanted
            byte[] arr = new byte[imageSizeBytes + preambleLength];

            using (MemoryStream memoryStream = new MemoryStream(arr))
            {
                memoryStream.Write(BitConverter.GetBytes(timeStamp), 0, 8);
                memoryStream.Write(BitConverter.GetBytes(packetType), 0, 4);
            }

            //now move the data to the managed heap
            fixed (byte* p = arr)
            {
                byte* p1 = p + preambleLength;//hack to not overwrite the timestamp 
                Win32.CopyMemory((IntPtr)p1, imageDataPtr, (UIntPtr)imageSizeBytes);
            }
            stopwatch4.Stop();
            return arr; 
        }

        private byte[] AssembleColorPacketJPEG(long timeStamp, IntPtr imageData, System.Windows.Media.PixelFormat format, int width, int height)
        {
            stopwatch4.Reset();
            stopwatch4.Start();
            BitmapSource srcImage = BitmapSource.Create(
                width,
                height,
                96,
                96,
                format,
                null,
                imageData,
                width * height * 4,
                width * 4);

            // Convert the color frame to RGB24 pixel format
            FormatConvertedBitmap image = new FormatConvertedBitmap();
            image.BeginInit();
            image.Source = srcImage;
            image.DestinationFormat = System.Windows.Media.PixelFormats.Rgb24;
            image.EndInit();

            // Memory stream used for encoding.
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(BitConverter.GetBytes(timeStamp), 0, 8);
                memoryStream.Write(BitConverter.GetBytes((int)MessageTypes.COLOR_JPEG), 0, 4);

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();

                // Add the frame to the encoder.
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(memoryStream);

                var buf = memoryStream.ToArray();
                
                stopwatch4.Stop();
                if (ShowTimingInformation) Console.WriteLine(string.Format("JPEG: {0} ms {1} bytes", stopwatch4.ElapsedMilliseconds, buf.Length));
                
                return buf;
            }
        }
#endregion

#region DepthProcessing
        System.Diagnostics.Stopwatch stopwatchTotal = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch stopwatchGetFrames = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch stopwatchBlur = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch stopwatchForeground = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch stopwatch4 = new System.Diagnostics.Stopwatch();
        /// <summary>
        /// Handles the depth/ir/body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private unsafe void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            fpsKinectDepth.Tick();

            MultiSourceFrameReference frameReference = e.FrameReference;

            MultiSourceFrame multiSourceFrame = null;
            DepthFrame depthFrame = null;
            InfraredFrame infraredFrame = null;
            BodyFrame bodyFrame = null;
            BodyIndexFrame bodyIndexFrame = null;


            stopwatchTotal.Restart();
            stopwatchGetFrames.Restart();
            try
            {
                multiSourceFrame = frameReference.AcquireFrame();

                if (multiSourceFrame != null)
                {

                    depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                    infraredFrame = multiSourceFrame.InfraredFrameReference.AcquireFrame();

                    if ((depthFrame != null) && (infraredFrame != null))
                    {
                        var timestampInfrared = infraredFrame.RelativeTime;
                        var timestampDepth = depthFrame.RelativeTime;

                        //INFRARED
                        var buffer1 = infraredFrame.LockImageBuffer();
                        mInfraredImageM.Copy(buffer1.UnderlyingBuffer);
                        buffer1.Dispose();
                        lock (mInfraredImage)
                        {
                            if (settings.FlipImages)
                                mInfraredImage.XMirror(mInfraredImageM);
                            else
                                Swap<ShortImage>(ref mInfraredImage, ref mInfraredImageM);
                        }
                        timeStampInfrared = infraredFrame.RelativeTime.Ticks;                        

                        //DEPTH
                        var buffer2 = depthFrame.LockImageBuffer();
                        mDepthImageM.Copy(buffer2.UnderlyingBuffer);
                        lock (mDepthFrameDataLock)
                        {
                            depthFrame.CopyFrameDataToArray(mDepthFrameData);
                            mKinectSensor.CoordinateMapper.MapColorFrameToDepthSpace(mDepthFrameData, mDepthSpacePoints);
                        }
                        buffer2.Dispose();
                        lock (mDepthImage)
                        {
                            if (settings.FlipImages)
                                mDepthImage.XMirror(mDepthImageM);
                            else
                                Swap<ShortImage>(ref mDepthImage, ref mDepthImageM);
                        }

                        bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();
                        if (bodyIndexFrame != null)
                        {
                            
                            var buffer3 = bodyIndexFrame.LockImageBuffer();
                            mBodyIndexImageM.Copy(buffer3.UnderlyingBuffer);
                            buffer3.Dispose();
                            lock (mBodyIndexImage)
                            {
                                if(settings.FlipImages)
                                    mBodyIndexImage.XMirror(mBodyIndexImageM);
                                else
                                    Swap<ByteImage>(ref mBodyIndexImage, ref mBodyIndexImageM);
                            }
                        }
                        stopwatchGetFrames.Stop();
                        //for acquiring and computing the average background image
                        if (acquireBackgroundCounter > 0)
                        {
                            preProcess.Update(mDepthImage);
                            acquireBackgroundCounter--;
                            if (acquireBackgroundCounter == 0)
                            {
                                Console.WriteLine("Acquired enough frames. Computing background image!");
                                FloatImage avgDepth;
                                FloatImage varDepthIm;
                                ByteImage mask;

                                preProcess.Compute(out avgDepth, out varDepthIm, out mask);
                                avgDepth.Mult(1000f);//averages are computed in meters for some reason

                                lock (mDepthBackgroundImage)
                                {
                                    mDepthBackgroundImage.Copy(avgDepth);
                                    Console.WriteLine("Saving background DEPTH image to file " + backgroundFileName +".bin");
                                    mDepthBackgroundImage.SaveToFile(backgroundFileName + ".bin");
                                }
                                lock (mColorImage)
                                {
                                    Console.WriteLine("Saving background COLOR image to file " + backgroundFileName + ".jpg");
                                    ObjFile.SaveColorToJPEG(backgroundFileName + ".jpg", mColorImage);
                                }
                            }
                        }
                        stopwatchForeground.Restart();
                        lock (mDepthForegroundImage)
                        {
                            ushort* pIn = mDepthImage.Data(0, 0);
                            ushort* pBack = mDepthBackgroundImage.Data(0, 0);
                            ushort* pOut = mDepthForegroundImage.Data(0, 0);
                            byte* pBody = mBodyIndexImage.Data(0, 0);

                            for (int i = 0; i < depthImageWidth * depthImageHeight; i++)
                            {
                                if (*pBack > settings.ThresholdNoise)
                                    *pOut++ = (*pIn < (*pBack - settings.ThresholdNoise) || *pBody != 255) ? *pIn : (byte)0;
                                else
                                    *pOut++ = *pIn;

                                pIn++; pBack++; pBody++;
                            }
                        }
                        stopwatchForeground.Stop();

                        if (settings.BlurDepthImages)
                        {
                            stopwatchBlur.Restart();
                            lock (mSmoothedDepthForegroundImage)
                                mSmoothedDepthForegroundImage.Blur5x5NonZero(mDepthForegroundImage);
                            stopwatchBlur.Stop();
                        }

                        lock (mDepthPlayerIndexImage)
                        {
                            ushort* pIn = mDepthImage.Data(0, 0);
                            byte* pMask = mBodyIndexImage.Data(0, 0);
                            ushort* pOut = mDepthPlayerIndexImage.Data(0, 0);

                            for (int i = 0; i < depthImageWidth * depthImageHeight; i++)
                            {
                                *pOut++ = (*pMask != 255) ? *pIn : (byte)0;
                                *pMask++ *= 40;
                                pIn++;
                            }
                        }
                        
                        bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();
                        
                        if (bodyFrame != null)
                        {
                            foreach (BodyContainer skeleton in skeletons.Values) skeleton.updated = false; //mark them all as not-fresh
                        
                            var floorClipPlane = bodyFrame.FloorClipPlane;
                            //BENKO CONVERSION FIX - check that this is really a good gravity vector.
                            // Not sure why this is necessary to bring this into agreement with kinect v1:
                            // one way to check this to look at the floor plane as estimated by above (kinectNormal): they should agree
                            lastKinectAccReading.X = floorClipPlane.X;
                            lastKinectAccReading.Y = -floorClipPlane.Y;
                            lastKinectAccReading.Z = -floorClipPlane.Z;
                            lastKinectAccReading.W = floorClipPlane.W;
                            
                            // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                            // As long as those body objects are not disposed and not set to null in the array,
                            // those body objects will be re-used.
                            bodyFrame.GetAndRefreshBodyData(mBodies); //get skeleton data

                            for(int k =0; k<bodyCount; k++)
                            {
                                Body body = this.mBodies[k];
                                if (body.IsTracked)
                                {
                                    //Console.WriteLine(body.TrackingId);
                                    BodyContainer skeleton; 
                                    if (skeletons.ContainsKey(body.TrackingId))
                                    {
                                        skeleton = skeletons[body.TrackingId];
                                    }
                                    else
                                    {
                                        skeleton = new BodyContainer();
                                        skeletons.Add(body.TrackingId, skeleton);
                                    }
                                    skeleton.ID = body.TrackingId;// validBodiesCount;
                                    skeleton.updated = true;
                                    //trackingBodyID = validBodiesCount;
                                    //validBodiesCount++;

                                    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                    skeleton.HandLeftConfidence = (byte)body.HandLeftConfidence; //0=not confident, 1= confident
                                    skeleton.HandRightConfidence = (byte)body.HandRightConfidence;
                                    skeleton.HandLeftState = (byte)body.HandLeftState;
                                    skeleton.HandRightState = (byte)body.HandRightState;

                                    // convert the joint points to depth (display) space (for rendering only)
                                    int i = 0;
                                    foreach (JointType jointType in joints.Keys)
                                    {
                                        skeleton.jointPositions[i] = joints[jointType].Position;//store joints for sending to remote clients
                                        skeleton.jointStates[i] = (byte)joints[jointType].TrackingState; //store the tracking state for each joint

                                        DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(joints[jointType].Position);
                                        skeleton.jointPoints[jointType] = new SharpDX.Point((int)depthSpacePoint.X, (int)depthSpacePoint.Y);

                                        i++;
                                    }

                                    //IMPORTANT: Face tracking doesn't work without that!
                                    // update the face frame source to track this body 
                                    this.faceFrameSources[k].TrackingId = body.TrackingId;                                   
                                }
                            }
                            
                            //cleanup old skeletons
                            List<ulong> toRemove = new List<ulong>();
                            foreach (BodyContainer skeleton in skeletons.Values) if (!skeleton.updated) toRemove.Add(skeleton.ID);
                            foreach (ulong id in toRemove) skeletons.Remove(id);

                        }

                        long currTime = kinectTimer.ElapsedMilliseconds;
                        AssembleSkeletonsPacket(currTime);
                        lock (nextFrameSkeleton)
                        {
                            Swap<byte[]>(ref nextFrameSkeleton, ref currentFrameSkeleton);
                        }

                        lock (nextFrameDepth)
                        {
                            switch (settings.streamType)
                            {
                                case KinectServerSettings.StreamType.All:
                                    nextFrameDepth = AssembleGenericImagePacket(currTime, mDepthImage.DataIntPtr, depthImageWidth * depthImageHeight * 2, (int)MessageTypes.DEPTH);
                                    break;
                                case KinectServerSettings.StreamType.BodyIndex:
                                    nextFrameDepth = AssembleGenericImagePacket(currTime, mDepthPlayerIndexImage.DataIntPtr, depthImageWidth * depthImageHeight * 2, (int) MessageTypes.BODYINDEX);
                                    break;
                                default:
                                case KinectServerSettings.StreamType.Foreground:
                                    if(settings.BlurDepthImages) 
                                        nextFrameDepth = AssembleGenericImagePacket(currTime, mSmoothedDepthForegroundImage.DataIntPtr, depthImageWidth * depthImageHeight * 2, (int)MessageTypes.DEPTH_FOREGROUND);
                                    else
                                        nextFrameDepth = AssembleGenericImagePacket(currTime, mDepthForegroundImage.DataIntPtr, depthImageWidth * depthImageHeight * 2, (int)MessageTypes.DEPTH_FOREGROUND);
                                    break;
                            }
                            Swap<byte[]>(ref nextFrameDepth, ref currentFrameDepth);
                        }

                        lock (nextFrameIr)
                        {
                            nextFrameIr = AssembleGenericImagePacket(currTime, mInfraredImage.DataIntPtr, depthImageWidth * depthImageHeight * 2, (int)MessageTypes.IR);
                            Swap<byte[]>(ref nextFrameIr, ref currentFrameIr);
                        }
                        stopwatchTotal.Stop();

                        if (ShowTimingInformation) Console.WriteLine("MultiFrame Get: {0} ms  Blr: {1} ms  FG: {2} ms Tot: {3} ms",
                            stopwatchGetFrames.ElapsedMilliseconds,
                            stopwatchBlur.ElapsedMilliseconds,
                            stopwatchForeground.ElapsedMilliseconds, 
                            stopwatchTotal.ElapsedMilliseconds);
                        nextFrameReady.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught: " + ex.ToString());
                // ignore if the frame is no longer available
            }
            finally
            {
                if (depthFrame != null) depthFrame.Dispose();
                if (infraredFrame != null) infraredFrame.Dispose();
                if (bodyFrame != null) bodyFrame.Dispose();
                if (bodyIndexFrame != null) bodyIndexFrame.Dispose();
            }
        }

        public float[] MapColorFrameToDepthSpace()
        {
            for (int i = 0; i < mDepthSpacePoints.Length; i++)
            {
                mDepthSpacePointsInFloats[i * 2] = mDepthSpacePoints[i].X;
                mDepthSpacePointsInFloats[i * 2 + 1] = mDepthSpacePoints[i].Y;
            }
            return mDepthSpacePointsInFloats;
        }

        public float[] MapColorSpaceToDepthSpace(int[] colorPixels)
        {
            float[] depthPixels = new float[colorPixels.Length];
            for (int i = 0; i + 1 < colorPixels.Length; i += 2)
            {
                int colorX = colorPixels[i];
                int colorY = colorPixels[i + 1];
                int depthSpacePointsIndex = colorY * colorImageWidth + colorX;
                DepthSpacePoint dsp = mDepthSpacePoints[depthSpacePointsIndex];
                depthPixels[i] = dsp.X;
                depthPixels[i + 1] = dsp.Y;
                //Console.WriteLine(" ----- colorPixels are " + colorPixels[i] + ", " + colorPixels[i + 1]);
                //Console.WriteLine(" ----- depthPixels are " + depthPixels[i] + ", " + depthPixels[i + 1]);
            }
            return depthPixels;
        }

        public ushort[] MapColorSpaceToDepth(int[] colorPixels)
        {
            ushort[] depths = new ushort[colorPixels.Length / 2];
            for (int i = 0; i + 1 < colorPixels.Length; i += 2)
            {
                int colorX = colorPixels[i];
                int colorY = colorPixels[i + 1];
                int depthSpacePointsIndex = colorY * colorImageWidth + colorX;
                DepthSpacePoint dsp = mDepthSpacePoints[depthSpacePointsIndex];
                if (float.IsNegativeInfinity(dsp.X) || float.IsNegativeInfinity(dsp.Y))
                {
                    depths[i / 2] = 0;
                    //Console.WriteLine(" ----- Depths are " + depths[i / 2]);
                    continue;
                }
                int depthFrameDataIndex = (int)dsp.Y * depthImageWidth + (int)dsp.X; // TODO what's the proper rounding here?
                depths[i / 2] = mDepthFrameData[depthFrameDataIndex]; // TODO add appropriate locking
                //Console.WriteLine(" ----- Depths are " + depths[i / 2]);
            }
            return depths;
        }


        public long timeStampInfrared;

        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private void AssembleSkeletonsPacket(long timeStamp)
        {
            //assemble a message
            MemoryStream stream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(stream);
            
            binaryWriter.Write(timeStamp); //CHANGED to front
            
            binaryWriter.Write((byte)skeletons.Count);
            foreach (BodyContainer skeleton in skeletons.Values)
            {
                binaryWriter.Write(skeleton.ID);

                for (int i = 0; i < 25; i++) 
                {

                    var joint = skeleton.jointPositions[i];
                    binaryWriter.Write(joint.X);
                    binaryWriter.Write(joint.Y);
                    binaryWriter.Write(joint.Z);

                    binaryWriter.Write(skeleton.jointStates[i]);
                }

                binaryWriter.Write(skeleton.HandLeftConfidence);
                binaryWriter.Write(skeleton.HandLeftState);
                binaryWriter.Write(skeleton.HandRightConfidence);
                binaryWriter.Write(skeleton.HandRightState);

                //face information
                for(int i = 0; i<5; i++)
                {
                    binaryWriter.Write(skeleton.facePointsWorld[i].X);
                    binaryWriter.Write(skeleton.facePointsWorld[i].Y);
                    binaryWriter.Write(skeleton.facePointsWorld[i].Z);
                }
                binaryWriter.Write(skeleton.faceRotationYPR.X); //yaw
                binaryWriter.Write(skeleton.faceRotationYPR.Y); //pitch
                binaryWriter.Write(skeleton.faceRotationYPR.Z); //roll

                binaryWriter.Write(skeleton.glasses);
                binaryWriter.Write(skeleton.happy);
                binaryWriter.Write(skeleton.engaged);
                binaryWriter.Write(skeleton.lookingAway);
                binaryWriter.Write(skeleton.leftEyeClosed);
                binaryWriter.Write(skeleton.rightEyeClosed);
                binaryWriter.Write(skeleton.mouthOpen);
                binaryWriter.Write(skeleton.mouthMoved);
            }
            //kinect accelerometer reading
            binaryWriter.Write(lastKinectAccReading.X);
            binaryWriter.Write(lastKinectAccReading.Y);
            binaryWriter.Write(lastKinectAccReading.Z);
            binaryWriter.Write(lastKinectAccReading.W);


            //send it to connected clients
            lock (nextFrameSkeleton)
            {
                nextFrameSkeleton = stream.ToArray();
            }
        }

        private void AssembleAudioPacket(long timeStamp)
        {
            byte[] audioData = GetLatestAudio();
            byte[] frame = new byte[audioData.Length + 8 + 4];
            using (MemoryStream memoryStream = new MemoryStream(frame))
            {
                memoryStream.Write(BitConverter.GetBytes(timeStamp), 0, 8);
                memoryStream.Write(BitConverter.GetBytes((int)MessageTypes.AUDIO), 0, 4);
                memoryStream.Write(audioData, 0, audioData.Length);
            }

            lock (nextFrameAudio)
                nextFrameAudio = frame;
        }

        private void AssembleAudioPacketDirectCapture(long timeStamp, CaptureBuffer captureBuffer)
        {
            byte[] frame = new byte[audioCaptureBuffer.Capabilities.BufferBytes + 8 + 4];
            using (MemoryStream memoryStream = new MemoryStream(frame))
            {
                memoryStream.Write(BitConverter.GetBytes(timeStamp), 0, 8);
                memoryStream.Write(BitConverter.GetBytes((int)MessageTypes.AUDIO), 0, 4);
            }
            audioCaptureBuffer.Read<byte>(frame, 8 + 4, audioCaptureBuffer.Capabilities.BufferBytes, 0, LockFlags.None); //BENKO: When switching to SharpDX, I had to specify LockFlags, not sure this flag is correct.  

            lock (nextFrameAudio)
                nextFrameAudio = frame;
        }
#endregion

#region FaceProcessing
        /// <summary>
        /// Converts rotation quaternion to Euler angles 
        /// And then maps them to a specified range of values to control the refresh rate
        /// </summary>
        /// <param name="rotQuaternion">face rotation quaternion</param>
        /// <param name="pitch">rotation about the X-axis</param>
        /// <param name="yaw">rotation about the Y-axis</param>
        /// <param name="roll">rotation about the Z-axis</param>
        private static void ExtractFaceRotationInDegrees(Microsoft.Kinect.Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            // clamp the values to a multiple of the specified increment to control the refresh rate
            double increment = FaceRotationIncrementInDegrees;
            pitch = (int)(Math.Floor((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
            yaw = (int)(Math.Floor((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
            roll = (int)(Math.Floor((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
        }

        /// <summary>
        /// Handles the face frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private unsafe void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // get the index of the face source from the face source array
                    int index = this.GetFaceSourceIndex(faceFrame.FaceFrameSource);

                    // check if this face frame has valid face frame results
                    if (this.ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult))
                    {
                        // store this face frame result to draw later
                        this.faceFrameResults[index] = faceFrame.FaceFrameResult;

                        //update the corresponding skeleton information
                        BodyContainer skeleton = skeletons[faceFrame.TrackingId];
                        if(skeleton != null)
                        {
                            skeleton.faceRotationQ = faceFrame.FaceFrameResult.FaceRotationQuaternion;
                            int yaw,pitch, roll;
                            ExtractFaceRotationInDegrees(faceFrame.FaceFrameResult.FaceRotationQuaternion, out pitch, out yaw, out roll);
                            skeleton.faceRotationYPR = new Vector3(yaw, pitch, roll);

                            //infrared
                            RectI faceBoxSource = faceFrame.FaceFrameResult.FaceBoundingBoxInInfraredSpace;
                            int leftBox = settings.FlipImages ? depthImageWidth - faceBoxSource.Right : faceBoxSource.Left;
                            int rightBox = settings.FlipImages ? depthImageWidth - faceBoxSource.Left : faceBoxSource.Right;

                            skeleton.faceRectInInfraredSpace = new SharpDX.Mathematics.Interop.RawRectangleF(leftBox, faceBoxSource.Top, rightBox, faceBoxSource.Bottom);

                            int i =0;
                            foreach(PointF p in faceFrame.FaceFrameResult.FacePointsInInfraredSpace.Values)
                            {
                                DepthSpacePoint dp = new DepthSpacePoint();
                                dp.X = p.X; dp.Y = p.Y;
                                int x = (int)(settings.FlipImages ? depthImageWidth - p.X : p.X);

                                ushort depth = mDepthImage[x, (int)dp.Y]; //account for flipping
                                CameraSpacePoint cp = this.coordinateMapper.MapDepthPointToCameraSpace(dp, depth); //look up without flipping
                                skeleton.facePointsWorld[i] = cp;
                                skeleton.facePointsInInfraredSpace[i] = new System.Drawing.PointF(x, dp.Y); //account for flipping
                                i++;
                            }

                            //color space
                            faceBoxSource = faceFrame.FaceFrameResult.FaceBoundingBoxInColorSpace;
                            leftBox = settings.FlipImages ? colorImageWidth - faceBoxSource.Right : faceBoxSource.Left;
                            rightBox = settings.FlipImages ? colorImageWidth - faceBoxSource.Left : faceBoxSource.Right;
                            skeleton.faceRectInColorSpace = new SharpDX.Mathematics.Interop.RawRectangleF(leftBox, faceBoxSource.Top, rightBox, faceBoxSource.Bottom);

                            i = 0;
                            foreach(PointF p in faceFrame.FaceFrameResult.FacePointsInColorSpace.Values)
                            {
                                float x = settings.FlipImages ? colorImageWidth - p.X : p.X;
                                skeleton.facePointsInColorSpace[i++] = new System.Drawing.PointF(x, p.Y);
                            }

                            skeleton.engaged = (byte)faceFrame.FaceFrameResult.FaceProperties[FaceProperty.Engaged];
                            skeleton.happy = (byte)faceFrame.FaceFrameResult.FaceProperties[FaceProperty.Happy];
                            skeleton.glasses = (byte)faceFrame.FaceFrameResult.FaceProperties[FaceProperty.WearingGlasses];
                            skeleton.lookingAway = (byte)faceFrame.FaceFrameResult.FaceProperties[FaceProperty.LookingAway];
                            skeleton.leftEyeClosed = (byte)faceFrame.FaceFrameResult.FaceProperties[FaceProperty.LeftEyeClosed];
                            skeleton.rightEyeClosed = (byte)faceFrame.FaceFrameResult.FaceProperties[FaceProperty.RightEyeClosed];
                            skeleton.mouthOpen = (byte)faceFrame.FaceFrameResult.FaceProperties[FaceProperty.MouthOpen];
                            skeleton.mouthMoved = (byte)faceFrame.FaceFrameResult.FaceProperties[FaceProperty.MouthMoved];
                        }
                    }
                    else
                    {
                        // indicates that the latest face frame result from this reader is invalid
                        this.faceFrameResults[index] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the index of the face frame source
        /// </summary>
        /// <param name="faceFrameSource">the face frame source</param>
        /// <returns>the index of the face source in the face source array</returns>
        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < this.bodyCount; i++)
            {
                if (this.faceFrameSources[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Validates face bounding box and face points to be within screen space
        /// </summary>
        /// <param name="faceResult">the face frame result containing face box and points</param>
        /// <returns>success or failure</returns>
        private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (faceBox != null)
                {
                    // check if we have a valid rectangle within the bounds of the screen space
                    isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                  (faceBox.Bottom - faceBox.Top) > 0 &&
                                  faceBox.Right <= colorImageWidth &&
                                  faceBox.Bottom <= colorImageHeight;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (Microsoft.Kinect.PointF pointF in facePoints.Values)
                            {
                                // check if we have a valid face point within the bounds of the screen space
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < colorImageWidth &&
                                                        pointF.Y < colorImageHeight;

                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return isFaceValid;
        }
#endregion FaceProcessing

#region Form Handling Methods
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
        }

        private void comboBoxDisplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            display = (DisplayType)comboBoxDisplay.SelectedIndex;
        }

        private void checkBoxSkeleton_CheckedChanged(object sender, EventArgs e)
        {
            settings.RenderSkeleton = checkBoxSkeleton.Checked;
        }

        private void comboBoxStreamType_SelectedIndexChanged(object sender, EventArgs e)
        {
            settings.streamType = (KinectServerSettings.StreamType)comboBoxStreamType.SelectedIndex;
        }

        private void buttonBackground_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Starting background acquisition...");
            acquireBackgroundToolStripMenuItem.Enabled = false;
            preProcess.Reset();
            acquireBackgroundCounter = 100;
        }

        private void trackBarThreshold_Scroll(object sender, EventArgs e)
        {
            settings.ThresholdNoise = (ushort)trackBarThreshold.Value;
        }

        private void checkBoxBlur_CheckedChanged(object sender, EventArgs e)
        {
            settings.BlurDepthImages = checkBoxBlur.Checked;
        }

        private void checkBoxStreamColor_CheckedChanged(object sender, EventArgs e)
        {
            settings.StreamColor = checkBoxStreamColor.Checked;
            if (!settings.StreamColor) checkBoxProcessRAW.Checked = false;
            checkBoxProcessRAW.Enabled = settings.StreamColor;
        }

        private void checkBoxStreamAudio_CheckedChanged(object sender, EventArgs e)
        {
            settings.StreamAudio = checkBoxStreamAudio.Checked;
        }

        private void comboBoxColorCompression_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetColorStreaming((KinectServerSettings.ColorCompressionType)comboBoxColorCompression.SelectedIndex);
        }

        private void checkBoxEncoderTiming_CheckedChanged(object sender, EventArgs e)
        {
            ShowTimingInformation = checkBoxEncoderTiming.Checked;
        }

        private void checkBoxProcessRAW_CheckedChanged(object sender, EventArgs e)
        {
            settings.ProcessColorRAW = checkBoxProcessRAW.Checked;
        }

        bool killAllClients = false;
        private void buttonKillAll_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Killing all clients");

            killAllClients = true;
        }


        private void checkBoxRenderFaceTracking_CheckedChanged(object sender, EventArgs e)
        {
            settings.RenderFaces = checkBoxRenderFaceTracking.Checked;
        }


        private void checkBoxFlip_CheckedChanged(object sender, EventArgs e)
        {
            settings.FlipImages = checkBoxFlip.Checked;
        }


        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settings.Save(settingsFileName);
        }

        private void saveToOBJToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "obj files (*.obj)|*.obj|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 0;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Console.WriteLine("Saving OBJ file " + saveFileDialog.FileName);
                    Console.WriteLine("IMPORTANT: Make sure that Flip Image option is turned OFF when the background images are acquired! Otherwise the saved OBJ will not be correct!");
                    ObjFile.Save(saveFileDialog.FileName, kinect2Calibration, mDepthBackgroundImage, backgroundFileName + ".jpg", RoomAliveToolkit.Matrix.Identity(4,4));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not save OBJ file to disk.\n" + ex);
                }
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
#endregion
    }
}
