using System;
using System.Windows.Forms;
using System.Collections;

using Strive.Rendering;
using Strive.Rendering.Models;
using Strive.Rendering.Textures;
using Strive.Multiverse;
using Strive.Logging;
using Strive.Math3D;
using Strive.Resources;


namespace Strive.UI.WorldView {
	/// <summary>
	/// Summary description for World.
	/// </summary>
	public class World {
		public Hashtable physicalObjectInstances = new Hashtable();
		public IEngine RenderingEngine;
		public ResourceManager Resources;
		public IScene RenderingScene;
		//public ITerrain WorldTerrain;
		public TerrainCollection TerrainPieces;
		public PhysicalObjectInstance CurrentAvatar;

		EnumCameraMode cameraMode = EnumCameraMode.FirstPerson;
		Vector3D cameraPosition = new Vector3D( 0, 0, 0 );
		Vector3D cameraRotation = new Vector3D( 0, 0, 0 );
		IViewport renderViewport;
		IViewport miniMapViewport;

		public World( ResourceManager resources, IEngine engine, IWin32Window parent, IWin32Window RenderTarget, IWin32Window MiniMapTarget ) {
			Resources = resources;
			RenderingEngine = engine;
			RenderingEngine.Initialise( parent, EnumRenderTarget.PictureBox, Resolution.Automatic );
			RenderingScene = engine.CreateScene();
			renderViewport = engine.CreateViewport( RenderTarget, "RenderTarget" );
			if ( MiniMapTarget != null ) {
				this.MiniMapTarget = MiniMapTarget;
			}
			TerrainPieces = new TerrainCollection( Resources, RenderingEngine, RenderingScene );
			//WorldTerrain = RenderingEngine.GetTerrain();
			RenderingScene.SetLighting( 100 );
			RenderingScene.SetFog( 500.0f );
			renderViewport.Camera.ViewDistance = 10000;
		}

		public IWin32Window MiniMapTarget {
			set {
				if ( miniMapViewport != null ) {
					throw new Exception( "Already have set a minimap" );
				}
				miniMapViewport = RenderingEngine.CreateViewport( value, "Minimap" );
				miniMapViewport.Camera.Rotation = new Vector3D( 90, 0, 0 );
				miniMapViewport.Camera.ViewDistance = 10000;
			}
		}


		public PhysicalObjectInstance Add( PhysicalObject po ) {
			if ( po is Terrain ) {
				Terrain t = (Terrain)po;
				TerrainPieces.Add( t );
				//WorldTerrain.SetHeight( po.Position.X, po.Position.Z, po.Position.Y );
				//WorldTerrain.SetTexture( po.Position.X, po.Position.Z, Resources.GetTexture( po.ResourceID ), po.Rotation.Y );
				return null;
			} else {
				if ( RenderingScene.Models.Contains( po.ObjectInstanceID ) ) {
					Log.WarningMessage( "Trying to add existing PhysicalObject " + po.ObjectInstanceID );
					return (PhysicalObjectInstance)physicalObjectInstances[po.ObjectInstanceID];
				}
				PhysicalObjectInstance poi;
				try {
					poi = new PhysicalObjectInstance( po, Resources );
				} catch ( Exception ) {
					return null;
				}
				physicalObjectInstances.Add( po.ObjectInstanceID, poi );
				RenderingScene.Models.Add( po.ObjectInstanceID, poi.model );
				poi.model.Position = po.Position;
				poi.model.Rotation = po.Rotation;
				return poi;
			}
		}

		public void Remove( int ObjectInstanceID ) {
			//TerrainPieces.Remove( ObjectInstanceID );
			physicalObjectInstances.Remove( ObjectInstanceID );
			RenderingScene.Models.Remove( ObjectInstanceID );
		}

		public void RemoveAll() {
			TerrainPieces.Clear();
			//WorldTerrain.Clear();
			physicalObjectInstances.Clear();
			RenderingScene.Models.Clear();
		}

		public PhysicalObjectInstance Find( int ObjectInstanceID ) {
			object o = physicalObjectInstances[ObjectInstanceID];
			if ( o != null ) {
				return o as PhysicalObjectInstance;
			} else {
				return null;
			}
		}

		public void Possess( int ObjectInstanceID ) {
			Object o = physicalObjectInstances[ObjectInstanceID];
			if ( o == null ) {
				Log.ErrorMessage( "Failed to possess " + ObjectInstanceID );
				return;
			}
			CurrentAvatar = (PhysicalObjectInstance)o;
			if ( cameraMode == EnumCameraMode.FirstPerson ) {
				CurrentAvatar.model.Visible = false;
			} else {
				CurrentAvatar.model.Visible = true;
			}
			RepositionCamera();
		}

		public Vector3D CameraPosition {
			set {
				cameraPosition.Set( value );
				renderViewport.Camera.Position = cameraPosition;
				if ( miniMapViewport != null ) {
					cameraPosition.Y += 100;
					miniMapViewport.Camera.Position = cameraPosition;
				}
			}
			get{ return cameraPosition; }
		}

		public Vector3D CameraRotation {
			set {
				cameraRotation.Set( value );
				renderViewport.Camera.Rotation = cameraRotation;
			}
			get{ return cameraRotation; }
		}

		public void RepositionCamera() {
			if ( CurrentAvatar == null ) return;

			if ( cameraMode == EnumCameraMode.FirstPerson ) {
				Vector3D newPos = CurrentAvatar.model.Position.Clone();
				newPos.Y += CurrentAvatar.physicalObject.Height*2F/5F;
				CameraPosition = newPos;
				CameraRotation = CurrentAvatar.model.Rotation;
			} else if ( cameraMode == EnumCameraMode.Chase ) {
				Vector3D newPos = CurrentAvatar.model.Position.Clone();
				newPos.Y += 100;
				newPos.Z -= 100;
				CameraPosition = newPos;
				CameraRotation = new Vector3D( 45, 0, 0 );
			} else if ( cameraMode == EnumCameraMode.Free ) {
				// do nothing;
			} else {
				Log.ErrorMessage( "Unknown camera mode." );
			}
			TerrainPieces.Recenter( CurrentAvatar.model.Position.X, CurrentAvatar.model.Position.Z );
		}

		public void Render() {
			renderViewport.SetFocus();
			RenderingScene.Render();
			TerrainPieces.Render();
			RenderingScene.Display();
			if ( miniMapViewport != null ) {
				//miniMapViewport.SetFocus();
				//RenderingScene.Render();
				//RenderingScene.Display();
			}
		}

		public void Clear() {
			physicalObjectInstances.Clear();
			TerrainPieces.Clear();
			//WorldTerrain.Clear();
			CurrentAvatar = null;
			RenderingScene.DropAll();
			Resources.DropAll();
		}

		public void SetSky( ITexture texture ) {
			RenderingScene.SetSky( texture );
		}

		public void SetClouds( ITexture texture ) {
			RenderingScene.SetClouds( texture );
		}

		public EnumCameraMode CameraMode {
			get{
				return cameraMode;
			}
			set{
				if ( cameraMode != value ) {
					if ( CurrentAvatar != null ) {
						cameraMode = value;
						if ( cameraMode == EnumCameraMode.FirstPerson ) {
							CurrentAvatar.model.Visible = false;
						} else {
							CurrentAvatar.model.Visible = true;
						}
						RepositionCamera();
					} else {
						cameraMode = value;
					}
				}
			}
		}
	}
}
