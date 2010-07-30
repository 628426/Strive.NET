using System;
using System.Collections;
using Strive.Server.Model;
using Strive.Network.Server;
using Strive.Network.Messages;
//using Strive.Rendering;
//using Strive.Rendering.Models;
//using Strive.Resources;
using Strive.Common;


namespace Strive.Server.Logic
{
	/// <summary>
	/// A square encompasses all the physical objects and clients
	/// in a discreet area.
	/// The World is split into Squares so that nearby objects and clients
	/// may be referenced with ease.
	/// Squares are used to define a clients world view,
	/// as only neigbouring physical objects affect the client.
	/// </summary>
	public class Square
	{
		public static int squareSize = Constants.objectScopeRadius;
		public ArrayList physicalObjects = new ArrayList();
		public ArrayList clients = new ArrayList();

		public Square() {
		}

		public void Add( PhysicalObject po ) {
			physicalObjects.Add( po );
			if ( po is MobileAvatar ) {
				MobileAvatar a = (MobileAvatar)po;
				if ( a.client != null ) {
					clients.Add( a.client );
				}
			}
		}

		public void Remove( PhysicalObject po ) {
			physicalObjects.Remove( po );
			if ( po is MobileAvatar ) {
				MobileAvatar a = (MobileAvatar)po;
				if ( a.client != null ) {
					clients.Remove( a.client );
				}
			}
		}

		public void NotifyClients( IMessage message ) {
			foreach ( Client c in clients ) {
				c.Send( message );
			}
		}

		public void NotifyClientsExcept( IMessage message, Client client ) {
			foreach ( Client c in clients ) {
				if ( c == client ) continue;
				c.Send( message );
			}
		}
	}
}