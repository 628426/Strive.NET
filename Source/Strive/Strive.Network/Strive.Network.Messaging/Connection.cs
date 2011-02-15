﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;
using UpdateControls;
using Common.Logging;
using Strive.Network.Messages;


namespace Strive.Network.Messaging
{
    public enum ConnectionStatus
    {
        Connecting,
        Connected,
        Disconnected
    }

    public class MessageEventArgs : EventArgs
    {
        public IMessage Message { get; private set; }
        public MessageEventArgs(IMessage message)
        {
            Message = message;
        }
    }

    public class Connection
    {
        private ConnectionStatus _status;

        // TODO: does updatecontrols belong in here?
        #region Independent properties
        // Generated by Update Controls --------------------------------
        private readonly Independent _indStatus = new Independent();

        public ConnectionStatus Status
        {
            get { _indStatus.OnGet(); return _status; }
            private set { _indStatus.OnSet(); _status = value; }
        }
        // End generated code --------------------------------
        #endregion

        BlockingCollection<IMessage> _messageInQueue;
        BlockingCollection<IMessage> _messageOutQueue;

        protected Socket TcpSocket;
        readonly byte[] _tcpbuffer = new byte[MessageTypeMap.BufferSize];  // Receive buffer.
        int _tcpOffset;

        public event EventHandler Connect;
        public event EventHandler ConnectFailed;
        public event EventHandler Disconnect;
        public event EventHandler MessageRecieved;

        protected ILog Log;
        public Connection()
        {
            Status = ConnectionStatus.Disconnected;
            Log = LogManager.GetCurrentClassLogger();
        }

        public void Start(Socket socket)
        {
            lock (this)
            {
                TcpSocket = socket;
                _tcpOffset = 0;
                Status = ConnectionStatus.Connected;
                _messageInQueue = new BlockingCollection<IMessage>();
                _messageOutQueue = new BlockingCollection<IMessage>();
                BeginReading();
                (new Thread(BeginSending)).Start();
            }
        }

        public void Start(IPEndPoint remoteEndPoint)
        {
            lock (this)
            {
                if (TcpSocket != null)
                {
                    Stop();
                    // TODO: fix the swapping and don't sleep hax
                    Thread.Sleep(100);
                }

                // Connect to the remote endpoint.
                TcpSocket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    TcpSocket.BeginConnect(remoteEndPoint, ConnectTcpCallback, this);
                }
                catch (SocketException se)
                {
                    Log.Error("Error connecting, closing connection.", se);
                    Stop();
                    if (ConnectFailed != null)
                        ConnectFailed(this, null);
                    return;
                }

                Status = ConnectionStatus.Connecting;
            }
        }

        public void Stop()
        {
            lock (this)
            {
                if (_messageOutQueue != null)
                    _messageInQueue.CompleteAdding();
                if (_messageOutQueue != null)
                    _messageOutQueue.CompleteAdding();

                if (TcpSocket != null && Status != ConnectionStatus.Disconnected)
                {
                    try
                    {
                        TcpSocket.Shutdown(SocketShutdown.Both);
                        TcpSocket.Close();
                    }
                    catch (SocketException se)
                    {
                        Log.Error("Error shutting down", se);
                    }
                }
                TcpSocket = null;

                if (Status == ConnectionStatus.Connected && Disconnect != null)
                    Disconnect(this, null);
                Status = ConnectionStatus.Disconnected;

                _messageInQueue = null;
                _messageOutQueue = null;
            }
        }

        private static void ConnectTcpCallback(IAsyncResult ar)
        {
            var client = (Connection)ar.AsyncState;
            lock (client)
            {
                try
                {
                    // Complete the connection.
                    client.TcpSocket.EndConnect(ar);
                }
                catch (SocketException se)
                {
                    client.Log.Error("Error connecting, closing connection.", se);
                    client.Stop();
                    if (client.ConnectFailed != null)
                        client.ConnectFailed(client, null);
                    return;
                }

                // Start reading/writting
                client.Start(client.TcpSocket);

                client.Log.Info("Connected to " + client.TcpSocket.RemoteEndPoint);
                if (client.Connect != null)
                    client.Connect(client, null);
            }
        }

        private IAsyncResult _expectingReadResult;
        private void BeginReading()
        {
            lock (this)
            {
                // Begin reading
                try
                {
                    _expectingReadResult = TcpSocket.BeginReceive(
                        _tcpbuffer, 0, MessageTypeMap.BufferSize, 0,
                        new AsyncCallback(ReceiveTcpCallback), this);
                }
                catch (SocketException e)
                {
                    Log.Error("Error receiving, closing connection.", e);
                    Stop();
                    return;
                }
            }
        }

