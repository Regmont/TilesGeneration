using System;

namespace TilesGeneration
{
    class Tile
    {
        public int Type { get; set; }
        //0 - water
        //1 - land

        public int CountLeft { get; set; } = 0;
        public int CountUp { get; set; } = 0;
    }

    class Program
    {
        const string landTile = "▓";
        const string waterTile = "░";
        const string unknownTile = "?";

        static void CreateTiles(Tile[,] tiles, float lwp, float wwp)
        {
            int height = tiles.GetLength(0);
            int width = tiles.GetLength(1);

            float currentProb = 0;
            Random random = new Random();

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (i == 0 && j == 0)
                    {
                        if (random.Next(2) == 0)
                        {
                            tiles[i, j].Type = 0;
                            tiles[i, j].CountLeft++;
                            tiles[i, j].CountUp++;
                        }
                        else
                        {
                            tiles[i, j].Type = 1;
                        }

                        continue;
                    }

                    if (i != 0)
                    {
                        if (tiles[i - 1, j].Type == 1)
                        {
                            currentProb += lwp;
                        }
                        else
                        {
                            currentProb += wwp;
                        }
                    }

                    if (j != 0)
                    {
                        if (tiles[i, j - 1].Type == 1)
                        {
                            currentProb += lwp;
                        }
                        else
                        {
                            currentProb += wwp;
                        }
                    }

                    if (random.Next(10) / 10f < currentProb)
                    {
                        tiles[i, j].Type = 0;
                    }
                    else
                    {
                        tiles[i, j].Type = 1;
                    }

                    currentProb = 0;
                }
            }
        }

        static void PrintTiles(Tile[,] tiles)
        {
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    switch (tiles[i, j].Type)
                    {
                        case 0:
                            Console.Write(waterTile);
                            break;
                        case 1:
                            Console.Write(landTile);
                            break;
                        default:
                            Console.Write(unknownTile);
                            break;
                    }
                }
                Console.WriteLine();
            }
        }

        static void Main(string[] args)
        {
            int height = 10;
            int width = 10;
            Tile[,] tiles = new Tile[height, width];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    tiles[i, j] = new Tile();
                }
            }

            float landWaterProb = 0.1f;
            float waterWaterProb = 0.4f;

            CreateTiles(tiles, landWaterProb, waterWaterProb);
            PrintTiles(tiles);
        }
    }
}

//countLeft, countUp - the amount of water tiles including the current one