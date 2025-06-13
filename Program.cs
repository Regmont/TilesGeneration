using System;
using System.Collections.Generic;
using System.Linq;

namespace TileGeneration
{
    struct Point
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    class Lake
    {
        public List<Point> Contour { get; set; }
        public int[,] DepthMap { get; set; }
        public Point Center { get; set; }
        public int MinX { get; set; }
        public int MinY { get; set; }
        public int MaxX { get; set; }
        public int MaxY { get; set; }
    }

    class LakeGenerator
    {
        private readonly int width;
        private readonly int height;
        private readonly int lakeCount;
        private readonly int minLakeDistance;
        private readonly int maxDepth;
        private readonly Random rand;

        public LakeGenerator(int width, int height, int lakeCount, int minLakeDistance, int maxDepth)
        {
            this.width = width;
            this.height = height;
            this.lakeCount = lakeCount;
            this.minLakeDistance = minLakeDistance;
            this.maxDepth = maxDepth;
            rand = new Random();
        }

        public int[,] Generate()
        {
            int[,] map = new int[width, height];
            List<Lake> lakes = new List<Lake>();

            for (int i = 0; i < lakeCount; i++)
            {
                Point center;
                int attempts = 0;

                do
                {
                    center = new Point(rand.Next(width), rand.Next(height));
                    attempts++;
                } while (attempts < 100 && lakes.Any(l => Distance(center, l.Center) < minLakeDistance));

                if (attempts >= 100) continue;

                Lake lake = GenerateLake(center);
                lakes.Add(lake);
                MergeLakeToMap(map, lake);
            }

            return map;
        }

        private Lake GenerateLake(Point center)
        {
            // Генерация контура озера
            List<Point> contour = new List<Point>();
            int vertices = rand.Next(8, 13);
            double radiusX = rand.Next(8, 15);
            double radiusY = rand.Next(8, 15);

            for (int i = 0; i < vertices; i++)
            {
                double angle = 2 * Math.PI * i / vertices;
                double rX = radiusX * (1 + rand.NextDouble() * 0.4 - 0.2);
                double rY = radiusY * (1 + rand.NextDouble() * 0.4 - 0.2);
                int x = center.X + (int)(rX * Math.Cos(angle));
                int y = center.Y + (int)(rY * Math.Sin(angle));
                contour.Add(new Point(x, y));
            }

            // Определение границ озера
            int minX = contour.Min(p => p.X);
            int maxX = contour.Max(p => p.X);
            int minY = contour.Min(p => p.Y);
            int maxY = contour.Max(p => p.Y);
            int lakeWidth = maxX - minX + 1;
            int lakeHeight = maxY - minY + 1;

            // Растеризация контура
            int[,] lakeMap = new int[lakeWidth, lakeHeight];
            List<Point> lakePoints = RasterizeContour(contour, minX, maxX, minY, maxY);

            foreach (Point p in lakePoints)
            {
                int x = p.X - minX;
                int y = p.Y - minY;
                lakeMap[x, y] = 1;
            }

            // Поиск береговой линии
            List<Point> shorePoints = new List<Point>();
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int x = 0; x < lakeWidth; x++)
            {
                for (int y = 0; y < lakeHeight; y++)
                {
                    if (lakeMap[x, y] == 0) continue;
                    bool isShore = false;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dx[d];
                        int ny = y + dy[d];

                        if (nx < 0 || ny < 0 || nx >= lakeWidth || ny >= lakeHeight || lakeMap[nx, ny] == 0)
                        {
                            isShore = true;
                            break;
                        }
                    }
                    
                    if (isShore) shorePoints.Add(new Point(x, y));
                }
            }

            // Расчёт расстояний до берега
            int[,] distanceMap = new int[lakeWidth, lakeHeight];
            Queue<Point> queue = new Queue<Point>();

            for (int x = 0; x < lakeWidth; x++)
                for (int y = 0; y < lakeHeight; y++)
                    distanceMap[x, y] = -1;

            foreach (Point p in shorePoints)
            {
                distanceMap[p.X, p.Y] = 0;
                queue.Enqueue(p);
            }

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    int nx = p.X + dx[d];
                    int ny = p.Y + dy[d];

