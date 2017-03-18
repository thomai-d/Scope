using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMD.MVVM;

namespace Scope.UI.Controls.Visualization
{
    public class Annotation : NPCBase
    {
        private double _X;
        public double X
        {
            get { return _X; }
            set
            {
                if (value != _X)
                {
                    _X = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private double _Y;
        public double Y
        {
            get { return _Y; }
            set
            {
                if (value != _Y)
                {
                    _Y = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        private string _Text;
        public string Text
        {
            get { return _Text; }
            set
            {
                if (value != _Text)
                {
                    _Text = value;
                    this.RaisePropertyChanged();
                }
            }
        }
    }
}
