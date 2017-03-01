using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using RoomAliveToolkit.Images;

namespace RoomAliveToolkit
{
    public enum FileStreamingMode
    {
        None,Read,ReadPreloaded,Write
    }

    [AddComponentMenu("RoomAliveToolkit/RATKinectClient")]
    /// <summary>
    /// Behavior that processes image/depth/audio data received over the network from KinectV2Server and makes it available to the scene.  
    /// </summary>
    public class RATKinectClient : RATSkeletonProvider
    {
        /// <summary>
        /// Calibration data asset. This is a component of the scene where the XML calibration file is read that is acquired by the room calibration routines. See CalibrateEnsamble example in RoomAlive Toolkit. 
        /// </summary>
        public RATCalibrationData calibrationData;
        /// <summary>
        /// Name of the Kinect camera in the calibration file
        /// </summary>
        public string nameInConfiguration = "0";
        private bool makeConfigurationRequests = true;
        /// <summary>
        /// Filename of the local calibration file in case one is not connecting to the Kinect server. I.e., one is not running Kinect feed live. 
        /// </summary>
        public string localCalibrationFilename = "";
        [HideInInspector]
        ProjectorCameraEnsemble.Camera cameraConfig = null;
        /// <summary>
        /// The calibration information for this Kinect camera. It gets read from the local file (if specified) or loaded directly from the KinectV2Server
        /// </summary>
        [SerializeField]
        [HideInInspector]
        public ProjectorCameraEnsemble.Camera.Kinect2Calibration calibration;

        /// <summary>
        /// A flag to signal whether the calibration information has been loaded properly on runtime. 
        /// </summary>
        public bool calibrationLoaded = false;

        [Space(10)]
        /// <summary>
        /// Name/IP address of the KinectV2Server. 
        /// </summary>
        public string serverIPAddress = "localhost";

        [Space(10)]
        public bool StreamDepth = true;
        public bool StreamColor = true;
        public bool StreamSkeleton = true;
        public bool StreamAudio = false;
       
        [Space(10)]
        // FPS computation (these will not be visible in the inspector)
        private FrameRate fpsUpdate = new FrameRate(1f);
        private FrameRate fpsKinectDepth = new FrameRate(1f);
        private FrameRate fpsKinectColor = new FrameRate(1f);
        private FrameRate fpsKinectSkeleton = new FrameRate(1f);
        private FrameRate fpsKinectAudio = new FrameRate(1f);
        [Space(10)]

        [ReadOnly]
        public float updateFPS = 0;
        [ReadOnly]
        public float depthFPS = 0;
        [ReadOnly]
        public float colorFPS = 0;
        [ReadOnly]
        public float skeletonFPS = 0;
        [ReadOnly]
        public float audioFPS = 0;


        /// <summary>
        /// If serializing live data to file, this option can be selected to create a Wav file automatically. 
        /// </summary>
        [HideInInspector]
        public bool writeAudioToWav = false;
 

        // State
        private bool inited = false;
        byte validBodiesCount;

        // Depth and color textures
        [SerializeField]
        [HideInInspector]
        public Float2Image DepthSpaceToCameraSpaceTable;
        public int depthToCameraSpaceTableUpdateCount { get; private set; }

        /// <summary>
        /// The current depth texture received from the server. 
        /// </summary>
        [Space(10)]
        [ReadOnly]
        public Texture2D DepthTexture;
        /// <summary>
        /// The current RGB texture received from the server. 
        /// </summary>
        [ReadOnly]
        public Texture2D RGBTexture;

        protected IntPtr nativeRGBTexPtr;
        [Space(10)]
        public const int depthWidth = 512;
        public const int depthHeight = 424;
        public const int colorWidth = 1920;
        public const int colorHeight = 1080;
        [Space(10)]
        // For network streaming of Kinect data
        private TCPNetworkStreamer depthClient;
        protected int depthPort = 10010;
        private TCPNetworkStreamer colorClient;
        protected int colorPort = 10011;
        private TCPNetworkStreamer audioClient;
        protected int audioPort = 10004;
        private TCPNetworkStreamer skeletonClient;
        protected int skeletonPort = 10005;
        private TCPNetworkStreamer configurationClient;
        protected int configurationPort = 10009;

        [Space(10)]

        private bool runThreads = true;
        //Unity Matrices - but RIGTH handed coordinate system (used by Kinect)
        [HideInInspector]
        public Matrix4x4 RGB_Intrinsics;
        [HideInInspector]
        public Matrix4x4 RGB_Extrinsics;
        [HideInInspector]
        public Matrix4x4 IR_Intrinsics;
        [HideInInspector]
        public Vector4 RGB_DistCoef;
        [HideInInspector]
        public Vector4 IR_DistCoef;

        // Wait handles for frame streaming events
        private AutoResetEvent nextDepthFrameReady = new AutoResetEvent(false);
        private AutoResetEvent nextColorFrameReady = new AutoResetEvent(false);
        private AutoResetEvent nextAudioFrameReady = new AutoResetEvent(false);
        private AutoResetEvent nextSkeletonFrameReady = new AutoResetEvent(false);

        // For file streaming of Kinect data
        private string depthPath, rgbPath, audioPath, skeletonPath, depthToCameraTablePath, kinectCalibrationPath;
        private Thread kinectFileStreamerThread = null;
        private IFormatter formatter;
        private Stream depthStream, rgbStream, audioStream, skeletonStream;
        private double streamTime = 0;
        private long pendingDepthTimeStamp, pendingRGBTimeStamp, pendingAudioTimeStamp, pendingSkeletonTimeStamp ;
        private bool pendingDepthFrameDeserialized, pendingRGBFrameDeserialized, pendingAudioFrameDeserialized, pendingSkeletonFrameDeserialized;

        /// <summary>
        /// Playback of streamed audio data 
        /// </summary>
        [SerializeField]
        [HideInInspector]
        public AudioClip audioClip = null;
        protected AudioSource _audioSource;
        public AudioSource audioSource {
            get
            {
                if (!StreamAudio)
                    return null;
                if (_audioSource == null)
                {
                    _audioSource = GetComponent<AudioSource>();
                    if (_audioSource == null)
                    {
                        _audioSource = gameObject.AddComponent<AudioSource>();
                    }
                }
                return _audioSource;
            }
        }
        public FileStreamingMode streamingMode
        {
            get
            {
                if (playbackController == null)
                    return FileStreamingMode.None;
                else
                    return playbackController.streamingMode;
            }
        }
        private Queue<float> audioPlaybackBuffer = null;
        private bool audioPlaybackBufferUnderrun = false;
        private MemoryStream wavAudioStream = null;
       
