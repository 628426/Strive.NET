using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using Strive.Network.Messages;
using Strive.Network.Messages.ToServer;
using Strive.Logging;
using Strive.Math3D;

namespace Strive.Network.Client {
	public class ServerConnection {
		Queue messageQueue;
		IPEndPoint remoteEndPoint;
		IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
		Socket udpsocket;
		Socket tcpsocket;
		byte[] udpbuffer = new byte[MessageTypeMap.BufferSize];  // Receive buffer.
		byte[] tcpbuffer = new byte[MessageTypeMap.BufferSize];  // Receive buffer.
		bool connected = false;

		public delegate void OnConnectHandler();
		public delegate void OnDisconnectHandler();
		public event OnConnectHandler OnConnect;
		public event OnDisconnectHandler OnDisconnect;


		public ServerConnection() {
		}

		public class AlreadyRunningException : Exception{}
		public void Start( IPEndPoint remoteEndPoint ) {
			this.remoteEndPoint = remoteEndPoint;
			messageQueue = new Queue();

			// Connect to the remote endpoint.
			tcpsocket = new Socket(AddressFamily.InterNetwork,
				SocketType.Stream, ProtocolType.Tcp);
			udpsocket = new Socket(AddressFamily.InterNetwork,
				SocketType.Dgram, ProtocolType.Udp);
			tcpsocket.BeginConnect( remoteEndPoint, 
				new AsyncCallback(ConnectTCPCallback), this);
		}

		public void Stop() {
			if ( tcpsocket != null ) {
				tcpsocket.Close();
				tcpsocket = null;
			}
			if ( udpsocket != null ) {
				udpsocket.Close();
				udpsocket = null;
			}
			if ( connected ) {
				OnDisconnect();
				connected = false;
			}
		}

		private static void ConnectTCPCallback(IAsyncResult ar) {
			ServerConnection client = (ServerConnection) ar.AsyncState;
			try {

				// Complete the connection.
				client.tcpsocket.EndConnect(ar);
				client.connected = true;
				client.OnConnect();

				// Begin reading.
				client.tcpsocket.BeginReceive( client.tcpbuffer, 0, MessageTypeMap.BufferSize, 0,
					new AsyncCallback(ReceiveTCPCallback), client );

				client.udpsocket.Bind( client.tcpsocket.LocalEndPoint );
				client.udpsocket.BeginConnect( client.tcpsocket.RemoteEndPoint,
					new AsyncCallback(ConnectUDPCallback), client );
			} catch (Exception) {
				client.Stop();
			}
		}

		private static void ConnectUDPCallback(IAsyncResult ar) {
			ServerConnection client = (ServerConnection) ar.AsyncState;
			try {

				// Complete the connection.
				client.udpsocket.EndConnect(ar);

				// Begin reading.
				client.udpsocket.BeginReceive( client.udpbuffer, 0, MessageTypeMap.BufferSize, 0,
					new AsyncCallback(ReceiveUDPCallback), client );
			} catch (Exception) {
				client.Stop();
			}
		}

		private static void ReceiveTCPCallback( IAsyncResult ar ) {
			// Retrieve the state object and the client socket 
			// from the async state object.
			ServerConnection client = (ServerConnection) ar.AsyncState;
			try {
				// Read data from the remote device.
				int bytesRead = client.tcpsocket.EndReceive(ar);

				// There might be more data, so store the data received so far.
				IMessage message = (IMessage)CustomFormatter.Deserialize( client.tcpbuffer );
				client.messageQueue.Enqueue( message );
				//Log.LogMessage( "enqueued " + message.GetType() + " message" );

				// the next message
				client.tcpsocket.BeginReceive( client.tcpbuffer, 0, MessageTypeMap.BufferSize, 0,
					new AsyncCallback(ReceiveTCPCallback), client );
			} catch ( Exception ) {
				client.Stop();
			}
		}

