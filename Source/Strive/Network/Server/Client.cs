using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;

using Strive.Multiverse;
using Strive.Network.Messages;
using Strive.Logging;

namespace Strive.Network.Server {
	/// <summary>
	/// Summary description for Client.
	/// </summary>
	public class Client	{
		Mobile avatar = null;
		string authenticatedUsername = null;
		public int PlayerID = 0;
		Socket tcpsocket;
		byte[] tcpbuffer = new byte[MessageTypeMap.BufferSize]; // Receive buffer.
		DateTime lastMessageTimestamp;
		Listener handler;

		public Client( Socket tcpsocket, Listener handler ) {
			this.tcpsocket = tcpsocket;
			this.handler = handler;

			// Begin Reading.
			try {
				tcpsocket.BeginReceive( tcpbuffer, 0, MessageTypeMap.BufferSize, 0,
					new AsyncCallback(ReadTCPCallback), this );
			} catch ( Exception ) {
				Close();
			}
		}

		public static void ReadTCPCallback(IAsyncResult ar) {
			Client client = (Client) ar.AsyncState;
			try {
				int bytesRead = client.tcpsocket.EndReceive(ar);
				if ( bytesRead == MessageTypeMap.BufferSize ) {
					Log.ErrorMessage( "Reached max buffer size." );
					throw new Exception();
				}

				IMessage message;
				try {
					message = (IMessage)CustomFormatter.Deserialize( client.tcpbuffer );
				} catch ( Exception e ) {
					Log.ErrorMessage( e );
					Log.ErrorMessage( "Invalid packet received" );
					return;
				}
				client.LastMessageTimestamp = DateTime.Now;
				ClientMessage clientMessage = new ClientMessage( client, message );
				// TODO: ensure threadsafe access to queue
				client.handler.clientMessageQueue.Enqueue( clientMessage );

				client.tcpsocket.BeginReceive( client.tcpbuffer, 0, MessageTypeMap.BufferSize, 0,
					new AsyncCallback(ReadTCPCallback), client );
			} catch ( Exception ) {
				// the underlying socket was closed
				client.Close();
			}
		}

		public bool Authenticated {
			get { return authenticatedUsername != null; }
		}

		public string AuthenticatedUsername {
			get { return authenticatedUsername; }
			set {
				authenticatedUsername = value;
			}
		}

		public void Send( IMessage message ) {
			// TODO: some clients may prefer no UDP
			if ( !Authenticated ) {
				Log.ErrorMessage( "Trying to send message without authenticated connection." );
				return;
			}

			if ( message is Strive.Network.Messages.ToClient.Position ) {
				SendUDP( message );
			} else {
				SendTCP( message );
			}
		}

		void SendTCP( IMessage message ) {
			// Custom serialization
			byte[] EncodedMessage = CustomFormatter.Serialize( message );

			// Begin sending the data to the remote device.
			try {
				tcpsocket.BeginSend( EncodedMessage, 0, EncodedMessage.Length, 0,
					new AsyncCallback(SendTCPCallback), this );
			} catch ( Exception ) {
				// the underlying socket was closed
				Close();
			}
		}

		private static void SendTCPCallback(IAsyncResult ar) {
			// Retrieve the socket from the state object.
			Client client = (Client) ar.AsyncState;
			try {
				// Complete sending the data to the remote device.
				int bytesSent = client.tcpsocket.EndSend(ar);
			} catch ( Exception ) {
				// the underlying socket was closed
				client.Close();
			}
		}

		void SendUDP( IMessage message ) {
			// Custom serialization
			byte[] EncodedMessage = CustomFormatter.Serialize( message );

			// Begin sending the data to the remote device.
			try {
				handler.udpsocket.BeginSendTo( EncodedMessage, 0, EncodedMessage.Length, 0,
					tcpsocket.RemoteEndPoint, new AsyncCallback(SendToUDPCallback), this );
			} catch ( Exception ) {
				// the underlying socket was closed
				Close();
			}
		}

		private static void SendToUDPCallback(IAsyncResult ar) {
			// Retrieve the socket from the state object.
			Client client = (Client) ar.AsyncState;
			try {
				// Complete sending the data to the remote device.
				int bytesSent = client.handler.udpsocket.EndSendTo(ar);
			} catch (Exception) {
				// underlying socket was closed
				client.Close();
			}
		}

		public void Close() {
			if ( tcpsocket != null ) {
				tcpsocket.Close();
				tcpsocket = null;
			}
			authenticatedUsername = null;
		}

		public Mobile Avatar {
			get { return avatar; }
			set { avatar = value; }
		}

		public DateTime LastMessageTimestamp {
			get { return lastMessageTimestamp; }
			set { lastMessageTimestamp = value; }
		}

		public EndPoint EndPoint {
			get { return tcpsocket.RemoteEndPoint; }
		}
	}
}
