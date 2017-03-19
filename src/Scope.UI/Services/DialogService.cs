using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TMD.MVVM;

namespace Scope.UI.Services
{
    public static class DialogService
    {
        public static void ShowDialog<T>(ViewModelBase viewModel)
            where T: Window, new()
        {
            var dialogView = new T();

            EventHandler closeHandler = (s, e) => { dialogView.Close(); };

            viewModel.OnViewLoaded();

            try
            {
                viewModel.CloseRequested += closeHandler;
                dialogView.DataContext = viewModel;
                dialogView.ShowDialog();
            }
            finally
            {
                viewModel.CloseRequested -= closeHandler;
                viewModel.OnViewUnloaded();
            }            
        }
    }
}
