using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPD_Filter
{
    [Serializable]
    class Pixel
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public Pixel()
        {
            R = -1;
            G = -1;
            B = -1;
        }

        public Pixel(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}
