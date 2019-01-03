using MPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PPD_Filter
{
    class Program
    {
        private static void GenerateFullImage(PPMImage image)
        {
             PPMOperations.GenerateImage(image, "after.ppm");
        }

        private static void GenerateFilteredImage(PPMImage image, string filter)
        {
            //add if statement for the new filter

            if (filter == "3")
            {
                double[,] filterMatrix =
                {
                  { 1, 0, 0, 0, 0, 0, 0, 0, 0 },
                  { 0, 1, 0, 0, 0, 0, 0, 0, 0 },
                  { 0, 0, 1, 0, 0, 0, 0, 0, 0 },
                  { 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                  { 0, 0, 0, 0, 1, 0, 0, 0, 0 },
                  { 0, 0, 0, 0, 0, 1, 0, 0, 0 },
                  { 0, 0, 0, 0, 0, 0, 1, 0, 0 },
                  { 0, 0, 0, 0, 0, 0, 0, 1, 0 },
                  { 0, 0, 0, 0, 0, 0, 0, 0, 1 },
                };

                double filterFactor = 1.0 / 9.0;
                double filterBias = 0.0;

                PPMOperations.GenerateFilteredImage(image, filterMatrix, filterFactor, filterBias, "after.ppm");
            }
            else if(filter == "4")
            {
                double[,] filterMatrix =
                {
                   { 0,  0, -1,  0,  0 },
                   { 0,  0, -1,  0,  0 },
                   { 0,  0,  2,  0,  0 },
                   { 0,  0,  0,  0,  0 },
                   { 0,  0,  0,  0,  0 },
                };

                double filterFactor = 1.0;
                double filterBias = 0.0;

                PPMOperations.GenerateFilteredImage(image, filterMatrix, filterFactor, filterBias, "after.ppm");
            }
        }

        private static void Master()
        {
            bool choosePictureAlive = true;

            while (choosePictureAlive)
            {
                bool chooseFilterAlive = true;

                Console.Write("Path to picture: ");
                string path = Console.ReadLine();

                PPMImage image = PPMOperations.ReadPPMImageFromFile(path);
                PPMOperations.Broadcast(image);

                while (chooseFilterAlive)
                {
                    //add filter in menu

                    Console.Write("Choose filter: \n\t'1'-Regular\n\t'2'-Grayscale" +
                    "\n\t'3'-Blur\n\t'4'-Find Edges" +
                    "\n\t'5'-Choose different picture\n\t'0'-Exit\nFilter: ");
                    string filter = Console.ReadLine();
                    switch (filter)
                    {
                        case "1":
                        case "2":
                            PPMOperations.Broadcast(filter);
                            GenerateFullImage(image);
                            break;
                        case "3":
                        case "4":
                            PPMOperations.Broadcast(filter);
                            GenerateFilteredImage(image, filter);
                            break;
                        case "5":
                            PPMOperations.Broadcast(filter);
                            chooseFilterAlive = false;
                            break;
                        case "0":
                            PPMOperations.Broadcast(filter);
                            chooseFilterAlive = false;
                            choosePictureAlive = false;
                            break;
                        default:
                            Console.WriteLine("Invalid command.\n");
                            break;
                    }
                }
            }
        }

        private static void Worker()
        {
            bool choosePictureAlive = true;

            while (choosePictureAlive)
            {
                bool chooseFilterAlive = true;

                PPMImage image = Communicator.world.Receive<PPMImage>(0, 0);

                //add filter in menu

                while (chooseFilterAlive)
                {
                    string filter = Communicator.world.Receive<string>(0, 0);
                    switch (filter)
                    {
                        case "1":
                            PPMOperations.GenerateRegularImage(image);
                            break;
                        case "2":
                            PPMOperations.GenerateGrayscaleImage(image);
                            break;
                        case "3":
                            PPMOperations.GenerateBlurImage(image);
                            break;
                        case "4":
                            PPMOperations.GenerateEdgeImage(image);
                            break;
                        case "5":
                            chooseFilterAlive = false;
                            break;
                        case "0":
                            chooseFilterAlive = false;
                            choosePictureAlive = false;
                            break;
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                if (Communicator.world.Rank == 0)
                {
                    Master();
                }
                else
                {
                    Worker();
                }
            }
        }
    }
}
