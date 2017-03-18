using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TMD.MVVM;

namespace Scope.UI.Controls.Visualization
{
    public class LineConfiguration : NPCBase
    {
        public event EventHandler IsVisibleChanged;

        #region NotifyProperties

        private Color _Color;
        public Color Color
        {
            get { return _Color; }
            set
            {
                if (value != _Color)
                {
                    _Color = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private string _Name;
        public string Name
        {
            get { return _Name; }
            set
            {
                if (value != _Name)
                {
                    _Name = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private double _CurrentValue;
        public double CurrentValue
        {
            get { return _CurrentValue; }
            set
            {
                if (value != _CurrentValue)
                {
                    _CurrentValue = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private string _Unit;
        public string Unit
        {
            get { return _Unit; }
            set
            {
                if (value != _Unit)
                {
                    _Unit = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private bool _IsVisible = true;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set
            {
                if (value != _IsVisible)
                {
                    _IsVisible = value;
                    this.IsVisibleChanged?.Invoke(this, EventArgs.Empty);
                    this.RaisePropertyChanged();
                }
            }
        }

        #endregion
    }
}
