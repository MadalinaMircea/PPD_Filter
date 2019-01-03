using MPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPD_Filter
{
    [Serializable]
    class PPMOperations
    {
        public PPMOperations()
        {
        }

        #region HelpMethods

        public static int Clamp(double val, int minValue, int maxValue)
        {
            if (val < minValue)
            {
                return minValue;
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

        #endregion

        #region MasterMethods
        public static PPMImage ReadPPMImageFromFile(String path)
        {
            List<String> before = File.ReadAllLines(path).ToList();
            string[] thirdLine = before[2].Split(' ');
            int width = Convert.ToInt16(thirdLine[0]);
            int height = Convert.ToInt16(thirdLine[1]);
            int maxValue = Convert.ToInt16(before[3].Trim());

            Block[] blocks = RGBListToYUVBlocks(before.Skip(4).ToList(), width, height, 8).ToArray();

            PPMImage image = new PPMImage(before[0], before[1], width, height, maxValue, blocks);

            return image;
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

        private static List<Block> DivideIntoBlocks(double[,] yMatrix, double[,] uMatrix, double[,] vMatrix, int blockSize)
        {
            List<Block> blocks = new List<Block>();

            for (int i = 0; i < yMatrix.GetLength(0); i = i + blockSize)
                for (int j = 0; j < yMatrix.GetLength(1); j = j + blockSize)
                {
                    blocks.Add(new Block(Block.Types.Y, blockSize, DivideIntoBlockForIndex(yMatrix, i, j, blockSize), i, j));
                    blocks.Add(new Block(Block.Types.U, blockSize, DivideIntoBlockForIndex(uMatrix, i, j, blockSize), i, j));
                    blocks.Add(new Block(Block.Types.U, blockSize, DivideIntoBlockForIndex(vMatrix, i, j, blockSize), i, j));
                }

            return blocks;
        }

        private static void PopulateYUVMatrices(double[,] yMatrix, double[,] uMatrix, double[,] vMatrix, List<String> values, int height, int width)
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

        private static List<Block> RGBListToYUVBlocks(List<String> values, int width, int height, int blockDimension)
        {
            List<Block> blocks = new List<Block>();

            double[,] yMatrix = new double[height, width];
            double[,] uMatrix = new double[height, width];
            double[,] vMatrix = new double[height, width];

            PopulateYUVMatrices(yMatrix, uMatrix, vMatrix, values, height, width);
            blocks = DivideIntoBlocks(yMatrix, uMatrix, vMatrix, 8);

            return blocks;
        }
        
        public static void Broadcast<T>(T message)
        {
            for (int i = 1; i < Communicator.world.Size; i++)
                Communicator.world.Send<T>(message, i, 0);
        }

        private static Pixel[,] ApplyFilter(Pixel[,] matrix, double[,] filterMatrix, double filterFactor, double filterBias)
        {
            int width = matrix.GetLength(1);
            int height = matrix.GetLength(0);
            int filterWidth = filterMatrix.GetLength(1);
            int filterHeight = filterMatrix.GetLength(0);

            Pixel[,] finalMatrix = new Pixel[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    double red = 0.0, green = 0.0, blue = 0.0;

                    for (int filterY = 0; filterY < filterMatrix.GetLength(1); filterY++)
                    {
                        for (int filterX = 0; filterX < filterMatrix.GetLength(0); filterX++)
                        {
                            int imageX = (x - filterWidth / 2 + filterX + width) % width;
                            int imageY = (y - filterHeight / 2 + filterY + height) % height;
                            Pixel pixel = matrix[imageY, imageX];

                            red += pixel.R * filterMatrix[filterY, filterX];
                            green += pixel.G * filterMatrix[filterY, filterX];
                            blue += pixel.B * filterMatrix[filterY, filterX];
                        }

                        finalMatrix[y, x] = new Pixel(
                            Clamp(filterFactor * red + filterBias, 0, 255),
                            Clamp(filterFactor * green + filterBias, 0, 255),
                            Clamp(filterFactor * blue + filterBias, 0, 255));
                    }
                }
            }

            return finalMatrix;
        }

        public static void GenerateFilteredImage(PPMImage image, double[,] filterMatrix, double filterFactor, double filterBias, string outputPath)
        {
            Pixel[,] matrix = MergePixelMatrices();

            Pixel[,] finalMatrix = ApplyFilter(matrix, filterMatrix, filterFactor, filterBias);
            StringBuilder sb = new StringBuilder();
            sb.Append(image.FirstComment);
            sb.Append('\n');
            sb.Append(image.SecondComment);
            sb.Append('\n');
            sb.Append(image.Width + " " + image.Height);
            sb.Append('\n');
            sb.Append(image.MaxValue);
            sb.Append(PixelMatrixToString(finalMatrix));
            WriteToFile(outputPath, sb.ToString());
        }

        public static void GenerateImage(PPMImage image, string outputPath)
        {
            Pixel[,] matrix = MergePixelMatrices();
            StringBuilder sb = new StringBuilder();
            sb.Append(image.FirstComment);
            sb.Append('\n');
            sb.Append(image.SecondComment);
            sb.Append('\n');
            sb.Append(image.Width + " " + image.Height);
            sb.Append('\n');
            sb.Append(image.MaxValue);
            sb.Append(PixelMatrixToString(matrix));
            WriteToFile(outputPath, sb.ToString());
        }

        private static Pixel[,] MergePixelMatrices()
        {
            Pixel[,] matrix = Communicator.world.Receive<Pixel[,]>(1, 0);
            for (int i = 2; i < Communicator.world.Size; i++)
            {
                Pixel[,] matrix2 = Communicator.world.Receive<Pixel[,]>(i, 0);
                for (int line = 0; line < matrix2.GetLength(0); line++)
                    for (int col = 0; col < matrix2.GetLength(1); col++)
                        //if (matrix2[line, col].R != -1)
                        if (matrix2[line, col] != null)
                            matrix[line, col] = matrix2[line, col];
            }
            return matrix;
        }

        private static string PixelMatrixToString(Pixel[,] final)
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

        private static void WriteToFile(string path, string text)
        {
            File.WriteAllText(path, text);
        }
        #endregion


        #region WorkerMethods

        private static Pixel[,] YUVBlocksToPixelMatrix(PPMImage Image)
        {
            Pixel[,] final = new Pixel[Image.Height, Image.Width];

            int step = 0;
            int rank = Communicator.world.Rank - 1;
            int size = Communicator.world.Size - 1;

            for (int i = 0; i < Image.Blocks.Count(); i = i + 3)
            {
                if (step % size == rank)
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
                            int r = Clamp((int)(298 * c + 409 * e + 128) >> 8, 0, 255);
                            int g = Clamp((int)(298 * c - 100 * d - 208 * e + 128) >> 8, 0, 255);
                            int b = Clamp((int)(298 * c + 516 * d + 128) >> 8, 0, 255);

                            final[starti + mi, startj + mj] = new Pixel(r, g, b);
                        }
                    }
                }

                step++;
            }

            return final;
        }

        private static Pixel[,] YUVBlocksToGrayscalePixelMatrix(PPMImage Image)
        {
            Pixel[,] final = new Pixel[Image.Height, Image.Width];

            int step = 0;
            int rank = Communicator.world.Rank - 1;
            int size = Communicator.world.Size - 1;

            for (int i = 0; i < Image.Blocks.Count(); i = i + 3)
            {
                if (rank == step % size)
                {
                    double[,] yMatrix = Image.Blocks[i].Matrix;
                    int starti = Image.Blocks[i].PositionI;
                    int startj = Image.Blocks[i].PositionJ;

                    for (int mi = 0; mi < yMatrix.GetLength(0); mi++)
                    {
                        for (int mj = 0; mj < yMatrix.GetLength(1); mj++)
                        {
                            double c = yMatrix[mi, mj] - 16;
                            int r = Clamp((int)(298 * c + 128) >> 8, 0, 255);
                            int g = Clamp((int)(298 * c + 128) >> 8, 0, 255);
                            int b = Clamp((int)(298 * c + 128) >> 8, 0, 255);

                            final[starti + mi, startj + mj] = new Pixel(r, g, b);
                        }
                    }
                }

                step++;
            }

            return final;
        }

        public static void GenerateRegularImage(PPMImage image)
        {
            Pixel[,] matrix = YUVBlocksToPixelMatrix(image);

            Communicator.world.Send<Pixel[,]>(matrix, 0, 0);
        }

        public static void GenerateGrayscaleImage(PPMImage image)
        {
            Pixel[,] matrix = YUVBlocksToGrayscalePixelMatrix(image);

            Communicator.world.Send<Pixel[,]>(matrix, 0, 0);
        }

        public static void GenerateBlurImage(PPMImage image)
        {
            Pixel[,] matrix = YUVBlocksToPixelMatrix(image);

            Communicator.world.Send<Pixel[,]>(matrix, 0, 0);
        }

        public static void GenerateEdgeImage(PPMImage image)
        {
            Pixel[,] matrix = YUVBlocksToPixelMatrix(image);

            Communicator.world.Send<Pixel[,]>(matrix, 0, 0);
        }

        //add new filter worker function

        #endregion
    }
}
