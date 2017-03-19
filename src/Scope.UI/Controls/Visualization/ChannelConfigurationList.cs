using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Scope.UI.Controls.Visualization
{
    public class ChannelConfigurationList : List<ChannelConfiguration>
    {
        public static ChannelConfigurationList FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ChannelConfigurationList>(json);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
