using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPD_Filter
{
    class PPMImage
    {
        public string FirstComment { get; set; }
        public string SecondComment { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MaxValue { get; set; }
        public List<Block> Blocks { get; set; }

        public PPMImage(string firstComment, string secondComment, int width, int height, int max, List<Block> blocks)
        {
            FirstComment = firstComment;
            SecondComment = secondComment;
            Width = width;
            Height = height;
            MaxValue = max;
            Blocks = blocks;
        }
    }
}