                    if (nx >= 0 && ny >= 0 && nx < lakeWidth && ny < lakeHeight &&
                        lakeMap[nx, ny] == 1 && distanceMap[nx, ny] == -1)
                    {
                        distanceMap[nx, ny] = distanceMap[p.X, p.Y] + 1;
                        queue.Enqueue(new Point(nx, ny));
                    }
                }
            }

            // Расчёт глубин
            int maxDist = 0;
            for (int x = 0; x < lakeWidth; x++)
                for (int y = 0; y < lakeHeight; y++)
                    if (distanceMap[x, y] > maxDist)
                        maxDist = distanceMap[x, y];

            double k = rand.NextDouble() * 0.7 + 1.8; // Коэффициент крутизны 1.8-2.5

            for (int x = 0; x < lakeWidth; x++)
            {
                for (int y = 0; y < lakeHeight; y++)
                {
                    if (lakeMap[x, y] == 0) continue;

                    if (distanceMap[x, y] < 0) // Для центральных точек без расстояния
                    {
                        lakeMap[x, y] = maxDepth;
                    }
                    else
                    {
                        double ratio = (double)distanceMap[x, y] / maxDist;
                        double rawDepth = maxDepth * Math.Pow(ratio, k);
                        int depth = (int)Math.Floor(rawDepth / 10) * 10;
                        lakeMap[x, y] = Math.Min(depth, maxDepth);
                    }
                }
            }

            return new Lake
            {
                Contour = contour,
                DepthMap = lakeMap,
                Center = center,
                MinX = minX,
                MinY = minY,
                MaxX = maxX,
                MaxY = maxY
            };
        }

        private List<Point> RasterizeContour(List<Point> contour, int minX, int maxX, int minY, int maxY)
        {
            List<Point> points = new List<Point>();
            int contourCount = contour.Count;

            for (int y = minY; y <= maxY; y++)
            {
                List<int> intersections = new List<int>();

                for (int i = 0; i < contourCount; i++)
                {
                    Point p1 = contour[i];
                    Point p2 = contour[(i + 1) % contourCount];

                    if ((p1.Y <= y && p2.Y > y) || (p2.Y <= y && p1.Y > y))
                    {
                        double x = p1.X + (double)(p2.X - p1.X) * (y - p1.Y) / (p2.Y - p1.Y);
                        intersections.Add((int)Math.Round(x));
                    }
                }

                intersections.Sort();

                for (int i = 0; i < intersections.Count; i += 2)
                {
                    if (i + 1 < intersections.Count)
                    {
                        int start = Math.Max(intersections[i], minX);
                        int end = Math.Min(intersections[i + 1], maxX);

                        for (int x = start; x <= end; x++)
                        {
                            points.Add(new Point(x, y));
                        }
                    }
                }
            }

            return points;
        }

        private void MergeLakeToMap(int[,] map, Lake lake)
        {
            for (int x = lake.MinX; x <= lake.MaxX; x++)
            {
                for (int y = lake.MinY; y <= lake.MaxY; y++)
                {
                    if (x < 0 || y < 0 || x >= width || y >= height) continue;

                    int lx = x - lake.MinX;
                    int ly = y - lake.MinY;

                    if (lx < lake.DepthMap.GetLength(0) && ly < lake.DepthMap.GetLength(1) && lake.DepthMap[lx, ly] > 0)
                    {
                        if (map[x, y] == 0) // Проверка перекрытий
                        {
                            map[x, y] = lake.DepthMap[lx, ly];
                        }
                    }
                }
            }
        }

        private double Distance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Параметры генерации
            const int WIDTH = 200;
            const int HEIGHT = 50;
            const int LAKE_COUNT = 10;
            const int MIN_DISTANCE = 25;
            const int MAX_DEPTH = 50; // Максимальная глубина (кратна 10)

            // Генерация карты
            LakeGenerator generator = new LakeGenerator(WIDTH, HEIGHT, LAKE_COUNT, MIN_DISTANCE, MAX_DEPTH);
            int[,] map = generator.Generate();

            // Визуализация
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Clear();

            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    int depth = map[x, y];
                    char c = ' '; // Суша

                    if (depth > 0)
                    {
                        // Подбор символов по глубине
                        if (depth < 10) c = '·';
                        else if (depth < 20) c = '░';
                        else if (depth < 30) c = '▒';
                        else if (depth < 40) c = '▓';
                        else c = '█';
                    }

                    Console.Write(c);
                }
                Console.WriteLine('|');
            }

            for (int x = 0; x <= WIDTH; x++)
            {
                Console.Write('-');
            }

            Console.WriteLine("\nЛегенда:");
            Console.WriteLine("  · : Мелководье (0-9)");
            Console.WriteLine("  ░ : Уровень 1 (10-19)");
            Console.WriteLine("  ▒ : Уровень 2 (20-29)");
            Console.WriteLine("  ▓ : Уровень 3 (30-39)");
            Console.WriteLine("  █ : Глубоководье (40+)");
            Console.WriteLine($"\nСгенерировано озер: {LAKE_COUNT}, Макс. глубина: {MAX_DEPTH}");
        }
    }
}