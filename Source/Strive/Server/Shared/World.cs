using System;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Collections;
using System.Threading;

using Strive.Common;
using Strive.Network.Server;
using Strive.Network.Messages;
using Strive.Multiverse;
using Strive.Math3D;
using Strive.Data;
using Strive.Logging;

// todo: this object needs to be made threadsafe

namespace Strive.Server.Shared {
	public class World {
		double highX = 0.0;
		double highZ = 0.0;
		double lowX = 0.0;
		double lowZ = 0.0;
		int squaresInX;		// see squareSize in Square
		int squaresInZ;
		int world_id;
		
		// squares are used to group physical objects
		Square[,] square;
		Terrain[,] terrain;

		// all physical objects are indexed in a hashtable
		public Hashtable physicalObjects;
		public ArrayList mobilesArrayList;

		Strive.Network.Messages.ToClient.Weather weather = new Strive.Network.Messages.ToClient.Weather( 1, 0, 0, 0 );

		public World( int world_id ) {
			this.world_id = world_id;
			Load();
		}

		public void Load() {
			physicalObjects = new Hashtable();
			mobilesArrayList = new ArrayList();

			// todo: would be nice to be able to load only the
			// world in question... but for now load them all
			Log.LogMessage( "Loading Global.multiverse..." );
			if ( Global.worldfilename != null ) {
				Global.multiverse = Strive.Data.MultiverseFactory.getMultiverseFromFile( Global.worldfilename );
			} else if ( Global.connectionstring != null ) {
				Global.multiverse = Strive.Data.MultiverseFactory.getMultiverseFromDatabase( Global.connectionstring );
			} else {
				throw new Exception( "must specify a world file or connection string" );
			}
			Log.LogMessage( "Global.multiverse loaded." );

			// find highX and lowX for our world dimensions
			// refactored in an attempt to increase performance:
			highX = 0;
			lowX = 0;
			highZ = 0;
			lowZ = 0;
			foreach(Schema.ObjectInstanceRow r in Global.multiverse.ObjectInstance.Rows) {
				if(highX == 0) {
					highX = r.X;
				}
				if(lowX == 0){
					lowX = r.X;
				}
				if(highZ == 0){ 
					highZ = r.Z;
				}
				if(lowZ == 0) {
					lowZ = 0;
				}

				if(r.X > highX) {
					highX = r.X;
				}
				if(r.X < lowX) {
					lowX = r.X;
				}
				if(r.Z > highZ) {
					highZ = r.Z;
				}
				if(r.Z < lowZ) {
					lowZ = r.Z;
				}
			}
			//highX = ((Schema.ObjectInstanceRow)Global.multiverse.ObjectInstance.Select( "X = max(X)" )[0]).X;
			//lowX = ((Schema.ObjectInstanceRow)Global.multiverse.ObjectInstance.Select( "X = min(X)" )[0]).X;
			//highZ = ((Schema.ObjectInstanceRow)Global.multiverse.ObjectInstance.Select( "Z = max(Z)" )[0]).Z;
			//lowZ = ((Schema.ObjectInstanceRow)Global.multiverse.ObjectInstance.Select( "Z = min(Z)" )[0]).Z;
			Log.LogMessage( "Global.multiverse bounds are " + lowX + "," + lowZ + " " + highX + "," + highZ );

			// figure out how many squares we need
			squaresInX = (int)(highX-lowX)/Square.squareSize + 1;
			squaresInZ = (int)(highZ-lowZ)/Square.squareSize + 1;

//			if ( squaresInX * squaresInZ > 10000 ) {
//				throw new Exception( "World is too big. Total area must not exceed " + 10000*Square.squareSize + ". Please fix the database." );
//			}

			// allocate the grid of squares used for grouping
			// physical objects that are close to each other
			square = new Square[squaresInX,squaresInZ];
			terrain = new Terrain[squaresInX*Square.squareSize/Constants.terrainPieceSize,squaresInZ*Square.squareSize/Constants.terrainPieceSize];

			Schema.WorldRow wr = Global.multiverse.World.FindByWorldID( world_id );
			if ( wr == null ) {
				throw new Exception( "ERROR: World ID not valid!" );	
			}
			
			Log.LogMessage( "Loading world \"" + wr.WorldName + "\"..." );
			Log.LogMessage( "Loading terrain..." );
			foreach ( Schema.TemplateTerrainRow ttr in Global.multiverse.TemplateTerrain.Rows ) {
				foreach ( Schema.ObjectInstanceRow oir in ttr.TemplateObjectRow.GetObjectInstanceRows() ) {
					Terrain t = new Terrain( ttr, ttr.TemplateObjectRow, oir );
					Add( t );
				}
			}
			Log.LogMessage( "Loading physical objects..." );
			foreach ( Schema.TemplateObjectRow otr in Global.multiverse.TemplateObject.Rows ) {
					foreach ( Schema.TemplateMobileRow tmr in otr.GetTemplateMobileRows() ) {
						foreach ( Schema.ObjectInstanceRow oir in otr.GetObjectInstanceRows() ) {
							// NB: don't add players yet
							if ( oir.GetMobilePossesableByPlayerRows().Length > 0 ) continue;

							// NB: we only add avatars to our world, not mobiles
							MobileAvatar a = new MobileAvatar( this, tmr, otr, oir );
							Add( a );
						}
					}
					foreach ( Schema.TemplateItemRow tir in otr.GetTemplateItemRows() ) {
						foreach ( Schema.TemplateItemEquipableRow ier in tir.GetTemplateItemEquipableRows() ) {
							foreach ( Schema.ObjectInstanceRow oir in otr.GetObjectInstanceRows() ) {
								Equipable e = new Equipable( ier, tir, otr, oir );
								Add( e );
							}
						}
						foreach ( Schema.TemplateItemJunkRow ijr in tir.GetTemplateItemJunkRows() ) {
							foreach ( Schema.ObjectInstanceRow oir in otr.GetObjectInstanceRows() ) {
								Junk j = new Junk( ijr, tir, otr, oir );
								Add( j );
							}
						}
						foreach ( Schema.TemplateItemQuaffableRow iqr in tir.GetTemplateItemQuaffableRows() ) {
							foreach ( Schema.ObjectInstanceRow oir in otr.GetObjectInstanceRows() ) {
								Quaffable q = new Quaffable( iqr, tir, otr, oir );
								Add( q );
							}
						}
						foreach ( Schema.TemplateItemReadableRow irr in tir.GetTemplateItemReadableRows() ) {
							foreach ( Schema.ObjectInstanceRow oir in otr.GetObjectInstanceRows() ) {
								Readable r = new Readable( irr, tir, otr, oir );
								Add( r );
							}
						}
						foreach ( Schema.TemplateItemWieldableRow iwr in tir.GetTemplateItemWieldableRows() ) {
							foreach ( Schema.ObjectInstanceRow oir in otr.GetObjectInstanceRows() ) {
								Wieldable w = new Wieldable( iwr, tir, otr, oir );
								Add( w );
							}

						}
					}
				}
			Log.LogMessage( "Loaded world." );
		}