        private static void ReceiveTcpCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket 
            // from the async state object.
            var client = (Connection)ar.AsyncState;
            lock (client)
            {
                if (client.TcpSocket == null)
                    return;

                if (ar != client._expectingReadResult)
                {
                    // this is the result of a previous connection, we discard it
                    return;
                }

                int bytesRead;
                try
                {
                    // Read data from the remote device.
                    bytesRead = client.TcpSocket.EndReceive(ar);
                }
                catch (SocketException e)
                {
                    client.Log.Error("Error reading, closing connection.", e);
                    client.Stop();
                    return;
                }

                if (bytesRead == 0)
                {
                    client.Log.Info("Connection closed by remote.");
                    client.Stop();
                    return;
                }

                client._tcpOffset += bytesRead;
                while (client._tcpOffset > MessageTypeMap.MessageLengthLength)
                {
                    int expectedLength = BitConverter.ToInt32(client._tcpbuffer, 0);

                    if (client._tcpOffset >= expectedLength)
                    {
                        IMessage message;
                        try
                        {
                            message = (IMessage)CustomFormatter.Deserialize(client._tcpbuffer, MessageTypeMap.MessageLengthLength);
                        }
                        catch (Exception e)
                        {
                            client.Log.Error("Invalid packet received, closing connection.", e);
                            client.Stop();
                            return;
                        }
                        if (client._messageInQueue.TryAdd(message))
                        {
                            client.Log.Trace("enqueued " + message.GetType() + " message");
                            if (client.MessageRecieved != null) client.MessageRecieved(client, new MessageEventArgs(message));
                        }
                        else
                        {
                            client.Log.Error("Failed to enqueue " + message.GetType() + " message");
                        }

                        // copy the remaining data to the front of the buffer
                        client._tcpOffset -= expectedLength;
                        if (client._tcpOffset > 0)
                            Array.Copy(client._tcpbuffer, expectedLength, client._tcpbuffer, 0, client._tcpOffset);
                    }
                }

                // listen for the next message
                client.BeginReading();
            }
        }

        public virtual bool Send(IMessage message)
        {
            lock (this)
            {
                if (Status != ConnectionStatus.Connected)
                {
                    Log.Trace("Not connected, cannot send " + message.GetType() + " message");
                    return false;
                }
                if (!_messageOutQueue.TryAdd(message))
                {
                    Log.Error("Failed to enqueue " + message.GetType() + " message");
                    return false;
                }
                return true;
            }
        }

        private IAsyncResult _expectingSendResult;
        private void BeginSending()
        {
            IMessage message;
            if (!_messageOutQueue.TryTake(out message, -1))
                return;

            byte[] buffer;
            try
            {
                // Custom serialization
                buffer = CustomFormatter.Serialize(message);
            }
            catch (Exception e)
            {
                Log.Error("Invalid message to serialize, closing connection.", e);
                Stop();
                return;
            }

            lock (this)
            {

                if (TcpSocket == null || !TcpSocket.Connected)
                {
                    Log.Error("Tried to send message " + message + " while not connected.");
                    return;
                }

                try
                {
                    _expectingSendResult = TcpSocket.BeginSend(
                        buffer, 0, buffer.Length, 0,
                        new AsyncCallback(SendTcpCallback), this);
                }
                catch (SocketException se)
                {
                    Log.Error("Error sending, closing connection", se);
                    Stop();
                    return;
                }
            }
        }

        private static void SendTcpCallback(IAsyncResult ar)
        {
            var client = (Connection)ar.AsyncState;
            lock (client)
            {
                if (client.TcpSocket == null)
                {
                    return;
                }

                if (ar != client._expectingSendResult)
                {
                    // this is the result of a previous connection, we discard it
                    return;
                }

                try
                {
                    // Complete sending the data to the remote device.
                    client.TcpSocket.EndSend(ar);
                }
                catch (SocketException se)
                {
                    client.Log.Error("Error while sending, closing connection", se);
                    client.Stop();
                }
            }

            // Send the next message
            client.BeginSending();
        }

        public IMessage PopNextMessage()
        {
            lock (this)
            {
                IMessage message;
                if (!_messageInQueue.TryTake(out message))
                    Log.Info("In queue of messages exhausted");
                return message;
            }
        }

        public int MessageCount
        {
            get
            {
                lock (this)
                {
                    return _messageInQueue == null ? 0 : _messageInQueue.Count;
                }
            }
        }
    }
}