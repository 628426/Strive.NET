using System;
using System.Collections;
using System.Threading;
using System.Net;

using Strive.Network.Messages;
using Strive.Network.Server;
using Strive.Math3D;
using Strive.Multiverse;

namespace Strive.Server.Shared {
	public class MessageProcessor {
		World world;
		UdpHandler listener;

		public MessageProcessor(
			World world, UdpHandler listener
		) {
			this.world = world;
			this.listener = listener;
		}

		public void ProcessNextMessage() {
			ClientMessage clientMessage = (ClientMessage)listener.PopNextMessage();
			ProcessMessage( clientMessage.Client, clientMessage.Message );
		}

		public void ProcessMessage( Client client, IMessage message ) {
			if ( !client.Authenticated ) {
				// new connection... only allow login
				if ( message is Network.Messages.ToServer.Login ) {
					ProcessLoginMessage(
						client, (Network.Messages.ToServer.Login)message
					);
				} else {
					Global.log.WarningMessage( "Non-login message " + message.GetType() + " from " + client.EndPoint );
				}
				return;
			}

			if ( message is Network.Messages.ToServer.RequestPossessable ) {
				ProcessRequestPossessable( client, message as Network.Messages.ToServer.RequestPossessable );
				return;
			}

			if ( client.Avatar == null ) {
				// no character selected yet... only allow posses
				// and logout
				if ( message is Network.Messages.ToServer.EnterWorldAsMobile ) 
				{
					ProcessEnterWorldAsMobile( client, (Network.Messages.ToServer.EnterWorldAsMobile)message );
				} 
				else if(message is Network.Messages.ToServer.Logout)
				{

				}
				else
				{

					Global.log.WarningMessage( "ERROR: Non-posses message " + message.GetType() + " from " + client.EndPoint );
				}
				return;
			}

			// normal message for logged in user
			if ( message is Network.Messages.ToServer.Position ) 
			{
				ProcessPositionMessage( client, message as Network.Messages.ToServer.Position );
			} 
			else if ( message is Network.Messages.ToServer.GameCommand.Communication ) 
			{
				ProcessChatMessage( client, message as Network.Messages.ToServer.GameCommand.Communication );
			} 
			else if ( message is Network.Messages.ToServer.GameCommand.UseSkill ) 
			{
				SkillCommandProcessor.ProcessUseSkill( client, message as Network.Messages.ToServer.GameCommand.UseSkill );
			} 
			else if ( message is Network.Messages.ToServer.GameCommand.Attack ) 
			{
				ProcessAttackMessage( client, message as Network.Messages.ToServer.GameCommand.Attack );
			} 
			else if ( message is Network.Messages.ToServer.GameCommand.Flee ) 
			{
				ProcessFleeMessage( client, message as Network.Messages.ToServer.GameCommand.Flee );
			} 
			else if ( message is Network.Messages.ToServer.ReloadWorld ) 
			{
				ProcessReloadWorldMessage( client, message as Network.Messages.ToServer.ReloadWorld );
			} 
			else if ( message is Network.Messages.ToServer.Logout ) 
			{
				ProcessLogout(client);
			}
			else if ( message is Network.Messages.ToServer.GameCommand.SkillList )
			{
				ProcessSkillList(client, message as Network.Messages.ToServer.GameCommand.SkillList);
			}
			else if ( message is Network.Messages.ToServer.GameCommand.WhoList )
			{
				ProcessWhoList(client, message as Network.Messages.ToServer.GameCommand.WhoList);
			}
			else 
			{
				Global.log.WarningMessage(
					"ERROR: Unknown message " + message.GetType()
					);
			}
		}

		void ProcessLoginMessage(
			Client client, Strive.Network.Messages.ToServer.Login loginMessage
			) {
			if (
				world.UserLookup( loginMessage.username, loginMessage.password, ref client.PlayerID )
			) {
				Global.log.LogMessage(
					"User " + loginMessage.username + " logged in"
					);
				client.AuthenticatedUsername = loginMessage.username;
			} else {
				Global.log.LogMessage(
					"Login failed for username " + loginMessage.username
				);
			}
		}

		void ProcessRequestPossessable( Client client, Strive.Network.Messages.ToServer.RequestPossessable message ) {
			Strive.Data.MultiverseFactory.refreshMultiverseForPlayer( Global.multiverse, client.PlayerID );
			Strive.Network.Messages.ToClient.CanPossess canPossess = new Strive.Network.Messages.ToClient.CanPossess(
				world.getPossessable( client.AuthenticatedUsername ) );
			client.Send( canPossess );
		}

		void ProcessLogout( Client client) 
		{
			if(client.Avatar != null)
			{
				// remove from world.
				world.Remove(client.Avatar);
			}
			Global.log.LogMessage("Logged out '" + client.AuthenticatedUsername +"'.");
			client.Close(); 
		}

			

