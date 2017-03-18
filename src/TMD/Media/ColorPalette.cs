using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using TMD.Extensions;

namespace TMD.Media
{
    public class ColorPalette
    {
        private int index = 0;
        private int colors;

        public ColorPalette(int colors)
        {
            this.colors = colors;
        }

        public Color NextColor()
        {
            return ColorExtension.FromHSV(360.0 * ((double)index++ / this.colors), 1.0, 1.0);
        }
    }
}