        // Audio streaming settings - these settings must match the ones in the Kinect server 
        //Setting for DirectCapture
        private const int AudioBitsPerSample = 16;
        private const int AudioSamplesPerSecond = 44100;
        private const int AudioChannels = 1;
        private const int AudioFrameSize = AudioChannels * AudioSamplesPerSecond * AudioBitsPerSample / 8 ; //bytes = 44100 * (16/8) * 1 (channel)

        private int DepthFrameSize = 434296;
        private Matrix4x4 localToWorldMatrix;   

        // For detecting frame dropping issues
        protected int numFramesReceived = 0,
            numRGBFramesReceived = 0,
            numFramesSerialized = 0,
            numRGBFramesSerialized = 0,
            numFramesRendered = 0,
            numRGBFramesRendered = 0;

        // Streamed-in frames
        private KinectDepthFrame nextDepthFrame, currentDepthFrame, pendingDepthFrame;
        private KinectRGBFrame nextRGBFrame, currentRGBFrame, pendingRGBFrame;
        private AudioFrame /*nextAudioFrame, currentAudioFrame, */ pendingAudioFrame;
        private KinectSkeletonFrame nextSkeletonFrame, currentSkeletonFrame, pendingSkeletonFrame;

        private Queue<AudioFrame> audioFrameQueue = new Queue<AudioFrame>(10);

        /// <summary>
        /// An extra behavior to enable playback control of the prerecorded Kinect streams. 
        /// </summary>
        [HideInInspector]
        public RATKinectPlaybackController playbackController;

        protected bool usePlaybackControls
        {
            get
            {
                return playbackController != null;
            }
        }
        protected bool localConfig
        {
            get
            {
                return !StreamFromFile && !localCalibrationFilename.Equals("");
            }
        }

        private long streamStartSample;

        //State
        public bool Initialized() { return inited; }
        private bool wantsRestart;

        public void Awake() //guaranteed to be executed before any Start functions
        {
            // Create a depth texture
            DepthTexture = new Texture2D(depthWidth / 2, depthHeight, TextureFormat.ARGB32, false, true);
            DepthTexture.filterMode = FilterMode.Point;
            DepthTexture.Apply();

            // Create a color texture
            RGBTexture = new Texture2D(colorWidth, colorHeight, TextureFormat.ARGB32, false, true); //ARGB32
            RGBTexture.filterMode = FilterMode.Trilinear;
            RGBTexture.Apply();
            nativeRGBTexPtr = RGBTexture.GetNativeTexturePtr();

            LoadCalibrationData();

            Initialize();
        }

        public bool HasCalibration()
        {
            return calibrationData != null;
        }

        public bool StreamFromFile
        {
            get { return streamingMode == FileStreamingMode.Read || streamingMode == FileStreamingMode.ReadPreloaded; }
        }

        public bool StreamToFile
        {
            get { return streamingMode == FileStreamingMode.Write; }
        }

        public void LoadCalibrationData()
        {
            if (HasCalibration())
            {
                ProjectorCameraEnsemble ensembleConfig = calibrationData.GetEnsemble();
                if(ensembleConfig!=null)
                {
                    foreach (ProjectorCameraEnsemble.Camera cc in ensembleConfig.cameras)
                    {
                        if (cc.name == nameInConfiguration)
                        {
                            cameraConfig = cc;
                            calibration = cc.calibration;
                            calibrationLoaded = true;
                        }
                    }
                }
            }
        }

        public void UpdateFromCalibrationData()
        {
            LoadCalibrationData();

            if (calibrationLoaded && cameraConfig != null)
            {
                Debug.Log("RATKinectClient - Loading camera calibration information.");
                SetPose(cameraConfig.pose);

                serverIPAddress = cameraConfig.hostNameOrAddress;
                UpdateKinectCalibrationData(cameraConfig.calibration);
            }
            else
            {
                Debug.Log("RATKinectClient - Using default camera calibration information.");
                RoomAliveToolkit.Matrix p = new Matrix(4, 4);
                p.Identity();
                SetPose(p);
                UpdateKinectCalibrationData(new ProjectorCameraEnsemble.Camera.Kinect2Calibration());         
            }
        }

        void UpdateKinectCalibrationData(ProjectorCameraEnsemble.Camera.Kinect2Calibration newC)
        {
            calibration = newC;

            IR_Intrinsics = RAT2Unity.Convert_3x3To4x4(calibration.depthCameraMatrix);
            RGB_Intrinsics = RAT2Unity.Convert_3x3To4x4(calibration.colorCameraMatrix);
            RGB_Extrinsics = RAT2Unity.Convert(calibration.depthToColorTransform);

            var colorCoef = calibration.colorLensDistortion.AsFloatArray();
            RGB_DistCoef = new Vector4(colorCoef[0], colorCoef[1], 0, 0);// colorCoef[2], colorCoef[3]);

            var irCoef = calibration.depthLensDistortion.AsFloatArray();
            IR_DistCoef = new Vector4(irCoef[0], irCoef[1], 0, 0);//, irCoef[2], irCoef[3]); 
        }

        public void SetPose(RoomAliveToolkit.Matrix pose)
        {
            // Create the world transform node
            Matrix4x4 worldToLocal = RAT2Unity.Convert(pose);// transpose matrix
            worldToLocal = UnityUtilities.ConvertRHtoLH(worldToLocal);

            transform.localPosition = worldToLocal.ExtractTranslation();
            transform.localRotation = worldToLocal.ExtractRotation();
        }

        public void Start()
        {
            Thread.Sleep(5);
        }

