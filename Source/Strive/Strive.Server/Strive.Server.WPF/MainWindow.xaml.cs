﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using UpdateControls.XAML;

using Strive.WPF;

namespace Strive.Server.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var view = new LogView();
            view.DataContext = ForView.Wrap(new LogViewModel(App.LogModel));
            view.ShowAsDocument(dockManager);
            view.Focus();
        }
    }
}