		public void Update() {
			foreach ( PhysicalObject po in physicalObjects.Values ) {
				if ( po is MobileAvatar ) {
					(po as MobileAvatar).Update();
				}
			}
			WeatherUpdate();
		}

		void WeatherUpdate() {
			bool weatherChanged = false;
			if ( Global.random.NextDouble() > 0.999 ) {
				weather.Fog++;
				weatherChanged = true;
			}
			if ( Global.random.NextDouble() > 0.999 ) {
				weather.Lighting++;
				weatherChanged = true;
			}
			if ( Global.random.NextDouble() > 0.995 ) {
				weather.SkyTextureID = (weather.SkyTextureID + 1) % 9 + 1;
				weatherChanged = true;
			}
			if ( weatherChanged ) {
				NotifyMobiles( weather );
			}
		}

		public void NotifyMobiles( Strive.Network.Messages.IMessage message ) {
			foreach ( MobileAvatar ma in mobilesArrayList ) {
				if ( ma.client != null ) {
					ma.client.Send( message );
				}
			}
		}

		public void Add( PhysicalObject po ) {
			if (
				po.Position.X > highX || po.Position.Z > highZ
				|| po.Position.X < lowX || po.Position.Z < lowZ
			) {
				Log.ErrorMessage( "Tried to add physical object " + po.ObjectInstanceID + " outside the world." );
				return;
			}

			// keep everything at ground level
			if ( !(po is Terrain) ) {
				try {
					float altitude = AltitudeAt( po.Position.X, po.Position.Z );
					po.Position.Y = altitude + po.Height/2F;
				} catch ( InvalidLocationException ) {
					Log.WarningMessage( "Physical object " + po.ObjectInstanceID + " is not on terrain." );
				}
			}

			// add the object to the world
			physicalObjects.Add( po.ObjectInstanceID, po );
			if ( po is Mobile ) {
				mobilesArrayList.Add( po );
			}
			int squareX = (int)(po.Position.X-lowX)/Square.squareSize;
			int squareZ = (int)(po.Position.Z-lowZ)/Square.squareSize;
			if ( square[squareX,squareZ] == null ) {
				square[squareX,squareZ] = new Square();
			}
			square[squareX,squareZ].Add( po );
			if ( po is Terrain ) {
				int terrainX = Helper.DivTruncate( (int)(po.Position.X-lowX), Constants.terrainPieceSize );
				int terrainZ = Helper.DivTruncate( (int)(po.Position.Z-lowZ), Constants.terrainPieceSize );
				terrain[terrainX,terrainZ] = (Terrain)po;
			}

			// notify all nearby clients that a new
			// physical object has entered the world
			InformNearby( po, Strive.Network.Messages.ToClient.AddPhysicalObject.CreateMessage( po ) );
			//Log.LogMessage( "Added new " + po.GetType() + " " + po.ObjectInstanceID + " at (" + po.Position.X + "," + po.Position.Y + "," +po.Position.Z + ") - ("+squareX+","+squareZ+")" );
		}

