using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.IO;

using Strive.Network.Messages;
using Common.Logging;

namespace Strive.Network.Server {
	/// <summary>
	/// Summary description for Listener.
	/// </summary>
	public class Listener {
		Hashtable clients = new Hashtable();
		// TODO: refactor don't expose underlying queue
		public Queue clientMessageQueue = new Queue();
		public Socket udpsocket;
		Socket tcpsocket;
		IPEndPoint localEndPoint;
		EndPoint remoteEndPoint = new IPEndPoint( IPAddress.Any, 0 );
		byte[] udpbuffer = new byte[MessageTypeMap.BufferSize]; // Receive buffer.

        ILog Log;
		public Listener( IPEndPoint localEndPoint ) {
			this.localEndPoint = localEndPoint;
            Log = LogManager.GetCurrentClassLogger();
        }

		public void Start() {
			try {
				tcpsocket = new Socket( localEndPoint.Address.AddressFamily,
					SocketType.Stream, ProtocolType.Tcp );
				tcpsocket.Bind( localEndPoint );
				tcpsocket.Listen( 10 );
				tcpsocket.BeginAccept(
					new AsyncCallback(acceptCallback), 
					this );
				
				udpsocket = new Socket(AddressFamily.InterNetwork,
					SocketType.Dgram, ProtocolType.Udp);
				udpsocket.Bind( localEndPoint );

				// Begin reading udp packets
				udpsocket.BeginReceiveFrom( udpbuffer, 0, MessageTypeMap.BufferSize, 0, ref remoteEndPoint,
					new AsyncCallback(ReceiveFromUDPCallback), this );
			} catch ( ObjectDisposedException ) {
				// the underlying socket was closed
			}
		}

		public static void ReceiveFromUDPCallback(IAsyncResult ar) {
			try {
				Listener handler = (Listener) ar.AsyncState;
				int bytesRead = handler.udpsocket.EndReceiveFrom(ar, ref handler.remoteEndPoint );
				Client client = handler.clients[handler.remoteEndPoint] as Client;
				if ( client == null || !client.Authenticated ) {
					// ignore the packet
					return;
				}
				if ( bytesRead == MessageTypeMap.BufferSize ) {
					throw new Exception( "Reached max buffer size, increase this limit." );
				}

				IMessage message;
				try {
					message = (IMessage)CustomFormatter.Deserialize( handler.udpbuffer, 0 );
				} catch ( Exception e ) {
					handler.Log.Error( "Invalid packet received", e );
					return;
				}
				client.LastMessageTimestamp = DateTime.Now;
				ClientMessage clientMessage = new ClientMessage( client, message );
				// TODO: ensure threadsafe access to queue
				handler.clientMessageQueue.Enqueue( clientMessage );

				handler.udpsocket.BeginReceiveFrom( handler.udpbuffer, 0, MessageTypeMap.BufferSize, 0, ref handler.remoteEndPoint,
					new AsyncCallback(ReceiveFromUDPCallback), handler );
			} catch ( Exception ) {
				// the underlying socket was closed
			}
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
		}

		public static void acceptCallback(IAsyncResult ar) {
			try {
				// Get the socket that handles the client request.
				Listener handler = (Listener) ar.AsyncState;
				Socket clientsocket = handler.tcpsocket.EndAccept(ar);

				// Create the state object.
				Client client = new Client( clientsocket, handler );
				handler.clients.Add( client.EndPoint, client );
				handler.Log.Info( "New connection from " + client.EndPoint );

				// The next connection
				handler.tcpsocket.BeginAccept(
					new AsyncCallback(acceptCallback), 
					handler );
			} catch ( ObjectDisposedException ) {
				// the underlying socket was closed
			}
		}

		public int MessageCount {
			get { return clientMessageQueue.Count; }
		}

		public ClientMessage PopNextMessage() {
			return (ClientMessage)clientMessageQueue.Dequeue();
		}

		public Hashtable Clients {
			get { return clients; }
		}


		public void SendToAll( IMessage message ) {
			foreach ( Client c in clients.Values ) {
				if ( c.Authenticated ) {
					c.Send( message );
				}
			}
		}
	}
}