﻿using Scope.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading.Tasks;
using Scope.Controls.Visualization;
using Scope.Interface.Probe;
using Scope.Properties;
using Scope.ViewModel;

namespace Scope
{
    public partial class MainWindow : Window
    {
        private MainViewModel mainViewModel;

        public MainWindow()
        {
            InitializeComponent();

            this.mainViewModel = new MainViewModel();
            this.mainViewModel.RedrawRequested += (s, e) => this.StreamGraph.Render();
            this.DataContext = this.mainViewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.mainViewModel.OnViewLoaded();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            this.mainViewModel.OnViewLoaded();
        }
    }
}