		void ProcessEnterWorldAsMobile( Client client, Strive.Network.Messages.ToServer.EnterWorldAsMobile message ) {
			MobileAvatar a;
			if ( world.physicalObjects.ContainsKey( message.InstanceID ) ) {
				// reconnected
				// simply replace existing connection with the new
				// TODO:  NTR 26 Mar 2003: The above actually needs to be properly implemented
				// Currently, the MobileAvatar in the World is correctly updated on reconnect,
				// but the OLD client in listener.Clients isn't removed
				Object o = world.physicalObjects[message.InstanceID];
				if ( o is MobileAvatar ) {
					a = (MobileAvatar)o;
				} else {
					Global.log.WarningMessage( "Can only possess mobiles." );
					return;
				}
				if ( a.client == client ) {
					Global.log.WarningMessage( "A client " + client.ToString() + " attempted to take control of the same mobile " + a.ObjectInstanceID + " twice... ignoring request." );
				} else if ( a.client != null ) {
					// EEERRR print reconnected message to clients
					Global.log.LogMessage( "Mobile " + a.ObjectInstanceID + " has been taken over by a new connection." );
					a.client = client;
					client.Avatar = a;
				} else {
					a.client = client;
					client.Avatar = a;
				}
			} else {
				// try to load the character
				a = world.LoadMobile( message.InstanceID );
				if ( a == null ) {
					Global.log.WarningMessage( "Character "+message.InstanceID+" not found." );
					client.Close();
					return;
				}
				a.client = client;
				client.Avatar = a;

				// try to add the character to the world
				world.Add( client.Avatar );
			}
			world.SendInitialWorldView( client );
		}

		void ProcessPositionMessage(
			Client client, Strive.Network.Messages.ToServer.Position message
		) {
			world.Relocate( client.Avatar,
				new Vector3D( message.position_x, message.position_y, message.position_z ),
				new Vector3D( message.heading_x, message.heading_y, message.heading_z )
			);
		}

		void ProcessChatMessage(
			Client client, Strive.Network.Messages.ToServer.GameCommand.Communication message
		) {
			foreach ( Client c in listener.Clients.Values ) {
				if (  c == client ) continue;
				c.Send(
					new Network.Messages.ToClient.Communication( client.Avatar.ObjectTemplateName, message.message, (Strive.Network.Messages.CommunicationType)message.communicationType )
				);
			}
		}

		void ProcessAttackMessage(
			Client client, Strive.Network.Messages.ToServer.GameCommand.Attack message
		) {
			(client.Avatar as MobileAvatar).Attack( message.targetObjectInstanceID );
		}

		void ProcessFleeMessage(
			Client client, Strive.Network.Messages.ToServer.GameCommand.Flee message
		) {
			(client.Avatar as MobileAvatar).Flee();
		}

		void ProcessReloadWorldMessage(
			Client client, Strive.Network.Messages.ToServer.ReloadWorld message
		) {
			Global.log.LogMessage( "ReloadWorld received." );
			world.Load();
			foreach( Client c in listener.Clients.Values ) {
				if ( c.Avatar != null ) {
					// respawn their mobile, old instance will be given over
					// to Garbage Collector
					c.Send( new Strive.Network.Messages.ToClient.DropAll() );
					ProcessEnterWorldAsMobile( c, new Strive.Network.Messages.ToServer.EnterWorldAsMobile( c.Avatar.ObjectInstanceID ) );
				}
			}
		}

		void ProcessWhoList(
			Client client, Strive.Network.Messages.ToServer.GameCommand.WhoList message
		) {
			ArrayList MobileIDs = new ArrayList();
			ArrayList Names = new ArrayList();
			foreach( Client c in listener.Clients.Values ) {
				if ( c.Avatar != null ) {
					MobileIDs.Add( c.Avatar.ObjectInstanceID );
					Names.Add( c.Avatar.ObjectTemplateName );
				}
			}
			client.Send( new Strive.Network.Messages.ToClient.WhoList( (int[])MobileIDs.ToArray( typeof( int ) ), (string[])Names.ToArray( typeof( string ) ) ) );
		}

		void ProcessSkillList(
			Client client, Strive.Network.Messages.ToServer.GameCommand.SkillList message
		) {
			ArrayList skillIDs = new ArrayList();
			ArrayList competancy = new ArrayList();
			for ( int i = 1; i < Global.multiverse.EnumSkill.Count; i++ ) {
				Schema.MobileHasSkillRow mhs = Global.multiverse.MobileHasSkill.FindByObjectTemplateIDEnumSkillID( client.Avatar.ObjectTemplateID, i );
				if ( mhs != null ) {
					skillIDs.Add( mhs.EnumSkillID );
					competancy.Add( mhs.Rating );
				}
			}
			client.Send( new Strive.Network.Messages.ToClient.SkillList( (int [])skillIDs.ToArray( typeof( int ) ), (float [])competancy.ToArray( typeof( float ) ) ) );
		}

		public void CleanupDeadConnections() {
			// Make a list of dead connections and remove them all
			ArrayList al = new ArrayList();
			foreach ( Client c in listener.Clients.Values ) {
				if ( (DateTime.Now - c.LastMessageTimestamp).Seconds > 60 ) {
					al.Add( c.EndPoint );
				}
			}
			foreach ( IPEndPoint ep in al ) {
				Global.log.LogMessage( "Dropping connection to "+ep+" due to inactivity" );
				listener.Clients.Remove( ep );
			}
		}
	}
}
