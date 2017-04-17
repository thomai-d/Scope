using TMD.Controls.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMD.MVVM;

namespace Scope.UI.ViewModel
{
    public class ChannelConfigurationViewModel : ViewModelBase
    {
        public ChannelConfigurationViewModel(ChannelConfiguration config)
        {
            this.Configuration = config;
        }

        public ChannelConfiguration Configuration { get; private set; }
    }
}
