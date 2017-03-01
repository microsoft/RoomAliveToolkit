using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoomAliveToolkit
{
    /// <summary>
    /// Behavior that shows playback controlls in the editor view for controlling the replay of previously recorded Kinect streams from files. 
    /// </summary>
    [AddComponentMenu("RoomAliveToolkit/RATKinectPlaybackController")]
    public class RATKinectPlaybackController : MonoBehaviour
    {
        public List<RATKinectClient> clients = new List<RATKinectClient>(); //Searches in children if empty

        public FileStreamingMode streamingMode = FileStreamingMode.None;

        public bool startOnAwake = false;
        public bool loopPlayback = false;

        public string streamFolder = null; //Leave empty to keep individual client filenames
        public bool trimProCamPlayback;
        public int depthFramesTrimFromStart;
        public int depthFramesTrimFromEnd;

        /// <summary>
        /// Time (in seconds) to delay the playback of RGB stream. This variable can be used when playback of Kinect data does not exactly match between depth and RGB data. 
        /// </summary>
        public float RGBDelay = 0;

        public bool writeAudioToWav; 

        public float AudioDelay;

        public bool running { get; private set; }
        public float playbackPos { get; private set; }

        private bool awake = false;
        public bool endOfStream { get; private set; }

        void Awake()
        {
            endOfStream = false;
            awake = false;
            ConnectToKinectClients();
        }

        public void ConnectToKinectClients()
        {
            clients.Clear();

            foreach (RATKinectClient childClient in GetComponentsInChildren<RATKinectClient>())
            {
                clients.Add(childClient);
                childClient.playbackController = this;
            }

            if(clients.Count == 0)
            {
                Debug.LogError("RATKinectPlaybackController should always be added to the existing RATKinectClient node or a parent of the node that contains some valid RATKinectClients!");
            }
        }

        public void UpdateWritingToWAV()
        {
            foreach (RATKinectClient c in clients)
            {
                c.writeAudioToWav = writeAudioToWav;
            }
        }
        void Start()
        {
        }

        public void OnApplicationQuit()
        {
            Pause();
        }

        public void Restart()
        {
            playbackPos = 0;

            running = true;
            endOfStream = false;
            foreach (RATKinectClient client in clients)
                if (client.isActiveAndEnabled)
                    client.SignalRestart();
        }

        public void OnStreamRestarted()
        {

        }

        public void StartStream()
        {
            running = true;
            endOfStream = false;
            foreach (RATKinectClient client in clients)
            {
                AudioSource source = client.audioSource;
                if (source!=null)
                {
                    source.PlayDelayed(AudioDelay);
                    source.playOnAwake =  startOnAwake;
                    source.loop = loopPlayback;
                }
                
            }
                    
        }

        bool pauseRequested;

        public void Pause()
        {
            running = false;
            pauseRequested = true;
        }

        void Update()
        {
            if (!awake)
            {
                awake = true;
                if (startOnAwake)
                    StartStream();
            }

            if (pauseRequested)
            {
                foreach (RATKinectClient client in clients)
                {
                    if(client.audioSource!=null)
                        client.audioSource.Pause();
                }
                    
            }
                
            if (running)
                playbackPos += Time.deltaTime;
        }

        void OnValidate()
        {
            if (!streamFolder.Equals(""))
            {
                if (!streamFolder.EndsWith("/") && !streamFolder.EndsWith("\\"))
                    streamFolder += "/";
            }
        }

        internal void SetEndOfStream()
        {
            running = false;
            endOfStream = true;
        }
    }
}