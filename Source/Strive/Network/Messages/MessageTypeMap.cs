using System;
using System.Collections;

using Strive.Multiverse;

namespace Strive.Network.Messages {
	/// <summary>
	/// Summary description for MessageTypeMap.
	/// </summary>
	public class MessageTypeMap	{
		public const int BufferSize = 16384;	// max message size
		public Hashtable messageTypeFromID = new Hashtable();
		public Hashtable idFromMessageType = new Hashtable();
		public enum EnumMessageID {
			ToClientAcknowledge = 1,
			ToClientAddReadable,
			ToClientAddEquipable,
			ToClientAddWieldable,
			ToClientAddJunk,
			ToClientAddTerrain,
			ToClientAddMobile,
			ToClientAddQuaffable,
			ToClientCanPossess,
			ToClientCombatReport,
			ToClientCommunication,
			ToClientCurrentHitpoints,
			ToClientDropAll,
			ToClientDropPhysicalObject,
			ToClientDropPhysicalObjects,
			ToClientNegativeAcknowledge,
			ToClientPosition,
			ToClientBeat,
			ToClientMobileState,
			ToClientServerInfo,
			ToClientSkillList,
			ToClientWeather,
			ToClientWhoList,
			ToServerAttack,
			ToServerChangeStance,
			ToServerCommunication,
			ToServerEmote,
			ToServerFlee,
			ToServerSkillList,
			ToServerUseSkill,
			ToServerWhoList,
			ToServerEnterWorldAsMobile,
			ToServerLogin,
			ToServerLogout,
			ToServerReloadWorld,
			ToServerRequestPossessable,
			ToServerRequestServerInfo,
			ToServerPosition
		}
		public MessageTypeMap()	{
			// build the mapping between message_id and message_type
			messageTypeFromID.Add( EnumMessageID.ToClientAcknowledge, typeof( ToClient.Acknowledge ) );
			messageTypeFromID.Add( EnumMessageID.ToClientAddReadable, typeof( ToClient.AddReadable ) );
			messageTypeFromID.Add( EnumMessageID.ToClientAddEquipable, typeof( ToClient.AddEquipable ) );
			messageTypeFromID.Add( EnumMessageID.ToClientAddWieldable, typeof( ToClient.AddWieldable ) );
			messageTypeFromID.Add( EnumMessageID.ToClientAddMobile, typeof( ToClient.AddMobile ) );
			messageTypeFromID.Add( EnumMessageID.ToClientAddJunk, typeof( ToClient.AddJunk ) );
			messageTypeFromID.Add( EnumMessageID.ToClientAddTerrain, typeof( ToClient.AddTerrain ) );
			messageTypeFromID.Add( EnumMessageID.ToClientAddQuaffable, typeof( ToClient.AddQuaffable ) );
			messageTypeFromID.Add( EnumMessageID.ToClientCanPossess, typeof( ToClient.CanPossess ) );
			messageTypeFromID.Add( EnumMessageID.ToClientCombatReport, typeof( ToClient.CombatReport ) );
			messageTypeFromID.Add( EnumMessageID.ToClientCommunication, typeof( ToClient.Communication ) );
			messageTypeFromID.Add( EnumMessageID.ToClientCurrentHitpoints, typeof( ToClient.CurrentHitpoints ) );
			messageTypeFromID.Add( EnumMessageID.ToClientDropAll, typeof( ToClient.DropAll ) );
			messageTypeFromID.Add( EnumMessageID.ToClientDropPhysicalObject, typeof( ToClient.DropPhysicalObject ) );
			messageTypeFromID.Add( EnumMessageID.ToClientDropPhysicalObjects, typeof( ToClient.DropPhysicalObjects ) );
			messageTypeFromID.Add( EnumMessageID.ToClientNegativeAcknowledge, typeof( ToClient.NegativeAcknowledge ) );
			messageTypeFromID.Add( EnumMessageID.ToClientPosition, typeof( ToClient.Position ) );
			messageTypeFromID.Add( EnumMessageID.ToClientMobileState, typeof( ToClient.MobileState ) );					
			messageTypeFromID.Add( EnumMessageID.ToClientServerInfo, typeof( ToClient.ServerInfo ) );					
			messageTypeFromID.Add( EnumMessageID.ToClientSkillList, typeof( ToClient.SkillList ) );					
			messageTypeFromID.Add( EnumMessageID.ToClientWeather, typeof( ToClient.Weather ) );					
			messageTypeFromID.Add( EnumMessageID.ToClientWhoList, typeof( ToClient.WhoList ) );					
			messageTypeFromID.Add( EnumMessageID.ToClientBeat, typeof( ToClient.Beat) );					

			messageTypeFromID.Add( EnumMessageID.ToServerAttack, typeof( ToServer.GameCommand.Attack ) );
			messageTypeFromID.Add( EnumMessageID.ToServerChangeStance, typeof( ToServer.GameCommand.ChangeStance ) );
			messageTypeFromID.Add( EnumMessageID.ToServerCommunication, typeof( ToServer.GameCommand.Communication ) );
			messageTypeFromID.Add( EnumMessageID.ToServerEmote, typeof( ToServer.GameCommand.Emote ) );
			messageTypeFromID.Add( EnumMessageID.ToServerFlee, typeof( ToServer.GameCommand.Flee ) );
			messageTypeFromID.Add( EnumMessageID.ToServerSkillList, typeof( ToServer.GameCommand.SkillList ) );
			messageTypeFromID.Add( EnumMessageID.ToServerUseSkill, typeof( ToServer.GameCommand.UseSkill ) );
			messageTypeFromID.Add( EnumMessageID.ToServerWhoList, typeof( ToServer.GameCommand.WhoList ) );
			messageTypeFromID.Add( EnumMessageID.ToServerEnterWorldAsMobile, typeof( ToServer.EnterWorldAsMobile ) );
			messageTypeFromID.Add( EnumMessageID.ToServerLogin, typeof( ToServer.Login ) );
			messageTypeFromID.Add( EnumMessageID.ToServerLogout, typeof( ToServer.Logout ) );
			messageTypeFromID.Add( EnumMessageID.ToServerReloadWorld, typeof( ToServer.ReloadWorld ) );
			messageTypeFromID.Add( EnumMessageID.ToServerRequestPossessable, typeof( ToServer.RequestPossessable ) );
			messageTypeFromID.Add( EnumMessageID.ToServerRequestServerInfo, typeof( ToServer.RequestServerInfo ) );
			messageTypeFromID.Add( EnumMessageID.ToServerPosition, typeof( ToServer.Position ) );

			// build the reverse lookup
			foreach ( EnumMessageID id in messageTypeFromID.Keys ) {
				idFromMessageType.Add( messageTypeFromID[id], id );
			}
		}
	}
}