		private static void ReceiveUDPCallback( IAsyncResult ar ) {
			// Retrieve the state object and the client socket 
			// from the async state object.
			ServerConnection client = (ServerConnection) ar.AsyncState;

			// TODO: is this wrong? shouldn't we endreceive?
			if ( client.udpsocket == null ) return;
			try {
				// Read data from the remote device.
				int bytesRead = client.udpsocket.EndReceive(ar);

				// There might be more data, so store the data received so far.
				IMessage message = (IMessage)CustomFormatter.Deserialize( client.udpbuffer );
				client.messageQueue.Enqueue( message );
				//Log.LogMessage( "enqueued " + message.GetType() + " message" );

				// Get the next message
				client.udpsocket.BeginReceive( client.udpbuffer, 0, MessageTypeMap.BufferSize, 0,
					new AsyncCallback(ReceiveUDPCallback), client );
			} catch ( Exception ) {
				client.Stop();
			}
		}

		public void Send( IMessage message ) {
			if ( !connected ) {
				// TODO: don't return, just throw
				return;
				throw new Exception( "Sending message while not connected." );
			}
			//try {
				// TODO: some clients may not want to send UDP messages
				if ( message is Strive.Network.Messages.ToServer.Position ) {
					SendUDP( message );
				} else {
					SendTCP( message );
				}
			//} catch ( Exception ) {
				//Stop();
			//}
		}

		void SendTCP( IMessage message ) {
			// Custom serialization
			byte [] buffer = CustomFormatter.Serialize( message );
			try {
				tcpsocket.BeginSend( buffer, 0, buffer.Length, 0,
					new AsyncCallback(SendTCPCallback), this );
			} catch ( Exception ) {
				Stop();
			}
		}

		private static void SendTCPCallback(IAsyncResult ar) {
			ServerConnection client = (ServerConnection) ar.AsyncState;
			try {
				// Complete sending the data to the remote device.
				int bytesSent = client.tcpsocket.EndSend(ar);
			} catch ( Exception ) {
				client.Stop();
			}
		}

		void SendUDP( IMessage message ) {
			// Custom serialization
			byte [] buffer = CustomFormatter.Serialize( message );
			//try {
				udpsocket.BeginSend( buffer, 0, buffer.Length, 0,
					new AsyncCallback(SendUDPCallback), this );
			//} catch ( Exception ) {
			//	Stop();
			//}
		}

		private static void SendUDPCallback(IAsyncResult ar) {
			ServerConnection client = (ServerConnection) ar.AsyncState;
			try {
				// Complete sending the data to the remote device.
				int bytesSent = client.udpsocket.EndSend(ar);
			} catch ( Exception ) {
				client.Stop();
			}
		}

		public int MessageCount {
			get { return messageQueue.Count; }
		}

		public IMessage PopNextMessage() {
			return (IMessage)messageQueue.Dequeue();
		}

		#region Simple Message API

		public void Chat(string message)
		{
			Send(new Strive.Network.Messages.ToServer.GameCommand.Communication( CommunicationType.Chat, message ) );
		}

		public void PossessMobile(int mobileId)
		{
			Send(new EnterWorldAsMobile(mobileId));
		}

		public void Login(string username, string password)
		{
			Send(new Login(username, password));
		}

		public void Logout()
		{
			Send(new Logout());
		}

		public void SkillList()
		{
			Send(new Strive.Network.Messages.ToServer.GameCommand.SkillList());
		}

		public void WhoList()
		{
			Send(new Strive.Network.Messages.ToServer.GameCommand.WhoList());
		}

		public void UseSkill(Strive.Multiverse.EnumSkill Skill)
		{
			Send(new Strive.Network.Messages.ToServer.GameCommand.UseSkill(Skill));
		}

		public void UseSkill(Strive.Multiverse.EnumSkill Skill, int[] Targets)
		{
			Send(new Strive.Network.Messages.ToServer.GameCommand.UseSkill(Skill, Targets));
		}

		public void UseSkill(int SkillID)
		{
			this.UseSkill((Strive.Multiverse.EnumSkill)SkillID);
		}

		public void UseSkill(int SkillID, int[] Targets)
		{
			this.UseSkill((Strive.Multiverse.EnumSkill)SkillID, Targets);
		}

		public void Position( Vector3D position, Vector3D rotation ) {
			Send(new Position( position, rotation ));
		}

		public void RequestPossessable()
		{
			Send(new RequestPossessable());
		}

		#endregion

	}
}
