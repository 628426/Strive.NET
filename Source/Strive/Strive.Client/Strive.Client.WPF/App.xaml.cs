﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Reflection;

using Common.Logging;

using Strive.Client.NeoAxisView;
using Strive.Client.ViewModel;
using Strive.Client.Model;
using Strive.Network.Client;
using Strive.WPF;
using Strive.Server.Logic;


namespace Strive.Client.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static ILog Log = LogManager.GetCurrentClassLogger();
        public static WorldViewModel WorldViewModel;
        public static ServerConnection ServerConnection;
        public static DictionaryModel<string, EntityModel> WorldModel;
        public static ConnectionHandler ConnectionHandler;
        public static LogModel LogModel;
        public static Engine ServerEngine = new Engine();

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            LogModel = new LogModel();
            Log.Info("Starting " + Assembly.GetExecutingAssembly().GetName().FullName);

            ServerConnection = new ServerConnection();
            WorldModel = new DictionaryModel<string, EntityModel>(); 
            WorldViewModel = new WorldViewModel(WorldModel);
            ConnectionHandler = new ConnectionHandler(ServerConnection, WorldModel);
        }

        private bool ReportException(Exception ex)
        {
            Log.Fatal("Uncaught Exception", ex);
            using (var d = new System.Windows.Forms.ThreadExceptionDialog(ex))
            {
                return d.ShowDialog() == System.Windows.Forms.DialogResult.Abort;
            }
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (ReportException(e.Exception))
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = true;
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportException(e.ExceptionObject as Exception);
        }
    }
}
