using System;
using System.Collections.Generic;
using System.Linq;

namespace MUD.Rulesets.D20.GameSystems
{
    public struct Point
    {
        public int X;
        public int Y;
        public Point(int x, int y) { X = x; Y = y; }
        public override string ToString() => $"({X},{Y})";
    }

    public static class Pathfinder
    {
        // Simple A* implementation finding path from start to target
        public static List<Point> FindPath(Point start, Point target, int mapWidth, int mapHeight)
        {
            var openSet = new List<Point> { start };
            var cameFrom = new Dictionary<Point, Point>();
            var gScore = new Dictionary<Point, int> { [start] = 0 };
            var fScore = new Dictionary<Point, int> { [start] = Heuristic(start, target) };

            while (openSet.Any())
            {
                // Get node with lowest F score
                var current = openSet.OrderBy(p => fScore.ContainsKey(p) ? fScore[p] : int.MaxValue).First();

                if (current.X == target.X && current.Y == target.Y)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                foreach (var neighbor in GetNeighbors(current, mapWidth, mapHeight))
                {
                    // D20 Rule: Moving 1 square costs 5 feet (1 unit)
                    // Optional: Add logic here to cost diagonals as 1.5 units
                    int tentativeGScore = gScore[current] + 1;

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, target);

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor);
                    }
                }
            }

            return null; // No path found
        }

        private static List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
        {
            var totalPath = new List<Point> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Add(current);
            }
            totalPath.Reverse(); // Start to End
            return totalPath.Skip(1).ToList(); // Exclude current start position
        }

        private static int Heuristic(Point a, Point b)
        {
            // Chebyshev distance is best for grids that allow diagonal movement
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        private static IEnumerable<Point> GetNeighbors(Point center, int width, int height)
        {
            var points = new List<Point>();
            // Check all 8 directions (including diagonals)
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    int newX = center.X + x;
                    int newY = center.Y + y;

                    // Check Bounds
                    if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                    {
                        points.Add(new Point(newX, newY));
                    }
                }
            }
            return points;
        }
    }
}