		public void Remove( PhysicalObject po ) {
			int squareX = (int)(po.Position.X-lowX)/Square.squareSize;
			int squareZ = (int)(po.Position.Z-lowZ)/Square.squareSize;
			InformNearby( po, new Strive.Network.Messages.ToClient.DropPhysicalObject( po ) );
			square[squareX,squareZ].Remove( po );
			physicalObjects.Remove( po.ObjectInstanceID );
			if ( po is MobileAvatar ) 
			{
				mobilesArrayList.Remove( po );
			}
			Log.LogMessage( "Removed " + po.GetType() + " " + po.ObjectInstanceID + " from the world." );
		}

		public void Relocate( PhysicalObject po, Vector3D newPosition, Vector3D newRotation ) {
			// keep everything inside world bounds
			if ( newPosition.X > highX ) {
				newPosition.X = (float)highX-1;
			}
			if ( newPosition.Z > highZ ) {
				newPosition.Z = (float)highZ-1;
			}
			if ( newPosition.X < lowX ) {
				newPosition.X = (float)lowX+1;
			}
			if ( newPosition.Z < lowZ ) {
				newPosition.Z = (float)lowZ+1;
			}

			int fromSquareX = (int)(po.Position.X - lowX)/Square.squareSize;
			int fromSquareZ = (int)(po.Position.Z - lowZ)/Square.squareSize;
			int toSquareX = (int)(newPosition.X - lowX)/Square.squareSize;
			int toSquareZ = (int)(newPosition.Z - lowZ)/Square.squareSize;
			int i, j;

			// keep everything on the ground
			// TODO: refactor below ma and overload Height
			try {
				float altitude = AltitudeAt( newPosition.X, newPosition.Z );
				if ( po is MobileAvatar ) {
					altitude += ((MobileAvatar)po).CurrentHeight/2;
				} else {
					altitude += po.Height / 2;
				}
				newPosition.Y = altitude;
			} catch ( InvalidLocationException ) {
				// disallow the relocation
				return;
			}

			MobileAvatar ma;
			if ( po is MobileAvatar ) {
				ma = (MobileAvatar)po;
			} else {
				ma = null;
			}

			// check that the object can fit there
			// TODO: revisit
			for ( i=-1; i<=1; i++ ) {
				for ( j=-1; j<=1; j++ ) {
					if (
						fromSquareX+i < 0 || fromSquareX+i >= squaresInX
						|| fromSquareZ+j < 0 || fromSquareZ+j >= squaresInZ
						|| square[toSquareX+i,toSquareZ+j] == null
					) continue;

					foreach ( PhysicalObject spo in square[toSquareX+i,toSquareZ+j].physicalObjects ) {
						// ignoring terrain and yourself
						if ( spo is Terrain || spo == po ) continue;

						// distance between two objects in 2d space
						float dx = newPosition.X - spo.Position.X;
						float dy = newPosition.Y - spo.Position.Y;
						float dz = newPosition.Z - spo.Position.Z;
						float distance_squared = dx*dx + dy*dy + dz*dz;
						if ( distance_squared <= spo.BoundingSphereRadiusSquared + po.BoundingSphereRadiusSquared ) {
							// only if not already collided!
							float dx1 = po.Position.X - spo.Position.X;
							float dy1 = po.Position.Y - spo.Position.Y;
							float dz1 = po.Position.Z - spo.Position.Z;
							float distance_squared2 = dx1*dx1 + dy1*dy1 + dz1*dz1;
							if ( distance_squared2 <= spo.BoundingSphereRadiusSquared + po.BoundingSphereRadiusSquared ) {
								continue;
							}
							if ( ma != null && ma.client != null ) {
								ma.client.Send(
									new Strive.Network.Messages.ToClient.Position( ma ) );
								return;
							}
						}
					}
				}
			}

			// send everything nearby
			//public void server_foo(float x1, float z1, float x, float z) {
			// TODO: apply -- for -ve div
			if ( ma != null && ma.client != null && po.Position != newPosition ) {
				int tx1 = Helper.DivTruncate( (int)po.Position.X, Constants.terrainPieceSize );
				int tz1 = Helper.DivTruncate( (int)po.Position.Z, Constants.terrainPieceSize );
				for ( int k=0; k<Constants.terrainZoomOrder; k++ ) {
					int chs = (int)Math.Pow(Constants.terrainHeightsPerChunk,k);
					int xradius = chs * Constants.terrainHeightsPerChunk * Constants.terrainXOrder/2;
					int zradius = chs * Constants.terrainHeightsPerChunk * Constants.terrainZOrder/2;
					int tbx = Helper.DivTruncate( (int)newPosition.X, Constants.terrainPieceSize) - xradius;
					int tbz = Helper.DivTruncate( (int)newPosition.Z, Constants.terrainPieceSize) - zradius;

					// NB: /2*2 is necessary for odd/even
					for ( i=0; i<=Constants.terrainXOrder/2*2*Constants.terrainHeightsPerChunk; i++ ) {
						for ( j=0; j<=Constants.terrainZOrder/2*2*Constants.terrainHeightsPerChunk; j++ ) {
							int tx = (tbx+i*chs);
							int tz = (tbz+j*chs);
							if ((Math.Abs(tx - tx1) > xradius) || (Math.Abs(tz - tz1) > zradius)) {
								int terrainX = tx - Helper.DivTruncate( (int)lowX, Constants.terrainPieceSize );
								int terrainZ = tz - Helper.DivTruncate( (int)lowZ, Constants.terrainPieceSize );
								if ( terrainX >= 0 && terrainX < squaresInX*Square.squareSize/Constants.terrainPieceSize && terrainZ >= 0 && terrainZ < squaresInZ*Square.squareSize/Constants.terrainPieceSize ) {
									Terrain t = terrain[ terrainX, terrainZ ];
									if ( t != null ) {
										ma.client.Send(	Strive.Network.Messages.ToClient.AddPhysicalObject.CreateMessage( t ) );
									}
								} else {
									Log.ErrorMessage( " terrainX " + terrainX + ", terrainZ " + terrainZ );
								}
							}
						}
					}
				}
			}
			//}

			po.Position = newPosition;
			po.Rotation = newRotation;

			for ( i=-1; i<=1; i++ ) {
				for ( j=-1; j<=1; j++ ) {
					if (
						Math.Abs(fromSquareX+i - toSquareX) > 1
						|| Math.Abs(fromSquareZ+j - toSquareZ) > 1
					) {
						// squares which need to have their clients
						// add or remove the object
						// as the jump has brought the object in or out of focus
/**** let the client remove them
						// remove from
						if (
							// check the square exists
							fromSquareX+i >= 0 && fromSquareX+i < squaresInX
							&& fromSquareZ+j >= 0 && fromSquareZ+j < squaresInZ
							&& square[fromSquareX+i, fromSquareZ+j] != null
						) {
							square[fromSquareX+i, fromSquareZ+j].NotifyClients(
								new Strive.Network.Messages.ToClient.DropPhysicalObject( po ) );
							// if the object is a mobile, it needs to be made aware
							// of its new world view
							if ( ma != null && ma.client != null ) {
								foreach( PhysicalObject toDrop in square[fromSquareX+i, fromSquareZ+j].physicalObjects ) {
									// Don't drop terrain, client is responsible for that
									if ( toDrop is Terrain ) continue;
									ma.client.Send(
										new Strive.Network.Messages.ToClient.DropPhysicalObject( toDrop ) );
									//Log.LogMessage( "Told client to drop " + toDrop.ObjectInstanceID + "." );
								}
							}
						}
						***/

						// add to
						if (
							// check the square exists
							toSquareX-i >= 0 && toSquareX-i < squaresInX
							&& toSquareZ-j >= 0 && toSquareZ-j < squaresInZ
							&& square[toSquareX-i, toSquareZ-j] != null
						) {
							square[toSquareX-i, toSquareZ-j].NotifyClients( Strive.Network.Messages.ToClient.AddPhysicalObject.CreateMessage( po ) );
							// if the object is a player, it needs to be made aware
							// of its new world view
							if ( ma != null && ma.client != null ) {
								foreach( PhysicalObject toAdd in square[toSquareX-i, toSquareZ-j].physicalObjects ) {
									if ( toAdd is Terrain ) {
										// TODO: take terrain out of the po list if thiw works
										continue;
									}
									ma.client.Send(	Strive.Network.Messages.ToClient.AddPhysicalObject.CreateMessage( toAdd ) );
									//Log.LogMessage( "Told client to add " + toAdd.ObjectInstanceID + "." );
								}
							}
						}
					} else {
						// clients that have the object already in scope need to be
						// told its new position
						if (
							// check the square exists
							toSquareX+i >= 0 && toSquareX+i < squaresInX
							&& toSquareZ+j >= 0 && toSquareZ+j < squaresInZ
							&& square[toSquareX+i, toSquareZ+j] != null
						) {
							if ( ma != null && ma.client != null ) {
								square[toSquareX+i, toSquareZ+j].NotifyClientsExcept(
									new Strive.Network.Messages.ToClient.Position( po ),
									ma.client );
							} else {
								square[toSquareX+i, toSquareZ+j].NotifyClients(
									new Strive.Network.Messages.ToClient.Position( po ) );
							}
						}
					}
				}
			}

			// transition the object to its new square if it changed squares
			if ( fromSquareX != toSquareX || fromSquareZ != toSquareZ ) {
				square[fromSquareX,fromSquareZ].Remove( po );
				if ( square[toSquareX,toSquareZ] == null ) {
					square[toSquareX,toSquareZ] = new Square();
				}
				square[toSquareX,toSquareZ].Add( po );
			}
		}

