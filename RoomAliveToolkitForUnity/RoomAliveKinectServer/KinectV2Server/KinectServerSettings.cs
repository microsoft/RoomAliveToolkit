using System;
using System.IO;

using System.Xml.Serialization;

namespace KinectV2Server
{
    public class KinectServerSettings
    {
        public bool StreamColor = true;
        public bool StreamAudio = true;
        public bool ProcessColorRAW = false;
        public bool FlipImages = false;
        public bool BlurDepthImages = false;
        public bool ComputeFlow = false;

        public bool RenderFlow = false;
        public bool RenderSkeleton = true;
        public bool RenderFaces = true;

        public ushort ThresholdNoise = 250;

        public enum ColorCompressionType { NONE, JPEG}
        public ColorCompressionType colorCompression = ColorCompressionType.JPEG;

        public enum StreamType { Foreground, All, BodyIndex }
        public StreamType streamType = StreamType.Foreground; //default

        public int depthPort = 10010;
        public int colorPort = 10011;
        public int audioPort = 10004;
        public int skeletonPort = 10005;
        public int infraredPort = 10008;
        public int configurationPort = 10009;
        public int flowPort = 10007;

        public static KinectServerSettings Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                var settings = new KinectServerSettings();
                settings.Save(fileName);
                return settings;
            }
            else
            {
                FileStream fs = null;
                KinectServerSettings settings = null;
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(KinectServerSettings));
                    fs = new FileStream(fileName, FileMode.Open);
                    settings = (KinectServerSettings)serializer.Deserialize(fs);
                    fs.Close();
                    Console.WriteLine("KinectServer settings read from file: " + fileName);
                }
                catch (Exception e)
                {
                    if (fs != null)
                        fs.Close();
                    Console.WriteLine("Error loading settings file: " + e.Message);
                    Console.WriteLine("Using default values.");
                    return new KinectServerSettings();
                }

                return settings;
            }
        }

        public void Save(string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(KinectServerSettings));
            TextWriter writer = new StreamWriter(fileName);
            serializer.Serialize(writer, this);
            writer.Close();
            Console.WriteLine("KinectServer settings written to file: " + fileName);
        }
    }


}
