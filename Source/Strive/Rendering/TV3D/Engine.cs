using System;
using System.Threading;
using System.Windows.Forms;

using Strive.Rendering;
using Strive.Rendering.Controls;
using Strive.Rendering.Models;
using Strive.Rendering.Textures;
using Strive.Rendering.TV3D.Models;
using Strive.Math3D;
using TrueVision3D;

namespace Strive.Rendering.TV3D {
	/// <summary>
	/// Creates intances of the other classes in Rendering
	/// </summary>
	public class Engine : IEngine {
		IMouse mouse = new Controls.Mouse();
		IKeyboard keyboard = new Controls.Keyboard();

		static internal TVEngine TV3DEngine;
		static internal TVInputEngine Input;
		static internal TVScene TV3DScene;
		static internal TVLandscape Land;
		static internal TVTextureFactory TexFactory;
		static internal TVScreen2DImmediate Screen2DImmediate;
		static internal TVScreen2DText Screen2DText;
		static internal TVLightEngine LightEngine;
		static internal TVGlobals Gl;
		static internal TVCamera Camera;
		static internal TVAtmosphere Atmosphere;

		IWin32Window _renderTarget;

		public Engine() {
		}


		public IScene CreateScene() {
			return new Scene();
		}

		public ITerrain CreateTerrain( string name, ITexture texture, float y, float xy, float zy, float xzy ) {
			return Terrain.CreateTerrain( name, texture, y, xy, zy, xzy );
		}
		public IActor LoadActor(string name, string path ) {
			return Actor.LoadActor( name, path );
		}
		public IModel LoadStaticModel(string name, string path ) {
			return Model.LoadStaticModel( name, path );
		}

		public ITexture LoadTexture( string name, string path ) {
			return Textures.Texture.LoadTexture( name, path );
		}


		public IMouse Mouse {
			get { return mouse; }
		}
		public IKeyboard Keyboard {
			get { return keyboard; }
		}

		/// <summary>
		/// Initialise the scene
		/// </summary>
		/// <param name="window">The IWin32Window to render to.  System.Windows.Forms.Form implements IWin32Window</param>
		/// <param name="target">The render target</param>
		/// <param name="resolution">The resolution to render in</param>
		public void Initialise(IWin32Window window, EnumRenderTarget target, Resolution resolution) {
			TV3DEngine = new TVEngine();
			try {
				Engine.TV3DEngine.Init3DWindowedMode(window.Handle.ToInt32(), true);
				_renderTarget = window;
			}
			catch(Exception e) {
				throw new EngineInitialisationException(e);
			}
			TV3DEngine.SetAngleSystem( CONST_TV_ANGLE.TV_ANGLE_DEGREE );
			TV3DScene = new TVScene();
			Land = new TVLandscape();
			TexFactory = new TVTextureFactory();
			Screen2DImmediate = new TVScreen2DImmediate();
			Screen2DText = new TVScreen2DText();
			LightEngine = new TVLightEngine();
			Gl = new TVGlobals();
			Camera = new TVCamera();
			Atmosphere = new TVAtmosphere();
			Input = new TVInputEngine();
			
		}
		public void Terminate() {
			TV3DEngine = null;
		}
		public IWin32Window RenderTarget {
			get {
				return _renderTarget;
			}
		}

	}
}