		public MobileAvatar LoadMobile( int instanceID ) {
			Schema.ObjectInstanceRow rpr = (Schema.ObjectInstanceRow)Global.multiverse.ObjectInstance.FindByObjectInstanceID( instanceID );
			if ( rpr == null ) return null;
			Schema.TemplateObjectRow por = Global.multiverse.TemplateObject.FindByTemplateObjectID( rpr.TemplateObjectID );
			if ( por == null ) return null;
			Schema.TemplateMobileRow mr = Global.multiverse.TemplateMobile.FindByTemplateObjectID( rpr.TemplateObjectID );
			if ( mr == null ) return null;
			return new MobileAvatar( this, mr, por, rpr );
		}

		public bool UserLookup( string email, string password, ref int playerID ) {
			Strive.Data.MultiverseFactory.refreshPlayerList( Global.multiverse );
			DataRow[] dr = Global.multiverse.Player.Select( "Email = '" + email + "'" );
			if ( dr.Length != 1 ) {
				Log.ErrorMessage( dr.Length + " players found with email '" + email + "'." );
				return false;
			} else {
				if ( String.Compare( (string)dr[0]["Password"], password ) == 0 ) {
					playerID = (int)dr[0]["PlayerID"];
					return true;
				} else {
					Log.LogMessage( "Incorrect password for player with email '" + email + "'." );
					return false;
				}
			}
		}

