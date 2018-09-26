using System;
using System.Collections.Generic;
using System.Windows;
using SlimDX.DirectInput;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.ComponentModel;

namespace Enjoy
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var mvm = DataContext as MainViewModel;
            mvm.Device1.Dispose();
            mvm.Device2.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow page = new HelpWindow();
            page.Show();
        }
    }
}
