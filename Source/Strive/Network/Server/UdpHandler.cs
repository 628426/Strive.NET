using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Strive.Network.Messages;
using Strive.Common;

namespace Strive.Network.Server {
	/// <summary>
	/// Summary description for NetworkHandler.
	/// </summary>
	public class UdpHandler {
		Queue clientMessageQueue = new Queue();
		IPEndPoint localEndPoint;
		//BinaryFormatter formatter = new BinaryFormatter();
		Hashtable clients = new Hashtable();
		StoppableThread myThread;

		//Creates a UdpClient for reading incoming data.
		UdpClient receivingUdpClient;

		//Creates an IPEndPoint to record the IP Address and port number of the sender. 
		// The IPEndPoint will allow you to read datagrams sent from any source.
		IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);

		public UdpHandler( IPEndPoint localEndPoint ) {
			this.localEndPoint = localEndPoint;
			receivingUdpClient = new UdpClient( localEndPoint );
			myThread = new StoppableThread( new StoppableThread.WhileRunning( Run ) );
		}

		public class AlreadyRunningException : Exception{}
		public void Start() {
			myThread.Start();
		}

		public void Stop() {
			if ( receivingUdpClient != null ) {
				receivingUdpClient.Close();
				receivingUdpClient = null;
			}
			myThread.Stop();
		}

		public void Run() {
			Byte[] receivedBytes;
			try {
				// Blocks until a message returns on this socket from a remote host.
				receivedBytes = receivingUdpClient.Receive(ref endpoint);
			} catch ( Exception ) {
				// do nothing, socket was closed by another thread
				return;
			}
			IMessage message;
			try {
				// Generic serialization
				//message = (IMessage)formatter.Deserialize(
				//	new MemoryStream( receivedBytes )
				//);
	
				// Custom serialization
				message = (IMessage)CustomFormatter.Deserialize( receivedBytes );
			} catch ( Exception e ) {
				System.Console.WriteLine( e );
				System.Console.WriteLine( "Invalid packet received" );
				return;
			}
			Client client = (Client)clients[endpoint];
			if ( client == null ) {
				// new connection
				System.Console.WriteLine(
					"New connection from " + endpoint );
				client = new Client( endpoint );
				clients.Add( endpoint, client );
			}
			client.LastMessageTimestamp = DateTime.Now;
			ClientMessage clientMessage = new ClientMessage(
				client, message	);
			clientMessageQueue.Enqueue( clientMessage );
			//Console.WriteLine( message.GetType() + " message enqueued from " + endpoint );
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
	}
}
