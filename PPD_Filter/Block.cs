using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPD_Filter
{
    class Block
    {
        public enum Types { Y, U, V}
        public int Size { get; set; }
        public double[,] Matrix { get; set; }
        public Types Type { get; set; }
        public int PositionI { get; set; }
        public int PositionJ { get; set; }

        public Block(Types t, int s, double[,] m, int posi, int posj)
        {
            Type = t;
            Size = s;
            Matrix = m;
            PositionI = posi;
            PositionJ = posj;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append("i=" + PositionI + ", j=" + PositionJ + ", type=" + Type.ToString() + "\n");

            for(int i = 0; i < Size; i++)
            {
                result.Append("\t");
                for(int j = 0; j < Size; j++)
                {
                    result.Append(Matrix[i, j] + " ");
                }
                result.Append("\n");
            }

            result.Append("\n");

            return result.ToString();
        }
    }
}