		public void InformNearby( PhysicalObject po, IMessage message ) {
			// notify all nearby clients
			int squareX = (int)(po.Position.X-lowX)/Square.squareSize;
			int squareZ = (int)(po.Position.Z-lowZ)/Square.squareSize;
			int i, j;
			for ( i=-1; i<=1; i++ ) {
				for ( j=-1; j<=1; j++ ) {
					// check that neigbour exists
					if (
						squareX+i < 0 || squareX+i >= squaresInX
						|| squareZ+j < 0 || squareZ+j >= squaresInZ
						|| square[squareX+i, squareZ+j] == null
					) {
						continue;
					}
					// need to send a message to all nearby clients
					// so long as the square isn't empty
					square[squareX+i, squareZ+j].NotifyClients( message );
				}
			}
		}

		public void SendInitialWorldView( Client client ) {
			// if a new client has entered the world,
			// notify them about surrounding physical objects
			// NB: this routine will send the client mobile's
			// position as one of the 'nearby' mobiles.
			Mobile mob = client.Avatar;
			client.Send( weather );
			int squareX = (int)(mob.Position.X-lowX)/Square.squareSize;
			int squareZ = (int)(mob.Position.Z-lowZ)/Square.squareSize;
			int i, j;
			if ( client != null ) {
				ArrayList nearbyPhysicalObjects = new ArrayList();
				for ( i=-1; i<=1; i++ ) {
					for ( j=-1; j<=1; j++ ) {
						// check that neigbour exists
						if (
							squareX+i < 0 || squareX+i >= squaresInX
							|| squareZ+j < 0 || squareZ+j >= squaresInZ
							|| square[squareX+i,squareZ+j] == null
						) {
							continue;
						}
						// add all neighbouring physical objects
						// to the clients world view
						foreach ( PhysicalObject p in square[squareX+i,squareZ+j].physicalObjects ) {
							nearbyPhysicalObjects.Add( p );
						}
					}
				}
				/*
				Strive.Network.Messages.ToClient.AddPhysicalObjects message = new Strive.Network.Messages.ToClient.AddPhysicalObjects(
					nearbyPhysicalObjects
				);
				client.Send( message );
				*/
				foreach ( PhysicalObject p in nearbyPhysicalObjects ) {
					client.Send( Strive.Network.Messages.ToClient.AddPhysicalObject.CreateMessage( p ) );
				}
			}
		}

