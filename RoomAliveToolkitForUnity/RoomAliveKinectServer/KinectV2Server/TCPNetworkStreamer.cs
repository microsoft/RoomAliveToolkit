//#define VERBOSE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace KinectV2Server
{
    // State object for reading client data asynchronously
    public class ClientState
    {
        private static int counter = 1;

        public ClientState()
        {
            ID = counter;
            counter++;
            receivingMessageBuffer = new byte[MessageSize];
        }

        public TcpClient client = null;
        public NetworkStream stream = null;

        public int ID = 0; //counter of clients (each is unique)
        public int MessageSize = 10000000;
        public bool readyToSend = true;

        public bool active = false;

        // Receiving buffers:
        public byte[] receivingMessageBuffer;
        public const int maximumMessageQueueLength = 50;
        public Queue<byte[]> sendingMessageQueue = new Queue<byte[]>(maximumMessageQueueLength);

        public int BytesReceived = 0;
        public int PacketCounter = 0;

        public void ResetCounters()
        {
            BytesReceived = 0;
            PacketCounter = 0;
        }
    }

    public interface INetworkCallback
    {
        void OnPrint(string message);
        void OnError(string message);
    }

    public class ReceivedMessageEventArgs : EventArgs
    {
        public ReceivedMessageEventArgs(byte[] _data)
        {
            this.data = _data;

        }

        public byte[] data;
    }

    public class TCPNetworkStreamer
    {
        private int server_port = 11000;
        private const int bufferSize = 10240000;
        public int tmpBufferSize = 640000;
        public int Port { get { return server_port; } }

        public INetworkCallback callback = null;

        //public bool CompressStream = false;
        private bool isServer = false;

        public bool IsServer { get { return isServer; } }

        public bool ReadyToSend
        {
            get
            {
                bool ready = (clients.Count > 0) ? true : false;
                foreach (ClientState client in clients)
                {
                    if (!client.readyToSend) ready = false;
                }
                return ready;
            }
        }

        private TcpListener server;
        private List<ClientState> clients = new List<ClientState>();

        public bool runningServer = false;

        public bool Connected { get { return clients.Count > 0; } }

        // Thread signal.
        public ManualResetEvent allDoneServer = new ManualResetEvent(false);
        public ManualResetEvent allDoneClient = new ManualResetEvent(false);

        // Sending queue:
        public delegate void ReceivedMessageEventHandler(object sender, ReceivedMessageEventArgs e);
        public ReceivedMessageEventHandler ReceivedMessage;

        public string name = "";

        public TCPNetworkStreamer()
        {
        }

        public TCPNetworkStreamer(bool _isServer, int _port)
        {
            isServer = _isServer;
            server_port = _port;
            if (isServer)
            {
                runningServer = true;
                Thread listenerT = new Thread(StartServer);
                listenerT.Start();
            }
        }

        public TCPNetworkStreamer(bool _isServer, int _port, string _name)
        {
            name = _name;
            isServer = _isServer;
            server_port = _port;
            if (isServer)
            {
                runningServer = true;
                Thread listenerT = new Thread(StartServer);
                listenerT.Start();
            }
        }

        public void Close()
        {
            runningServer = false;
            allDoneServer.Set();
            allDoneClient.Set();
            foreach (ClientState cs in clients)
            {
                cs.active = false;
            }
        }

        protected void Print(string message)
        {
            if(callback!=null)
            {
                callback.OnPrint(message);
            }else
            {
                Console.WriteLine(message);
            }
        }

        protected void PrintError(string message)
        {
            if (callback != null)
            {
                callback.OnError(message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        #region SERVER SPECIFIC
        private void StartServer()
        {
            try
            {
                // Get our own IP address for display
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                string localIP = "localhost";
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
                //Console.WriteLine("IP: " + localIP);

                IPEndPoint listenEP = new IPEndPoint(IPAddress.Any, server_port);
                server = new TcpListener(listenEP);
                // Start listening for client requests.
                server.Start();
                Print(name + " Server started " + localIP + ":" + server_port);
                while (runningServer)
                {
                    // Set the event to nonsignaled state.
                    allDoneServer.Reset();
                    server.BeginAcceptTcpClient(new AsyncCallback(ClientHandlerServerSide), server);
                    // Wait until a connection is made before continuing.
                    allDoneServer.WaitOne();
                }
            }
            catch (SocketException e)
            {
                PrintError(name + " Server SocketException: "+e);
            }
            finally
            {
                // Stop listening for new clients.
                runningServer = false;
                Print(name + " Server stopped!");
                server.Stop();
            }
        }

        private void ClientHandlerServerSide(IAsyncResult ar)
        {
            TcpListener server = (TcpListener)ar.AsyncState;

            ClientState clientState = new ClientState();
            clients.Add(clientState);
            try
            {
                clientState.client = server.EndAcceptTcpClient(ar);
                clientState.client.ReceiveBufferSize = bufferSize;
                clientState.client.SendBufferSize = bufferSize;
                clientState.stream = clientState.client.GetStream();
                allDoneServer.Set();

                Print(name + " Server connected new client: " + clientState.client.Client.RemoteEndPoint);

                RunClient(clientState);
            }
            catch (ObjectDisposedException )
            {
                PrintError(name + " Server must have closed. Aborting waiting for clients!");
            }
        }
        #endregion SERVER SPECIFIC

        #region CLIENT SPECIFIC

        public void ConnectToSever(string host, int port)
        {
            if (!IsServer)
            {
                // Connect asynchronously to the specifed host.
                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                IPHostEntry ipHostInfo = Dns.GetHostEntry(host); //localhost
                IPAddress ipAddress = ipHostInfo.AddressList.Where(a => a.AddressFamily == AddressFamily.InterNetwork).First(); // OrDefault();
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

                Print("Establishing Connection to "+ host + " - "+ ipAddress + ":"+ port);
                try
                {
                    client.BeginConnect(ipAddress, port, new AsyncCallback(ClientHandlerClientSide), client);

                }
                catch (Exception e)
                {
                    PrintError("Connecting to server failed: " + e.Message);
                }
            }
            else
            {
                PrintError("ConnectToServer Error! This TCPNetworkStreamer is configured to run as a server!");
            }
        }

        private void ClientHandlerClientSide(IAsyncResult ar)
        {
            try
            {
                ClientState clientState = new ClientState();
                clientState.client = (TcpClient)ar.AsyncState;
                clientState.client.ReceiveBufferSize = bufferSize;
                clientState.client.SendBufferSize = bufferSize;
                clientState.client.EndConnect(ar);
                clientState.stream = clientState.client.GetStream(); // Get a client stream for reading and writing.

                clients.Add(clientState);

                Print("Connection established!");

                RunClient(clientState);
            }
            catch (Exception e)
            {
                PrintError("Connecting to server failed: " + e.Message);
            }
        }

        /// <summary>
        /// ClientThread (same for server and client)
        /// </summary>
        /// <param name="clientState"></param>
        private void RunClient(ClientState clientState)
        {
            clientState.active = true;
            int bytesReceived = 0;
            byte[] bufferTmp = new byte[tmpBufferSize];

            // Loop to receive all the data sent by the client.
            try
            {
                while (clientState.client.Connected && clientState.active)
                {
                    //both reading and writing to a network stream can happen concurrently, so we will do this through asynchronous calls
                    //send to them whatever you need to send
                    if (clientState.readyToSend && clientState.stream.CanWrite && clientState.sendingMessageQueue.Count > 0)
                    {
                        byte[] sendBuffer;
                        lock (clientState.sendingMessageQueue)
                        {
                            sendBuffer = clientState.sendingMessageQueue.Dequeue();
                            
                            clientState.readyToSend = false;
                        }
                        // Send a message:
                        clientState.stream.BeginWrite(sendBuffer, 0, sendBuffer.Length, new AsyncCallback(SendCallback), clientState);
                    }

                    bytesReceived = 0;

                    //read from them whatever you need to read from them
                    while (clientState.stream.DataAvailable)
                    {
                        // Translate data bytes to a ASCII string.
                        bytesReceived = clientState.stream.Read(bufferTmp, 0, bufferTmp.Length);
                        ProcessReceivedBuffer(bufferTmp, bytesReceived, clientState);
                    }
                    Thread.Sleep(0);
                }
            }
            catch (Exception)
            {
                //PrintError("Client "+ clientState.ID + " Exception: Unable to write to socket (beginAsync): "+e);
            }

            Print("Client "+ clientState.ID + " disconnected.");

            // Shutdown and end connection
            clientState.stream.Close();
            clientState.client.Close();

            //should probably check if this one is in the list
            clients.Remove(clientState);
        }

        private void SendCallback(IAsyncResult ar)
        {
            var clientState = (ClientState)ar.AsyncState;

            try
            {
                clientState.stream.EndWrite(ar);
            }
            catch (Exception)
            {
                Print("Client "+ clientState.ID + " Exception: Unable to write to socket (endAsync).");
                //Console.WriteLine("Client {0} Exception: {1}", clientState.ID, e);
                clientState.active = false;
            }
            clientState.readyToSend = true;
#if VERBOSE
            Console.WriteLine("Sent message to client: " + clientState.client.Client.RemoteEndPoint);
#endif
        }

        //there is no guarrantee that the message will be transferred in one packet, so we need to assemble a buffer and keep track on where it is.
        private void ProcessReceivedBuffer(byte[] buffer, int bytesReceived, ClientState clientState)
        {

#if VERBOSE
            Console.WriteLine("Processing! This message: " + bytesReceived + " Received so far: " + clientState.BytesReceived);
#endif
            if (clientState.BytesReceived == 0 && bytesReceived > 3) //new message
            {
                int newMessageSize = BitConverter.ToInt32(buffer, 0) + sizeof(int); //prefix is one int
                if (newMessageSize != clientState.MessageSize)
                {
#if VERBOSE
                    Console.WriteLine("Resizing receiving buffer to: " + newMessageSize + " Old size: " + clientState.MessageSize + " Bytes received so far: " + clientState.BytesReceived);
#endif
                    clientState.MessageSize = newMessageSize;
                    byte[] newBuffer = new byte[newMessageSize];
                    clientState.receivingMessageBuffer = newBuffer;
                }
            }

            int availableLength = clientState.MessageSize - clientState.BytesReceived;
            int copyLen = Math.Min(availableLength, bytesReceived);
            clientState.PacketCounter++;
            Array.Copy(buffer, 0, clientState.receivingMessageBuffer, clientState.BytesReceived, copyLen);

            clientState.BytesReceived += copyLen;

            if (clientState.BytesReceived == clientState.MessageSize)
            {
#if VERBOSE
                Console.WriteLine("Received Message of {0} bytes from client #{1} in {2} messages", clientState.BytesReceived, clientState.ID, clientState.PacketCounter);
#endif
                clientState.ResetCounters();
                //let folks know that they have the full message
                if (ReceivedMessage != null)
                {
                    ReceivedMessage(clientState, new ReceivedMessageEventArgs(UnwrapMessage(clientState.receivingMessageBuffer)));
                }
            }

            if (copyLen != bytesReceived) // process the remainder of the message
            {
#if VERBOSE
                Console.WriteLine("Packet was longer than the expected. Received {0}  Copied {1}", bytesReceived, copyLen);
#endif
                byte[] newBuffer = new byte[bytesReceived - copyLen];

                Array.Copy(buffer, copyLen, newBuffer, 0, bytesReceived - copyLen);

                ProcessReceivedBuffer(newBuffer, bytesReceived - copyLen, clientState);
            }
        }
        #endregion CLIENT SPECIFIC

        public void CloseClient(int clientID)
        {
            foreach (ClientState state in clients)
            {
                if (state.ID == clientID)
                {
                    Print("Closing client "+ clientID + "...");
                    state.active = false;
                    
                }
            }
        }
        public void CloseAllClients()
        {
            foreach (ClientState state in clients)
            {
                Print(this.name+" Closing client "+ state.ID + "...");
                state.active = false;
            }
            clients.Clear();
        }

        public void SendMessageToAllClients(byte[] data)
        {
            byte[] dataWrapped = WrapMessage(data);
            foreach (ClientState state in clients)
            {
                lock (state.sendingMessageQueue)
                {
                    if (state.sendingMessageQueue.Count < ClientState.maximumMessageQueueLength)
                    {
                        state.sendingMessageQueue.Enqueue(dataWrapped);
                    }
                }
            }
        }

        public void SendMessageToClient(byte[] data, int clientID)
        {
            byte[] dataWrapped = WrapMessage(data);
            foreach (ClientState state in clients)
            {
                if (state.ID == clientID)
                {
                    lock (state.sendingMessageQueue)
                    {
                        if (state.sendingMessageQueue.Count < ClientState.maximumMessageQueueLength)
                        {
                            state.sendingMessageQueue.Enqueue(dataWrapped);
                        }
                    }
                }
            }
        }

        public int GetClientCount() { return clients.Count; }

        /// <summary>
        /// Wraps a message. The wrapped message is ready to send to a stream.
        /// </summary>
        /// <remarks>
        /// <para>Generates a length prefix for the message and returns the combined length prefix and message.</para>
        /// </remarks>
        /// <param name="message">The message to send.</param>
        public byte[] WrapMessage(byte[] message)
        {
            //if (CompressStream)
            //{
            //    MemoryStream stream1 = new MemoryStream();
            //    System.IO.Compression.DeflateStream defStream = new System.IO.Compression.DeflateStream(stream1, System.IO.Compression.CompressionMode.Compress);

            //    defStream.Write(message, 0, message.Length);
            //    message = stream1.ToArray();
            //    stream1.Close();
            //}

            // Get the length prefix for the message
            byte[] lengthPrefix = BitConverter.GetBytes(message.Length);
            // Concatenate the length prefix and the message
            byte[] ret = new byte[lengthPrefix.Length + message.Length];
            lengthPrefix.CopyTo(ret, 0);
            message.CopyTo(ret, lengthPrefix.Length);
            return ret;
        }

        public byte[] UnwrapMessage(byte[] message)
        {
            byte[] data = new byte[message.Length - sizeof(int)];

            //skip the prefix
            Array.Copy(message, sizeof(int), data, 0, data.Length);

            //if (CompressStream)
            //{
            //    MemoryStream original = new MemoryStream(data);
            //    System.IO.Compression.DeflateStream defStream = new System.IO.Compression.DeflateStream(original, System.IO.Compression.CompressionMode.Decompress);
            //    MemoryStream memStream = new MemoryStream();
            //    defStream.CopyTo(memStream);// Read(tmp, 0, tmp.Length);
            //    original.Close();
            //    data = memStream.ToArray();
            //    memStream.Close();
            //}
            return data;
        }
    }
}
