using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

using Strive.Common;
using Strive.Math3D;
using Strive.Rendering;
using Strive.Resources;
using Strive.UI.WorldView;
using Strive.UI.Engine;
using Strive.Logging;
using Strive.Network.Client;

namespace Strive.UI
{
	/// <summary>
	/// Main Entry Point to the game
	/// </summary>
	public class Game
	{

		#region Public Fields

		public static ServerConnection CurrentServerConnection;// = new ServerConnection();
		public static GameLoop CurrentGameLoop;// = new GameLoop();
		public static Windows.Main CurrentMainWindow;// = new Windows.Main();
		public static World CurrentWorld;// = new Scene();
		public static Logging.Log CurrentLog;// = new Logging.Log();
		public static bool GameControlMode = false;
		public static IEngine RenderingFactory = Strive.Rendering.Activator.GetEngine();
        public static int CurrentPlayerID;
		public static DateTime now;
		public static string userName;
		public static string password;
		public static Strive.Network.Messages.NetworkProtocolType protocol;

		#endregion

		static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs args)
		{
			Exit();
		}
		static void OnApplicationExit(object sender, EventArgs args)
		{
			Exit();
		}


		static void Exit()
		{
			if(CurrentServerConnection != null)
			{
				CurrentServerConnection.Stop();

			}
			if(CurrentGameLoop != null)
			{
				CurrentGameLoop.Stop();
			}
			Application.ExitThread();
		}

		[STAThread]
		static void Main(string[] args)
		{
			// deal with exceptions
			//Application.ApplicationExit += new EventHandler(OnApplicationExit);
			//Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(OnThreadException);
			// todo: umg refactor this out of existance
			ResourceManager.factory = RenderingFactory;

			// Initialise required objects
			CurrentServerConnection = new ServerConnection();
			CurrentServerConnection.OnConnect += new ServerConnection.OnConnectHandler( HandleConnect );
			CurrentServerConnection.OnDisconnect += new ServerConnection.OnDisconnectHandler( HandleDisconnect );
			CurrentGameLoop = new GameLoop();
			CurrentWorld = new World( RenderingFactory );
			CurrentLog = new Logging.Log();
			CurrentMainWindow = new Windows.Main();
			// Configure Resource manager
			if ( System.Configuration.ConfigurationSettings.AppSettings["ResourcePath"] == null ) 
			{
				throw new System.Configuration.ConfigurationException( "ResourcePath" );
			}
			string path = System.Configuration.ConfigurationSettings.AppSettings["ResourcePath"];
			ResourceManager.SetPath( path );
			Application.Run(CurrentMainWindow);

			// must terminate all threads to quit
			CurrentGameLoop.Stop();
			CurrentServerConnection.Stop();
			CurrentWorld.Clear();
		}


		public static void Play(string ServerName, string LoginName, string Password, int Port, Strive.Network.Messages.NetworkProtocolType protocol, IWin32Window RenderTarget) 
		{
			CurrentServerConnection.protocol = protocol;
			Stop();
			CurrentWorld.InitialiseView( RenderTarget );
			Log.LogMessage( "Connecting to " + ServerName + ":" + Port );
			CurrentServerConnection.Start( new IPEndPoint( Dns.GetHostByName( ServerName ).AddressList[0], Port ) );
			CurrentGameLoop.Start(CurrentServerConnection);
			userName = LoginName;
			password = Password;
			protocol = protocol;
		}

		public static void Stop() {
			CurrentGameLoop.Stop();
			CurrentServerConnection.Stop();
			CurrentWorld.Clear();
		}

		public static void HandleConnect() {
			Strive.Logging.Log.LogMessage( "Connected." );
			CurrentServerConnection.Login(userName, password, protocol);
			password = null;
			CurrentServerConnection.RequestPossessable();
		}

		public static void HandleDisconnect() {
			Strive.Logging.Log.LogMessage( "Disconnected." );
			password = null;
			Stop();
		}
	}
}