        private void LoadKinectCalibration(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ProjectorCameraEnsemble.Camera.Kinect2Calibration));
            var reader = new StreamReader(new FileStream(filename, FileMode.Open));
            calibration = (ProjectorCameraEnsemble.Camera.Kinect2Calibration)serializer.Deserialize(reader);
            reader.Close();
            calibrationLoaded = true;
            UpdateFromCalibrationData();
        }

        public void Initialize()
        {
            // Find the playback controller, if there is one
            if (playbackController == null)
            {
                playbackController = this.GetComponent<RATKinectPlaybackController>();
                if (playbackController == null)
                    playbackController = this.GetComponentInParent<RATKinectPlaybackController>();
            }

            // Construct paths for data streams
            if (usePlaybackControls)
            {
                depthPath = playbackController.streamFolder + name + "_KinectDepth.bin";
                rgbPath = playbackController.streamFolder + name + "_KinectRGB.bin";
                audioPath = playbackController.streamFolder + name + "_Audio.bin";
                skeletonPath = playbackController.streamFolder + name + "_Skeleton.bin";
                depthToCameraTablePath = playbackController.streamFolder + name + "_DepthToCameraTable.bin";
                kinectCalibrationPath = playbackController.streamFolder + name + "_KinectCalibration.xml";
            }

            DepthSpaceToCameraSpaceTable = new Float2Image(depthWidth, depthHeight);
            makeConfigurationRequests = !localConfig;
            if (localConfig)
            {
                DepthSpaceToCameraSpaceTable.LoadFromFile(localCalibrationFilename + "_DepthToCameraTable.bin");
                depthToCameraSpaceTableUpdateCount++;
                LoadKinectCalibration(localCalibrationFilename + "_KinectCalibration.xml");

                if (StreamToFile) //record this info for playback
                {
                    DepthSpaceToCameraSpaceTable.SaveToFile(depthToCameraTablePath);
                    var writer = new StreamWriter(new FileStream(kinectCalibrationPath, FileMode.Create));
                    new XmlSerializer(typeof(ProjectorCameraEnsemble.Camera.Kinect2Calibration)).Serialize(writer, calibration);
                    writer.Close();
                }

            }

            if (StreamAudio)
            {
                // Create audio clip and circular buffer for streaming audio
                audioPlaybackBuffer = new Queue<float>(AudioSamplesPerSecond*4);

                audioClip = AudioClip.Create(this.gameObject.name + "_Audio",
                    1, 1, AudioSamplesPerSecond,
                    true, //for streaming audio
                    _OnAudioRead);

                audioSource.clip = audioClip;
                if (!StreamFromFile)
                { 
                    audioSource.Play();
                    audioSource.playOnAwake = true;
                    audioSource.loop = true;
                }
            }

            // Create current and next frames
            currentDepthFrame = new KinectDepthFrame(depthWidth, depthHeight);
            nextDepthFrame = new KinectDepthFrame(depthWidth, depthHeight);
            currentRGBFrame = new KinectRGBFrame(colorWidth, colorHeight);
            nextRGBFrame = new KinectRGBFrame(colorWidth, colorHeight);
            currentSkeletonFrame = new KinectSkeletonFrame();
            nextSkeletonFrame = new KinectSkeletonFrame();
            pendingSkeletonFrame = new KinectSkeletonFrame();


            // Initialize streaming of Kinect input data from file or network
            if (StreamFromFile)
            {
                _InitStreamFromFile();

                // Start data deserialization
                kinectFileStreamerThread = new Thread(_RunStreamFromFile);
                kinectFileStreamerThread.Start();

                Debug.Log("Loading DepthToColorTable from file:" + depthToCameraTablePath);
                DepthSpaceToCameraSpaceTable.LoadFromFile(depthToCameraTablePath);
                depthToCameraSpaceTableUpdateCount ++;

                try
                {
                    Debug.Log("Loading Kinect2Calibration from file: " + kinectCalibrationPath);
                    LoadKinectCalibration(kinectCalibrationPath);
                }
                catch(Exception)
                {
                    Debug.LogError("Unable to deserialize Kinect2Calibration");
                }
            }
            else
            {
                //connect to the configuration server
                if(!localConfig)
                {
                    configurationClient = new TCPNetworkStreamer();
                    configurationClient.ConnectToSever(serverIPAddress, configurationPort);
                    configurationClient.ReceivedMessage += new TCPNetworkStreamer.ReceivedMessageEventHandler(RemoteComputer_NewConfigurationFrame);
                }
                if (StreamDepth) 
                {
                    depthClient = new TCPNetworkStreamer();
                    depthClient.ConnectToSever(serverIPAddress, depthPort);
                    depthClient.ReceivedMessage += new TCPNetworkStreamer.ReceivedMessageEventHandler(RemoteComputer_NewDepthFrame);
                }

                if (StreamColor)
                {
                    colorClient = new TCPNetworkStreamer();
                    colorClient.ConnectToSever(serverIPAddress, colorPort);
                    colorClient.ReceivedMessage += new TCPNetworkStreamer.ReceivedMessageEventHandler(RemoteComputer_NewColorFrame);
                }
                //if (EnableInfrared)
                //{
                //    infraredClient = new TCPNetworkStreamer();
                //    infraredClient.ConnectToSever(ServerIpAddress, infraredPort);
                //    infraredClient.ReceivedMessage += new TCPNetworkStreamer.ReceivedMessageEventHandler(RemoteComputer_NewIRFrame);
                //}
                if (StreamAudio) 
                {
                    audioClient = new TCPNetworkStreamer();
                    audioClient.ConnectToSever(serverIPAddress, audioPort);
                    audioClient.ReceivedMessage += new TCPNetworkStreamer.ReceivedMessageEventHandler(RemoteComputer_NewAudioFrame);
                }
                if (StreamSkeleton)
                {
                    skeletonClient = new TCPNetworkStreamer();
                    skeletonClient.ConnectToSever(serverIPAddress, skeletonPort);
                    skeletonClient.ReceivedMessage += new TCPNetworkStreamer.ReceivedMessageEventHandler(RemoteComputer_NewSkeletonFrame);
                }
            }

            inited = true;

            // Set up serialization of Kinect data
            if (StreamToFile)
            {
                formatter = new BinaryFormatter();
                try
                {
                    depthStream = new FileStream(depthPath, FileMode.Create);
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to create output file stream for Kinect data to " + depthPath);
                }
                try
                {
                    rgbStream = new FileStream(rgbPath, FileMode.Create);
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to create output file stream for Kinect data to " + rgbPath);
                }
                try
                {
                    audioStream = new FileStream(audioPath, FileMode.Create);
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to create output file stream for Kinect data to " + audioPath);
                }
                try
                {
                    skeletonStream = new FileStream(skeletonPath, FileMode.Create);
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to create output file stream for Kinect data to " + skeletonPath);
                }
               
            }

            if (writeAudioToWav) 
            {
                // For easier writing of streamed audio data to a WAV file,
                // we create a memory stream for that purpose
                wavAudioStream = new MemoryStream();
            }

            Debug.Log("RATKinectClient Initialized: " + serverIPAddress + ":" + depthPort);
        }

        public void SignalRestart()
        {
            wantsRestart = true;
        }

        public void RestartStreamFromFile()
        {
            wantsRestart = false;

            // We have reached the end of the streams, close them
            depthStream.Close();
            if (rgbStream != null)
                rgbStream.Close();
            if (audioStream != null)
                audioStream.Close();
            if (skeletonStream != null)
                skeletonStream.Close();

            // Print how many frames were received and rendered
            _PrintNumFrames();

            _InitStreamFromFile();

            if (writeAudioToWav) 
            {
                // We have reached the end of a serialized data stream on loop,
                // so write the streamed audio to WAV
                _WriteAudioToWav();
            }
        }

        private void _InitStreamFromFile()
        {
            formatter = new BinaryFormatter();
            bool preLoad = streamingMode != FileStreamingMode.ReadPreloaded;
            // Open streams for depth, color, and skeleton

            if(StreamDepth)
            {
                try
                {
                    depthStream = preLoad ?
                    (Stream)(new FileStream(depthPath, FileMode.Open)) :
                    (Stream)(new MemoryStream(File.ReadAllBytes(depthPath)));

                    streamStartSample = 0;
                    if (usePlaybackControls && playbackController.trimProCamPlayback)
                        streamStartSample = DepthFrameSize * playbackController.depthFramesTrimFromStart;

                    depthStream.Seek(streamStartSample, SeekOrigin.Begin);

                    Debug.Log("Opened input stream for Kinect data from " + depthPath);
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to create input stream for Kinect data from " + depthPath);
                    depthStream = null;
                }
            }
            
            if (StreamColor)
            {
                try
                {             
                        rgbStream = preLoad ?
                        (Stream)(new FileStream(rgbPath, FileMode.Open)) :
                        (Stream)(new MemoryStream(File.ReadAllBytes(rgbPath)));
                        rgbStream.Seek(0, SeekOrigin.Begin);

                        Debug.Log("Opened input stream for Kinect data from " + rgbPath);
                
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to create input stream for Kinect data from " + rgbPath);
                    rgbStream = null;
                }
            }

            if(StreamSkeleton)
            {
                try
                {
                    skeletonStream = preLoad ? 
                        (Stream)(new FileStream(skeletonPath, FileMode.Open)) :
                        (Stream)(new MemoryStream(File.ReadAllBytes(skeletonPath)));
                    skeletonStream.Seek(0, SeekOrigin.Begin);

                    Debug.Log("Opened input stream for Kinect data from " + skeletonPath);
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to create input stream for Kinect data from " + skeletonPath);
                    skeletonStream = null;
                }
            }

            if (StreamAudio) 
            {
                // Open audio stream
                try
                {
                    audioStream = !preLoad ? 
                        (Stream)(new FileStream(audioPath, FileMode.Open)) :
                        (Stream)(new MemoryStream(File.ReadAllBytes(audioPath)));
                    audioStream.Seek(0, SeekOrigin.Begin);

                    Debug.Log("Opened input file stream for audio data from " + audioPath);
                }
                catch (Exception)
                {
                    Debug.LogError("Failed to create input file stream for audio data from " + audioPath);
                    audioStream = null;
                }
            }

            // Initialize flags and timing variables
            pendingDepthTimeStamp = pendingRGBTimeStamp = pendingAudioTimeStamp = pendingSkeletonTimeStamp = -1;
            pendingDepthFrameDeserialized = pendingRGBFrameDeserialized = pendingAudioFrameDeserialized = pendingSkeletonFrameDeserialized = false;
            streamTime = 0;

            // Reset frame counters
            numFramesReceived = numFramesSerialized =
                numRGBFramesReceived = numRGBFramesSerialized =
                numFramesRendered = numRGBFramesRendered = 0;

            // Create pending frames
            pendingDepthFrame = new KinectDepthFrame(depthWidth, depthHeight);
            pendingRGBFrame = new KinectRGBFrame(colorWidth, colorHeight);
            pendingAudioFrame = new AudioFrame();
            pendingSkeletonFrame = new KinectSkeletonFrame();
            
        }

        private void _RunStreamFromFile()
        {
            Thread.Sleep(400);
            
            while (runThreads)
            {
                
                if (wantsRestart)
                {
                    RestartStreamFromFile();
                }
                if (!inited)
                {
                    Thread.Sleep(5);
                    continue;
                }

                if (usePlaybackControls && !playbackController.running)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                if (StreamDepth)
                {
                    if (depthStream.Position == streamStartSample)
                    {
                        // Streaming just started, get first depth frame
                        pendingDepthFrame = (KinectDepthFrame)formatter.Deserialize(depthStream);
                        pendingDepthTimeStamp = pendingDepthFrame.timeStampDepth;
                        pendingDepthFrameDeserialized = true;

                        // Set stream time
                        streamTime = pendingDepthTimeStamp;
                    }

                    // First handle depth stream:
                    if (pendingDepthFrameDeserialized)
                    {
                        // Next depth frame in the stream already acquired
                        if (streamTime >= (double)pendingDepthTimeStamp)
                        {
                            lock (nextDepthFrame)
                            {
                                // We have reached the depth frame
                                pendingDepthFrameDeserialized = false;
                                nextDepthFrame = pendingDepthFrame;

                                ++numFramesReceived;
                            }

                            nextDepthFrameReady.Set();
                        }
                    }

                    if (!pendingDepthFrameDeserialized &&
                        depthStream != null && depthStream.Position < depthStream.Length)
                    {
                        // Get the next depth frame
                        pendingDepthFrame = (KinectDepthFrame)formatter.Deserialize(depthStream);
                        pendingDepthTimeStamp = pendingDepthFrame.timeStampDepth;

                        if (streamTime >= (double)pendingDepthTimeStamp)
                        {
                            lock (nextDepthFrame)
                            {
                                // We have reached the depth frame
                                nextDepthFrame = pendingDepthFrame;

                                ++numFramesReceived;
                            }

                            nextDepthFrameReady.Set();
                        }
                        else
                            pendingDepthFrameDeserialized = true;
                    }
                }

                // Next handle color stream:
                if(StreamColor)
                {
                    if (pendingRGBFrameDeserialized && usePlaybackControls)
                    {
                        // Next color frame in the stream already acquired
                        if (streamTime - (double)playbackController.RGBDelay >= (double)pendingRGBTimeStamp)
                        {
                            lock (nextRGBFrame)
                            {
                                // We have reached the color frame
                                pendingRGBFrameDeserialized = false;
                                nextRGBFrame = pendingRGBFrame;

                                ++numRGBFramesReceived;
                            }

                            nextColorFrameReady.Set();
                        }
                    }

                    if (!pendingRGBFrameDeserialized &&
                        rgbStream != null && rgbStream.Position < rgbStream.Length)
                    {
                        // Get the next color frame
                        do
                        {
                            pendingRGBFrame = (KinectRGBFrame)formatter.Deserialize(rgbStream);
                        } while (pendingRGBFrame.timeStampRGB <= streamTime);
                        pendingRGBTimeStamp = pendingRGBFrame.timeStampRGB;
                        pendingRGBFrameDeserialized = true;
                    }
                }
                
                // Next handle skeleton stream:
                if(StreamSkeleton)
                {
                    if (pendingSkeletonFrameDeserialized)
                    {
                        // Next skeleton frame in the stream already acquired

                        if (streamTime >= (double)pendingSkeletonTimeStamp)
                        {
                            lock (nextSkeletonFrame)
                            {
                                pendingSkeletonFrameDeserialized = false;
                                nextSkeletonFrame = pendingSkeletonFrame;
                                nextSkeletonFrameReady.Set();
                            }
                        }
                    }
                    
                    if (!pendingSkeletonFrameDeserialized && skeletonStream != null && skeletonStream.Position < skeletonStream.Length)
                    {
                        // Get the next skeleton frame
                        do
                        {
                            pendingSkeletonFrame = (KinectSkeletonFrame)formatter.Deserialize(skeletonStream);
                        } while (pendingSkeletonFrame.timeStampSkeleton <= streamTime);
                        pendingSkeletonTimeStamp = pendingSkeletonFrame.timeStampSkeleton;
                        pendingSkeletonFrameDeserialized = true;
                    }
                }

                // Next handle audio stream:
                if (StreamAudio)
                {
                    if (pendingAudioFrameDeserialized)
                    {
                        // Next audio frame in the stream already acquired
                        if (streamTime >= (double)pendingAudioTimeStamp)
                        {
                            lock(audioFrameQueue) //lock (nextAudioFrame)
                            {
                                // We have reached the audio frame
                                pendingAudioFrameDeserialized = false;
                                //nextAudioFrame = pendingAudioFrame;
                                audioFrameQueue.Enqueue(pendingAudioFrame);
                            }

                            nextAudioFrameReady.Set();
                        }
                    }

                    if (!pendingAudioFrameDeserialized &&
                        audioStream != null && audioStream.Position < audioStream.Length)
                    {
                        // Get the next audio frame
                        do { pendingAudioFrame = (AudioFrame)formatter.Deserialize(audioStream); }
                        while (pendingAudioFrame.timeStampAudio <= streamTime);
                        pendingAudioTimeStamp = pendingAudioFrame.timeStampAudio;
                        pendingAudioFrameDeserialized = true;
                    }
                }

                // Rewind streams when end is reached:
                bool loopPlayback = !usePlaybackControls || playbackController.loopPlayback;
                long endSample = depthStream.Length;

                if (usePlaybackControls && playbackController.trimProCamPlayback)
                    endSample -= DepthFrameSize * playbackController.depthFramesTrimFromEnd;


                if (depthStream != null && depthStream.Position >= endSample)
                {
                    if (loopPlayback)
                        RestartStreamFromFile();
                    else
                        playbackController.SetEndOfStream();
                }

                Thread.Sleep(1);
            }
            Debug.Log("RATKinectClient.RunStreamFromFile() exited!");
        }


        public void UpdateTextures() //used to be called from the Update function of the ProCamUnit 
        {
            Debug.LogError("RATKinectClient.UpdateTextures() called. This method is deprecated!");
        }

        public unsafe void Update()
        {
            if (!inited)
            {
                return;
            }
            else
            {
                if (makeConfigurationRequests && !StreamFromFile)
                {
                    byte[] b = new byte[1];
                    b[0] = 2;
                    Thread.Sleep(5);
                    configurationClient.SendMessageToAllClients(b); //request the Kinect2Calibration
                    Debug.Log("Configuration request sent");
                }
                makeConfigurationRequests = false;

                localToWorldMatrix = transform.localToWorldMatrix;

                if (StreamFromFile) 
                {
                    if(usePlaybackControls && !playbackController.running)
                        return;

                    // Increment stream timer
                    streamTime += (Time.deltaTime * 1000.0);
                }

                fpsUpdate.Tick();

                //DEPTH
                if (nextDepthFrameReady.WaitOne(1))
                {
                    lock (nextDepthFrame)
                        Swap<KinectDepthFrame>(ref nextDepthFrame, ref currentDepthFrame);

                    DepthTexture.LoadRawTextureData(currentDepthFrame.depthData); //texture unpacking is done in the shader
                    DepthTexture.Apply();

                    ++numFramesRendered;
                    fpsKinectDepth.Tick();
                }
               
                //COLOR
                if (nextColorFrameReady.WaitOne(1))
                {

                    lock (nextRGBFrame)
                        Swap<KinectRGBFrame>(ref nextRGBFrame, ref currentRGBFrame);

                    RGBTexture.LoadImage(currentRGBFrame.rgbData); //loads JPEG image into Texture2D directly
                                                                   
                    ++numRGBFramesRendered;
                    fpsKinectColor.Tick();
                }
                
                //AUDIO
                if (nextAudioFrameReady.WaitOne(1))
                {
                    AudioFrame frame;
                    while (audioFrameQueue.Count > 0)
                    {
                        lock (audioFrameQueue) frame = audioFrameQueue.Dequeue();

                        if (this.gameObject.GetComponent<AudioSource>() != null &&
                            this.gameObject.GetComponent<AudioSource>().clip != null) 
                        {
                            // Enqueue audio frame for playback
                            _PlayAudioFrame16(frame.audioData);

                            // Print a warning in case of audio playback buffer underrun
                            //if (audioPlaybackBufferUnderrun) Debug.LogWarning("Audio playback buffer underrun!");
                        }
                        fpsKinectAudio.Tick();
                    }
                }

                //SKELETON
                if (nextSkeletonFrameReady.WaitOne(1))
                {
                    
                    lock (nextSkeletonFrame)
                        Swap<KinectSkeletonFrame>(ref nextSkeletonFrame, ref currentSkeletonFrame);

                    fpsKinectSkeleton.Tick();
                }
            }
            updateFPS = (float)fpsUpdate.Framerate;
            depthFPS = (float)fpsKinectDepth.Framerate;
            colorFPS = (float)fpsKinectColor.Framerate;
            skeletonFPS = (float)fpsKinectSkeleton.Framerate;
            audioFPS = (float)fpsKinectAudio.Framerate;       
        }

        public ShortImage GetDepthImage()
        {
            return currentDepthFrame.depthImage;
        }
    
        public override RATKinectSkeleton GetKinectSkeleton(int n)
        {
            if (currentSkeletonFrame == null)
                return null;
            else if (n >= 0 && n < 6)
                return currentSkeletonFrame.skeletons[n];
            else
                return null;
        }

        public List<RATKinectSkeleton> GetKinectSkeletons()
        {
            return currentSkeletonFrame.skeletons;
        }

        public override int GetMaxBodiesCount()
        {
            return 6;
        }

        public void OnApplicationQuit()
        {
            Debug.Log("RATKinectClient Closing: " + serverIPAddress);

            inited = false;
            runThreads = false;

            if (depthClient != null)
            {
                depthClient.ReceivedMessage -= RemoteComputer_NewDepthFrame;
                depthClient.Close();
            }
            if (colorClient != null)
            {
                colorClient.ReceivedMessage -= RemoteComputer_NewColorFrame;
                colorClient.Close();
            }
            if (configurationClient != null)
            {
                configurationClient.ReceivedMessage -= RemoteComputer_NewConfigurationFrame;
                configurationClient.Close();
            }
            if (audioClient != null)
            {
                audioClient.ReceivedMessage -= RemoteComputer_NewAudioFrame;
                audioClient.Close();
            }
            if (skeletonClient != null)
            {
                skeletonClient.ReceivedMessage -= RemoteComputer_NewSkeletonFrame;
                skeletonClient.Close();
            }

            if (writeAudioToWav && !StreamFromFile) 
            {
                // We write the streamed audio to WAV here only if we are streaming from a remote unit,
                // rather than looping previously streamed and serialized data
                _WriteAudioToWav();
            }

            // Make sure all Kinect data streams are closed
            if (depthStream != null)
                depthStream.Close();
            if (rgbStream != null)
                rgbStream.Close();
            if (audioStream != null)
                audioStream.Close();
            if (skeletonStream != null)
                skeletonStream.Close();

            // Print how many frames were received and rendered
            _PrintNumFrames();
        }

        private void RemoteComputer_NewDepthFrame(object state, ReceivedMessageEventArgs args)
        {
            
            MemoryStream memStream = new MemoryStream(args.data);
            BinaryReader reader = new BinaryReader(memStream);

            long timeStampDepth;
            byte[] depth;

            //Debug.Log(string.Format("Server received message {0} of size {1}", messageType, args.data.Length));
            timeStampDepth = reader.ReadInt64();
            //int messageType;
            reader.ReadInt32(); //messageType
            depth = reader.ReadBytes(2 * depthWidth * depthHeight);        

            lock (nextDepthFrame)
            {
                nextDepthFrame.timeStampDepth = timeStampDepth;
                nextDepthFrame.depthData = depth;

                Marshal.Copy(depth, 0, nextDepthFrame.depthImage.DataIntPtr, depth.Length);

                // Depth data serialization
                ++numFramesReceived;
                if (StreamToFile) //if (mProCamUnit.SerializeKinectData)
                {
                    // Serialize depth frame to file
                    formatter.Serialize(depthStream, nextDepthFrame);
                    ++numFramesSerialized;
                }
            }

            nextDepthFrameReady.Set();
            reader.Close();
        }

        //byte[] incomingRGBBuffer;
        private void RemoteComputer_NewColorFrame(object state, ReceivedMessageEventArgs args)
        {
            //DateTime now = DateTime.Now;
            MemoryStream memStream = new MemoryStream(args.data);
            BinaryReader reader = new BinaryReader(memStream);


            long timeStamp = reader.ReadInt64();
            reader.ReadInt32(); //type
            lock (nextRGBFrame)
            {
                //incomingRGBBuffer = reader.ReadBytes(args.data.Length - 12);
                nextRGBFrame.rgbData = reader.ReadBytes(args.data.Length - 12);
                nextRGBFrame.length = args.data.Length - 12;
                nextRGBFrame.timeStampRGB = timeStamp;

                numRGBFramesReceived++;

                if (StreamToFile) //if (mProCamUnit.SerializeKinectData)
                {
                    // Serialize depth frame to file
                    formatter.Serialize(rgbStream, nextRGBFrame);
                    ++numRGBFramesSerialized;
                }
            }
            nextColorFrameReady.Set();

            reader.Close();
        }

        private void RemoteComputer_NewFlowFrame(object sender, ReceivedMessageEventArgs e)
        {
            Debug.LogError("Someone tried to call RemoteComputer_NewFlowFrame: Not Implemented Yet!");
            throw new NotImplementedException();
        }

        private void RemoteComputer_NewIRFrame(object sender, ReceivedMessageEventArgs e)
        {
            Debug.LogError("Someone tried to call RemoteComputer_NewIRFrame: Not Implemented Yet!");
            throw new NotImplementedException();
        }

        private void RemoteComputer_NewConfigurationFrame(object sender, ReceivedMessageEventArgs e)
        {
            //protocol is: 
            //1) request Kinect2Calibration (command 2)
            //2) request DepthSpaceToCameraSpaceTable (command 1)
            //3) request to terminate the connection (command 0) - important otherwise server keeps the connection open even when the process terminates

            if (e.data.Length > 1)
            {
                byte type = e.data[0];
                Debug.Log("Received message from configuration server. Type: " + type + " Size " + e.data.Length + " bytes");
                if (type == (byte)1)
                {
                    DepthSpaceToCameraSpaceTable.FromByteArray(e.data,1);
                    depthToCameraSpaceTableUpdateCount++;

                    if (StreamToFile) //record this info for playback
                    {
                        Debug.Log("Saving DepthSpaceToCameraSpaceTable to file: " + depthToCameraTablePath);
                        DepthSpaceToCameraSpaceTable.SaveToFile(depthToCameraTablePath);
                    }
                    
                    byte[] a = new byte[1];
                    a[0] = 0; //request that server terminates this connection
                    configurationClient.SendMessageToAllClients(a);
                }
                else if (type == (byte)2)
                {
                    try
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(ProjectorCameraEnsemble.Camera.Kinect2Calibration));
                        MemoryStream ms = new MemoryStream(e.data, 1, e.data.Length - 1);
                        ProjectorCameraEnsemble.Camera.Kinect2Calibration calib = (ProjectorCameraEnsemble.Camera.Kinect2Calibration)serializer.Deserialize(ms);
                        UpdateKinectCalibrationData(calib);
                        ms.Close();
                        calibrationLoaded = true;
                        byte[] a = new byte[1];
                        a[0] = 1; //request DepthSpaceToCameraSpaceTable
                        configurationClient.SendMessageToAllClients(a);

                        

                        if (StreamToFile) //record this information for playback
                        {
                            Debug.Log("Saving Kinect2Calibration to file: " + kinectCalibrationPath);
                            var writer = new StreamWriter(new FileStream(kinectCalibrationPath, FileMode.Create));
                            serializer.Serialize(writer, calibration);
                            writer.Close();
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.LogError("Unable to deserialize Kinect2Calibration: " + ex.Message);
                    }
                }
                else//unknown message 
                {
                    byte[] a = new byte[1];
                    a[0] = 0; //request that server terminates this connection
                    configurationClient.SendMessageToAllClients(a);
                }
            }
        }

        private void RemoteComputer_NewAudioFrame(object state, ReceivedMessageEventArgs args)
        {
            //DateTime now = DateTime.Now;
            MemoryStream memStream = new MemoryStream(args.data);
            BinaryReader reader = new BinaryReader(memStream);

            //Debug.Log(string.Format("Server received message {0} of size {1}", messageType, args.data.Length));

            lock (audioFrameQueue)
            {
                AudioFrame nextAudioFrame = new AudioFrame();
                nextAudioFrame.timeStampAudio = reader.ReadInt64();
                nextAudioFrame.audioData = reader.ReadBytes(AudioFrameSize);
                // TODO: server should never send no data, but if it does, there is this workaround with extra logging
                if (nextAudioFrame.audioData == null)
                {
                    nextAudioFrame.audioData = new byte[AudioFrameSize];
                    Debug.LogError("KinectV2Server sent an audio message with no data");
                }
                //

                // Audio data serialization
                if (StreamToFile) 
                {
                    // Write the audio frame to file
                    formatter.Serialize(audioStream, nextAudioFrame);
                }
                if (writeAudioToWav) 
                {
                    // Write it to WAV file
                    wavAudioStream.Write(nextAudioFrame.audioData, 0, nextAudioFrame.audioData.Length);
                }
                audioFrameQueue.Enqueue(nextAudioFrame);
            }

            nextAudioFrameReady.Set();

            reader.Close();
        }

        private void RemoteComputer_NewSkeletonFrame(object state, ReceivedMessageEventArgs args)
        {
            //DateTime now = DateTime.Now;
            MemoryStream memStream = new MemoryStream(args.data);
            BinaryReader reader = new BinaryReader(memStream);

            long timeStampSkeleton = reader.ReadInt64();
            validBodiesCount = reader.ReadByte();

            for (int cnt = 0; cnt < 6; cnt++)
            {
                RATKinectSkeleton skeleton = pendingSkeletonFrame.skeletons[cnt];
                
                if (cnt < validBodiesCount)
                {
                    skeleton.valid = true;
                    skeleton.ID = (ulong) reader.ReadInt64();
                    skeleton.jointPositions3D = new Vector3[25];

                    for (int i = 0; i < RATKinectSkeleton.JOINT_COUNT; i++)
                    {
                        var position = new Vector3();
                        position.x = -reader.ReadSingle();
                        position.y = reader.ReadSingle();
                        position.z = reader.ReadSingle();
                        skeleton.jointPositions3D[i] = position;

                        skeleton.jointStates[i] = (RATKinectSkeleton.TrackingState)reader.ReadByte();
                    }
                    skeleton.handLeftConfidence = reader.ReadByte();  //0=not confident, 1= confident
                    skeleton.handLeftState = reader.ReadByte();
                    skeleton.handRightConfidence = reader.ReadByte();
                    skeleton.handRightState = reader.ReadByte();

                    //face information
                    for(int i = 0; i<5; i++)
                    {
                        var facePos = new Vector3();
                        facePos.x = -reader.ReadSingle();
                        facePos.y = reader.ReadSingle();
                        facePos.z = reader.ReadSingle();
                        skeleton.facePositions3D[i] = facePos;
                    }
                    var faceYPR = new Vector3();
                    faceYPR.x = reader.ReadSingle();
                    faceYPR.y = reader.ReadSingle();
                    faceYPR.z = reader.ReadSingle();
                    skeleton.faceOrientationYPR = faceYPR;
                    skeleton.faceOrientation = Quaternion.Euler(faceYPR);
                    skeleton.glasses = (DetectionResult)reader.ReadByte();
                    skeleton.happy = (DetectionResult)reader.ReadByte();
                    skeleton.engaged = (DetectionResult)reader.ReadByte();
                    skeleton.lookingAway = (DetectionResult)reader.ReadByte();
                    skeleton.leftEyeClosed = (DetectionResult)reader.ReadByte();
                    skeleton.rightEyeClosed = (DetectionResult)reader.ReadByte();
                    skeleton.mouthOpen = (DetectionResult)reader.ReadByte();
                    skeleton.mouthMoved = (DetectionResult)reader.ReadByte(); 
                }
                else
                {
                    skeleton.valid = false;
                }
            }
            

            //accelerometer reading
            float x, y, z, w;
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            w = reader.ReadSingle();
            pendingSkeletonFrame.deviceAcceleration = KinectToWorldNoT(new Vector3(x / w, y / w, z / w)); //mProCamUnit.KinectToWorldNoT(new Vector3(x / w, y / w, z / w));
            pendingSkeletonFrame.timeStampSkeleton = timeStampSkeleton;

            reader.Close();

            lock (nextSkeletonFrame)
            {
                Swap<KinectSkeletonFrame>(ref nextSkeletonFrame, ref pendingSkeletonFrame);

                // Skeletal data serialization
                if (StreamToFile)
                {
                    // Serialize skeleton frame to file
                    formatter.Serialize(skeletonStream, nextSkeletonFrame);
                }
            }

            nextSkeletonFrameReady.Set();

        }

        private void _PlayAudioFrame16(byte[] data)
        {
            for (int sampleIndex = 0; sampleIndex < data.Length; sampleIndex += 2)
            {
                audioPlaybackBuffer.Enqueue(100f * BitConverter.ToInt16(data, sampleIndex) / ((float)Int16.MaxValue)); //100x is just to bring the volume up to reasonable level
            }
            //Debug.Log("AUDIO ENCODE <<< Incomming=" + data.Length / 2 + " AudioBuffer=" + audioPlaybackBuffer.Count);
        }

        private void _OnAudioRead(float[] data)
        {
            audioPlaybackBufferUnderrun = audioPlaybackBuffer == null || data.Length > audioPlaybackBuffer.Count;

            if (audioPlaybackBuffer != null)
            {
                for (int sampleIndex = 0; sampleIndex < data.Length; sampleIndex++)
                {
                    data[sampleIndex] = (audioPlaybackBuffer.Count <= 0) ? 0f : audioPlaybackBuffer.Dequeue();
                   
                }

                //Debug.Log("AUDIO DECODE >>> Playing = " + data.Length + " AudioBuffer=" + audioPlaybackBuffer.Count);
            }
        }

        private void _WriteAudioToWav()
        {
            FileStream wavFileStream = new FileStream(audioPath + ".wav", FileMode.Create);
            byte[] data = wavAudioStream.GetBuffer();

            // Write RIFF header
            byte[] str = Encoding.ASCII.GetBytes("RIFF");
            wavFileStream.Write(str, 0, str.Length);
            wavFileStream.Write(BitConverter.GetBytes((int)(36 + 8 + data.Length)), 0, 4);
            str = Encoding.ASCII.GetBytes("WAVE");
            wavFileStream.Write(str, 0, str.Length);

            // Write fmt chunk
            str = Encoding.ASCII.GetBytes("fmt ");
            wavFileStream.Write(str, 0, str.Length);
            wavFileStream.Write(BitConverter.GetBytes((int)16), 0, 4);
            wavFileStream.Write(BitConverter.GetBytes((short)1), 0, 2);
            wavFileStream.Write(BitConverter.GetBytes((short)1), 0, 2);
            wavFileStream.Write(BitConverter.GetBytes((int)AudioSamplesPerSecond), 0, 4);
            wavFileStream.Write(BitConverter.GetBytes((int)(AudioSamplesPerSecond * AudioBitsPerSample / 8)), 0, 4);
            wavFileStream.Write(BitConverter.GetBytes((short)(AudioBitsPerSample / 8)), 0, 2);
            wavFileStream.Write(BitConverter.GetBytes((short)AudioBitsPerSample), 0, 2);

            // Write data chunk
            str = Encoding.ASCII.GetBytes("data");
            wavFileStream.Write(str, 0, str.Length);
            wavFileStream.Write(BitConverter.GetBytes((int)data.Length), 0, 4);
            wavFileStream.Write(data, 0, data.Length);
            wavFileStream.Close();
        }

        private void _PrintNumFrames()
        {
            Debug.Log(string.Format("RATKinectClient {0}: received {1} depth frames, rendered {2}",
                        serverIPAddress, numFramesReceived, numFramesRendered));
            Debug.Log(string.Format("RATKinectClient {0}: received {1} color frames, rendered {2}",
                serverIPAddress, numRGBFramesReceived, numRGBFramesRendered));
        }


        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public ushort GetDepthAt(int x, int y)
        {
            lock (nextDepthFrame)
            {
                return currentDepthFrame.depthImage[x, y];
            }
        }
        public byte GetValidBodyCount()
        {
            lock (nextSkeletonFrame)
            {
                return currentSkeletonFrame.validBodiesCount;
            }
        }

        public unsafe Vector3 GetAccReading()
        {
            return new Vector3(currentSkeletonFrame.deviceAcceleration.x,
                currentSkeletonFrame.deviceAcceleration.y,
                currentSkeletonFrame.deviceAcceleration.z);
        }

        #region Transform Conversions
        //Helper function which converts a position in the Kinect coordinate space to the world coordinates
        public override Vector3 KinectToWorld(Vector3 pos)
        {
            Vector4 headPos4W = localToWorldMatrix * new Vector4(-pos.x, pos.y, pos.z, 1.0f);  //also converts it from RH to LH
            return new Vector3(headPos4W.x / headPos4W.w, headPos4W.y / headPos4W.w, headPos4W.z / headPos4W.w);
        }


        //Helper function which converts a position in the Kinect coordinate space to the world coordinates
        //reset translation (used for accelerations and velocities)
        public Vector3 KinectToWorldNoT(Vector3 pos)
        {
            Matrix4x4 tmp = localToWorldMatrix; 
            tmp.m30 = tmp.m31 = tmp.m32 = 0; //set translation to 0
            tmp.m03 = tmp.m13 = tmp.m23 = 0; //set translation to 0
            Vector4 headPos4W = tmp * new Vector4(-pos.x, pos.y, pos.z, 1.0f);
            return new Vector3(headPos4W.x / headPos4W.w, headPos4W.y / headPos4W.w, headPos4W.z / headPos4W.w);
        }

        //Helper function which converts a position in world coordinates to the Kinect coordinate space
        public Vector3 WorldToKinect(Vector3 pos)
        {
            Vector4 headPos4 = localToWorldMatrix * new Vector4(pos.x, pos.y, pos.z, 1.0f); //also converts it from RH to LH
            return new Vector3(-headPos4.x / headPos4.w, headPos4.y / headPos4.w, headPos4.z / headPos4.w);
        }

        //Helper function which converts a position in world coordinates to the Kinect coordinate space
        //reset translation (used for accelerations and velocities)
        public Vector3 WorldToKinectNoT(Vector3 pos)
        {
            Matrix4x4 tmp = localToWorldMatrix;
            tmp.m30 = tmp.m31 = tmp.m32 = 0; //set translation to 0
            tmp.m03 = tmp.m13 = tmp.m23 = 0; //set translation to 0
            Vector4 headPos4 = tmp * new Vector4(pos.x, pos.y, pos.z, 1.0f);
            return new Vector3(-headPos4.x / headPos4.w, headPos4.y / headPos4.w, headPos4.z / headPos4.w);  //also converts it from LH to RH
        }

        Quaternion GetLocalRotation()
        {
            Matrix4x4 m = transform.localToWorldMatrix;
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
            return q;
        }
        #endregion
    }



    #region Serialization Helper stuff
    [Serializable]
    public class KinectDepthFrame
    {
        public KinectDepthFrame(int depthImageWidth, int depthImageHeight)
        {
            depthImage = new ShortImage(depthImageWidth, depthImageHeight);

        }

        public long timeStampDepth;
        public byte[] depthData;


        [NonSerializedAttribute]
        public ShortImage depthImage;

    }

    [Serializable]
    public class KinectRGBFrame
    {
        public KinectRGBFrame(int colorImageWidth, int colorImageHeight)
        {
            rgbData = new byte[colorImageHeight * colorImageWidth*4];
            length = colorImageHeight * colorImageWidth * 4; //default
        }

        public long timeStampRGB;
        public int type; 
        public byte[] rgbData;
        public int length;    
    }

    [Serializable]
    public class AudioFrame
    {
        public AudioFrame()
        {
        }

        public long timeStampAudio;
        public byte[] audioData;
    }



    [Serializable]
    public class KinectSkeletonFrame
    {
        public KinectSkeletonFrame()
        {
            skeletons = new List<RATKinectSkeleton>(6);
            for (int i = 0; i < 6; i++) skeletons.Add(new RATKinectSkeleton());
        }

        public long timeStampSkeleton;
        public byte validBodiesCount;
        public List<RATKinectSkeleton> skeletons;

        [NonSerializedAttribute]
        public Vector3 deviceAcceleration = new Vector3();
        [NonSerializedAttribute]
        public bool flipped = false;
    }

    #endregion

}