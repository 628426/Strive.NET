using System;
using System.Collections;
using System.IO;

using Strive.Math3D;
using Strive.Network.Client;
using Strive.Network.Messages;
using Strive.Rendering;
using Strive.Rendering.Controls;
using Strive.Rendering.Models;
using Strive.Resources;
using Strive.Common;

namespace Strive.UI.Engine
{
	public class GameLoop
	{
		public MessageProcessor _message_processor = new MessageProcessor();
		public string modelPath;
		Scene _scene;
		System.Windows.Forms.IWin32Window _screen;
		ServerConnection _connection;
		StoppableThread thisThread;

		public GameLoop()
		{
			thisThread = new StoppableThread( new StoppableThread.WhileRunning( MainLoop ) );
		}
	

		public void Start(Scene scene, System.Windows.Forms.IWin32Window screen, ServerConnection connection)
		{
			_scene = scene;
			_screen = screen;
			_connection = connection;
			thisThread.Start();
		}

		public void Stop() {
			thisThread.Stop();
		}

		public void MainLoop()
		{
				ProcessOutstandingMessages();
				ProcessKeyboardInput();
				//ProcessMouseInput();
				ProcessAnimations();
				Render();
		}

		void ProcessOutstandingMessages() {
			while(_connection.MessageCount > 0) {
				IMessage m = _connection.PopNextMessage();
				_message_processor.Process( m );
			}
		}

		void ProcessAnimations() {
			foreach ( Model model in _scene.Models.Values ) {
				if ( model.ModelFormat == ModelFormat.MDL ) {
					model.nextFrame();
				}
			}
		}

		void ProcessMouseInput() {
			#region ProcessMouseInput
			
			bool WasMouseInput = false;
			Vector3D cameraPosition = _scene.View.Position;
			Vector3D cameraRotation = _scene.View.Rotation;

			Mouse m = Mouse.GetState();
			if( m.x != 0 ) {
				WasMouseInput = true;
				cameraRotation.Y += m.x*0.2f; 
			}
			//if( m.y != 0 ) {
				//WasMouseInput = true;
				//cameraRotation.Y -= m.y; 
			//}
			if(WasMouseInput) {
				_scene.View.Rotation = cameraRotation;
				Vector3D cameraHeading = Helper.GetHeadingFromRotation( cameraRotation );
				SendCurrentPosition();
			}
#endregion
		}

		void ProcessKeyboardInput() {
			#region 2.0 Get keyboard input 
			if ( !((System.Windows.Forms.PictureBox)_screen).Visible ) {
		//		System.Windows.Forms.MessageBox.Show("No keyboard");
				return;
			}

			bool WasKeyboardInput = false;
			const int moveunit = 5;
			Vector3D cameraPosition = _scene.View.Position;
			Vector3D cameraRotation = _scene.View.Rotation;
			Keyboard.ReadKeys();
			if(Keyboard.GetKeyState(Keys.key_W)) {
				WasKeyboardInput = true;
				cameraPosition.X +=
					(float)Math.Sin( cameraRotation.Y * Math.PI/180.0 ) * moveunit*2;
				cameraPosition.Z +=
					(float)Math.Cos( cameraRotation.Y * Math.PI/180.0 ) * moveunit*2;
			}

			if(Keyboard.GetKeyState(Keys.key_S)) {
				WasKeyboardInput = true;
				cameraPosition.X -=
					(float)Math.Sin( cameraRotation.Y * Math.PI/180.0 ) * moveunit;
				cameraPosition.Z -=
					(float)Math.Cos( cameraRotation.Y * Math.PI/180.0 ) * moveunit;
			}

			if(Keyboard.GetKeyState(Keys.key_D)) {
				WasKeyboardInput = true;
				cameraPosition.X +=
					(float)Math.Cos( cameraRotation.Y * Math.PI/180.0 ) * moveunit/2.0F;
				cameraPosition.Z -=
					(float)Math.Sin( cameraRotation.Y * Math.PI/180.0 ) * moveunit/2.0F;
			}

			if(Keyboard.GetKeyState(Keys.key_A)) {
				WasKeyboardInput = true;
				cameraPosition.X -=
					(float)Math.Cos( cameraRotation.Y * Math.PI/180.0 ) * moveunit/2.0F;
				cameraPosition.Z += 
					(float)Math.Sin( cameraRotation.Y * Math.PI/180.0 ) * moveunit/2.0F;
			}

			if(Keyboard.GetKeyState(Keys.key_Q)) {
				WasKeyboardInput = true;
				cameraRotation.Y -= 5.0F; 
			}

			if(Keyboard.GetKeyState(Keys.key_E)) {
				WasKeyboardInput = true;
				cameraRotation.Y += 5.0F;
			}

			if(Keyboard.GetKeyState(Keys.key_ESCAPE )) {
				WasKeyboardInput = true;
				System.Windows.Forms.Application.Exit();
			}				

			if(WasKeyboardInput) {
				// check that we can go there
				foreach ( Model m in _scene.Models.Values ) {
					if ( m.ModelFormat != ModelFormat.MDL ) {
						// only check other players for now
						continue;
					}
					float dx1 = _scene.View.Position.X - m.Position.X;
					float dy1 = _scene.View.Position.Y - m.Position.Y;
					float dz1 = _scene.View.Position.Z - m.Position.Z;
					float distance_squared1 = dx1*dx1 + dy1*dy1 + dz1*dz1;
					if ( distance_squared1 < m.BoundingSphereRadiusSquared + 100 ) {
						// already a collision
						continue;
					}
					float dx = cameraPosition.X - m.Position.X;
					float dy = cameraPosition.Y - m.Position.Y;
					float dz = cameraPosition.Z - m.Position.Z;
					float distance_squared = dx*dx + dy*dy + dz*dz;
					// assumes my radius is root 100
					if ( distance_squared < m.BoundingSphereRadiusSquared + 100 ) {
						Game.CurrentLog.LogMessage( "Canceled move due to collision" );
						return;
					}
				}
				_scene.View.Position = cameraPosition;
				_scene.View.Rotation = cameraRotation;
				SendCurrentPosition();
			}

				#endregion
		}

		void SendCurrentPosition() {
			Vector3D cameraHeading = Helper.GetHeadingFromRotation( _scene.View.Rotation );

			_connection.Position(_scene.View.Position.X,
				_scene.View.Position.Y,
				_scene.View.Position.Z,
				cameraHeading.X,
				cameraHeading.Y,
				cameraHeading.Z);
		}

		void Render() {
			#region 3.0 Render
			_scene.Render();
			_scene.Display();
				#endregion
		}


	}
}