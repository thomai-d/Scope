using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using TMD.MVVM;

namespace TMD.Controls.Visualization
{
    [Serializable]
    public class ChannelConfiguration : NotifyPropertyChanged
    {
        public event EventHandler IsVisibleChanged;

        private ChannelConfiguration()
        {
            // Required for XmlSerialization.
        }

        public ChannelConfiguration(string name, Color color, double min, double max, string unit)
        {
            this.Name = name;
            this.Color = color;
            this.Unit = unit;
            this.MinValue = min;
            this.MaxValue = max;
        }

        #region NotifyProperties

        private double _Scale = 1.0;
        public double Scale
        {
            get { return _Scale; }
            set
            {
                if (value != _Scale)
                {
                    _Scale = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private double _MinValue;
        public double MinValue
        {
            get { return _MinValue; }
            set
            {
                if (value != _MinValue)
                {
                    _MinValue = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private double _MaxValue;
        public double MaxValue
        {
            get { return _MaxValue; }
            set
            {
                if (value != _MaxValue)
                {
                    _MaxValue = value;
                    this.RaisePropertyChanged();
                }
            }
        }

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

        [JsonIgnore]
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

        public override string ToString()
        {
            return $"LineConfig '{this.Name}'; {this.MinValue} - {this.MaxValue} {this.Unit}; {this.Color}";
        }

        public double RawToValue(double raw)
        {
            return (this.MinValue + raw * (this.MaxValue - this.MinValue)) * this.Scale;
        }

        public void SetCurrentValueRaw(double v)
        {
            this.CurrentValue = this.RawToValue(v);
        }
    }
}
