using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPD_Filter
{
    class Backup
    {
        public static void ReadPPMImageFromFile(PPMImage Image, String path)
        {
            List<String> before = File.ReadAllLines(path).ToList();
            string[] thirdLine = before[2].Split(' ');
            Image.Width = Convert.ToInt16(thirdLine[0]);
            Image.Height = Convert.ToInt16(thirdLine[1]);
            Image.MaxValue = Convert.ToInt16(before[3].Trim());

            Block[] blocks = RGBListToYUVBlocks(before.Skip(4).ToList(), Image.Width, Image.Height, 8);

            for (int i = 0; i < blocks.Length; i++)
            {
                if (Image.Blocks[i].Type == Block.Types.None)
                {
                    Image.Blocks[i] = blocks[i];
                }
            }

            //Image.Blocks = blocks;
        }

        public static void GenerateGrayscaleImage(PPMImage Image, string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Image.FirstComment);
            sb.Append('\n');
            sb.Append(Image.SecondComment);
            sb.Append('\n');
            sb.Append(Image.Width + " " + Image.Height);
            sb.Append('\n');
            sb.Append(Image.MaxValue);
            sb.Append('\n');
            sb.Append(YUVBlocksToGrayscaleString(Image));

            File.WriteAllText(path, sb.ToString());
        }

        public static void GenerateRegularImage(PPMImage Image, string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Image.FirstComment);
            sb.Append('\n');
            sb.Append(Image.SecondComment);
            sb.Append('\n');
            sb.Append(Image.Width + " " + Image.Height);
            sb.Append('\n');
            sb.Append(Image.MaxValue);
            sb.Append('\n');
            sb.Append(YUVBlocksToRGBString(Image));

            File.WriteAllText(path, sb.ToString());
        }

        private static double[,] DivideIntoBlockForIndex(double[,] matrix, int line, int column, int blockSize)
        {
            double[,] blockValues = new double[blockSize, blockSize];
            for (int k = line; k < line + blockSize; k++)
            {
                for (int l = column; l < column + blockSize; l++)
                    blockValues[k - line, l - column] = matrix[k, l];
            }
            return blockValues;
        }

        public static Block[] DivideIntoBlocks(double[,] yMatrix, double[,] uMatrix, double[,] vMatrix, int blockSize)
        {
            List<Block> blocks = new List<Block>();
            //Block[] blocks = new Block[];

            for (int i = 0; i < yMatrix.GetLength(0); i = i + blockSize)
                for (int j = 0; j < yMatrix.GetLength(1); j = j + blockSize)
                {
                    blocks.Add(new Block(Block.Types.Y, blockSize, DivideIntoBlockForIndex(yMatrix, i, j, blockSize), i, j));
                    blocks.Add(new Block(Block.Types.U, blockSize, DivideIntoBlockForIndex(uMatrix, i, j, blockSize), i, j));
                    blocks.Add(new Block(Block.Types.U, blockSize, DivideIntoBlockForIndex(vMatrix, i, j, blockSize), i, j));
                }

            return blocks.ToArray();
        }

        public static void PopulateYUVMatrices(double[,] yMatrix, double[,] uMatrix, double[,] vMatrix, List<String> values, int height, int width)
        {
            double y, u, v;

            int k = 0;

            for (int i = 0; i < values.Count; i = i + 3)
            {
                y = 0.299 * Convert.ToInt16(values[i]) + 0.587 * Convert.ToInt16(values[i + 1]) +
                    0.114 * Convert.ToInt16(values[i + 2]);

                u = 128 - 0.1687 * Convert.ToInt16(values[i]) - 0.3312 * Convert.ToInt16(values[i + 1]) +
                    0.5 * Convert.ToInt16(values[i + 2]);

                v = 128 + 0.5 * Convert.ToInt16(values[i]) - 0.4186 * Convert.ToInt16(values[i + 1]) -
                    0.0813 * Convert.ToInt16(values[i + 2]);

                yMatrix[k / width, k % width] = y;
                uMatrix[k / width, k % width] = u;
                vMatrix[k / width, k % width] = v;

                k++;
            }
        }

        public static Block[] RGBListToYUVBlocks(List<String> values, int width, int height, int blockDimension)
        {
            Block[] blocks = new PPD_Filter.Block[(3 * width * height) / blockDimension];

            double[,] yMatrix = new double[height, width];
            double[,] uMatrix = new double[height, width];
            double[,] vMatrix = new double[height, width];

            PopulateYUVMatrices(yMatrix, uMatrix, vMatrix, values, height, width);

            ////Split the matrices into 8x8 blocks
            //for (int blockLine = 0; blockLine < height / 8; blockLine++)
            //{
            //    for (int blockColumn = 0; blockColumn < width / 8; blockColumn++)
            //    {
            //        double[,] my = new double[8, 8];
            //        double[,] mu = new double[8, 8];
            //        double[,] mv = new double[8, 8];
            //        int mi = 0, mj = 0;

            //        for (int i = blockLine * 8; i < blockLine * 8 + 8; i++)
            //        {
            //            mj = 0;
            //            for (int j = blockColumn * 8; j < blockColumn * 8 + 8; j++)
            //            {
            //                my[mi, mj] = yMatrix[i, j];
            //                mu[mi, mj] = uMatrix[i, j];
            //                mv[mi, mj] = vMatrix[i, j];
            //                mj++;
            //            }
            //            mi++;
            //        }

            //        Block by = new Block(Block.Types.Y, 8, my, blockLine * 8, blockColumn * 8);
            //        Block bu = new Block(Block.Types.U, 8, mu, blockLine * 8, blockColumn * 8);
            //        Block bv = new Block(Block.Types.V, 8, mv, blockLine * 8, blockColumn * 8);

            //        blocks.Add(by);
            //        blocks.Add(bu);
            //        blocks.Add(bv);
            //    }
            //}

            blocks = DivideIntoBlocks(yMatrix, uMatrix, vMatrix, 8);

            return blocks.ToArray();
        }

        public static int Clamp(double val, int maxValue)
        {
            if (val < 0)
            {
                return 0;
            }
            else if (val > maxValue)
            {
                return maxValue;
            }
            else
            {
                return (int)val;
            }
        }

        private static Pixel[,] YUVBlocksToPixelMatrix(PPMImage Image)
        {
            Pixel[,] final = new Pixel[Image.Height, Image.Width];

            int size = Communicator.world.Size - 1;
            int rank = Communicator.world.Rank - 1;

            int k = 0;

            for (int i = 0; i < Image.Blocks.Length; i = i + 3)
            {
                if (k % size == rank)
                {
                    double[,] yMatrix = Image.Blocks[i].Matrix;
                    double[,] uMatrix = Image.Blocks[i + 1].Matrix;
                    double[,] vMatrix = Image.Blocks[i + 2].Matrix;
                    int starti = Image.Blocks[i].PositionI;
                    int startj = Image.Blocks[i].PositionJ;

                    for (int mi = 0; mi < yMatrix.GetLength(0); mi++)
                    {
                        for (int mj = 0; mj < yMatrix.GetLength(1); mj++)
                        {
                            double c = yMatrix[mi, mj] - 16;
                            double d = uMatrix[mi, mj] - 128;
                            double e = vMatrix[mi, mj] - 128;
                            int r = Clamp((int)(298 * c + 409 * e + 128) >> 8, Image.MaxValue);
                            int g = Clamp((int)(298 * c - 100 * d - 208 * e + 128) >> 8, Image.MaxValue);
                            int b = Clamp((int)(298 * c + 516 * d + 128) >> 8, Image.MaxValue);

                            final[starti + mi, startj + mj] = new Pixel(r, g, b);
                        }
                    }
                }
                k++;
            }

            return final;
        }

        private static Pixel[,] YUVBlocksToGrayscalePixelMatrix(PPMImage Image)
        {
            Pixel[,] final = new Pixel[Image.Height, Image.Width];

            int k = 0;

            int size = Communicator.world.Size - 1;
            int rank = Communicator.world.Rank - 1;

            for (int i = 0; i < Image.Blocks.Length; i = i + 3)
            {
                if (k % size == rank)
                {
                    double[,] yMatrix = Image.Blocks[i].Matrix;
                    int starti = Image.Blocks[i].PositionI;
                    int startj = Image.Blocks[i].PositionJ;

                    for (int mi = 0; mi < yMatrix.GetLength(0); mi++)
                    {
                        for (int mj = 0; mj < yMatrix.GetLength(1); mj++)
                        {
                            double c = yMatrix[mi, mj] - 16;
                            int r = Clamp((int)(298 * c + 128) >> 8, Image.MaxValue);
                            int g = Clamp((int)(298 * c + 128) >> 8, Image.MaxValue);
                            int b = Clamp((int)(298 * c + 128) >> 8, Image.MaxValue);

                            final[starti + mi, startj + mj] = new Pixel(r, g, b);
                        }
                    }
                }
                k++;
            }

            return final;
        }

        public static String PixelMatrixToRGBString(Pixel[,] final)
        {
            StringBuilder result = new StringBuilder();

            foreach (Pixel p in final)
            {
                result.Append('\n');
                result.Append(p.R);
                result.Append('\n');
                result.Append(p.G);
                result.Append('\n');
                result.Append(p.B);
            }

            return result.ToString();
        }

        public static String PixelMatrixToGrayscaleString(Pixel[,] final)
        {
            StringBuilder result = new StringBuilder();

            foreach (Pixel p in final)
            {
                result.Append('\n');
                result.Append(p.R);
                result.Append('\n');
                result.Append(p.G);
                result.Append('\n');
                result.Append(p.B);
            }

            return result.ToString();
        }

        private static PPMImage MergeImages()
        {
            PPMImage Image = Communicator.world.Receive<PPMImage>(1, 0);
            for (int i = 2; i < Communicator.world.Size; i++)
            {
                PPMImage img = Communicator.world.Receive<PPMImage>(i, 0);
                for (int j = 0; j < img.Blocks.Length; j++)
                {
                    if (img.Blocks[j].Type != Block.Types.None)
                    {
                        Image.Blocks[j] = img.Blocks[j];
                    }
                }
            }
            return Image;
        }
    }
}