		public Strive.Network.Messages.ToClient.CanPossess.id_name_tuple[] getPossessable( string username ) {
			DataRow[] dr = Global.multiverse.Player.Select( "Email = '" + username + "'" );
			Schema.PlayerRow pr = Global.multiverse.Player.FindByPlayerID( (int)dr[0][0] );
			Schema.MobilePossesableByPlayerRow [] mpbpr = pr.GetMobilePossesableByPlayerRows();
			ArrayList list = new ArrayList();
			foreach ( Schema.MobilePossesableByPlayerRow mpr in mpbpr ) {
				Strive.Network.Messages.ToClient.CanPossess.id_name_tuple tuple = new Strive.Network.Messages.ToClient.CanPossess.id_name_tuple(
					mpr.ObjectInstanceID, mpr.ObjectInstanceRow.TemplateObjectRow.TemplateObjectName
				);
				list.Add( tuple );
			}
			return (Strive.Network.Messages.ToClient.CanPossess.id_name_tuple [])list.ToArray( typeof( Strive.Network.Messages.ToClient.CanPossess.id_name_tuple ) );
		}

		public class InvalidLocationException : Exception {}
		public float AltitudeAt( float x, float z ) {
			int terrainX = Helper.DivTruncate( (int)(x - lowX), Constants.terrainPieceSize );
			int terrainZ = Helper.DivTruncate( (int)(z - lowZ), Constants.terrainPieceSize );

			// if terrain piece exists, keep everything on the ground
			if (	terrain[ terrainX, terrainZ ] != null
				&&  terrain[ terrainX+1, terrainZ ] != null
				&&  terrain[ terrainX, terrainZ+1 ] != null
				&&  terrain[ terrainX+1, terrainZ+1 ] != null 
				) {
				float dx = x - terrain[ terrainX, terrainZ ].Position.X;
				float dz = z - terrain[ terrainX, terrainZ ].Position.Z;

				// terrain is a diagonally split square, forming two triangles
				// which touch the altitude points of 4 neighbouring terrain
				// points, the current terrain and its xplus, zplus, xpluszplus.
				// so for either triangle, just apply the slope in x and z
				// to find the altitude at that point
				float xslope;
				float zslope;
				if ( dz < dx ) {
					// lower triangle
					xslope = ( terrain[ terrainX+1, terrainZ ].Position.Y - terrain[ terrainX, terrainZ ].Position.Y ) / Constants.terrainPieceSize;
					zslope = ( terrain[ terrainX+1, terrainZ+1 ].Position.Y - terrain[ terrainX+1, terrainZ ].Position.Y ) / Constants.terrainPieceSize;
				} else {
					// upper triangle
					xslope = ( terrain[ terrainX+1, terrainZ+1 ].Position.Y - terrain[ terrainX, terrainZ+1 ].Position.Y ) / Constants.terrainPieceSize;
					zslope = ( terrain[ terrainX, terrainZ+1 ].Position.Y - terrain[ terrainX, terrainZ ].Position.Y ) / Constants.terrainPieceSize;
				}
				return terrain[ terrainX, terrainZ ].Position.Y + xslope * dx + zslope * dz;
			}
			// no terrain here
			throw new InvalidLocationException();
		}
	}
}
