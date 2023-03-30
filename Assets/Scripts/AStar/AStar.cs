using System.Collections;
using System.Collections.Generic;
using MAES3D.Agent;
using UnityEngine;

namespace MAES3D 
{
    public class AStar
    {
        public static List<Cell> FindPath(Vector3 start, Vector3 target, CellStatus[,,] grid)
        {
            var startNode = new Node((int) start.x, (int) start.y, (int) start.z);
            var targetNode = new Node((int) target.x, (int) target.y, (int) target.z);

            var openSet = new HashSet<Node>();
            var closedSet = new HashSet<Node>();

            startNode.GScore = 0;
            startNode.FScore = Heuristic(startNode, targetNode);

            openSet.Add(startNode);

            while (openSet.Count > 0) 
            {
                var current = GetLowestFScore(openSet);

                if (current == targetNode) 
                {
                    return ReconstructPath(current);
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in GetNeighbors(grid, current)) 
                {
                    if (closedSet.Contains(neighbor)) 
                    {
                        continue;
                    }

                    var tentativeGScore = current.GScore + 1;

                    if (!openSet.Contains(neighbor) || tentativeGScore < neighbor.GScore) 
                    {
                        neighbor.Parent = current;
                        neighbor.GScore = tentativeGScore;
                        neighbor.FScore = tentativeGScore + Heuristic(neighbor, targetNode);

                        if (!openSet.Contains(neighbor)) 
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return null;
        }

        private static Node GetLowestFScore(HashSet<Node> set)
        {
            var lowestScore = float.MaxValue;
            Node lowestNode = null;

            foreach (Node node in set)
            {
                if (node.FScore < lowestScore)
                {
                    lowestScore = node.FScore;
                    lowestNode = node;
                }
            }

            return lowestNode;
        }

        private static List<Node> GetNeighbors(CellStatus[,,] grid, Node node)
        {
            List<Node> neighbors = new List<Node>();

            // Check each of the 6 surrounding nodes
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        // Skip the center node and diagonal neighbors
                        if ((dx != 0 && dy != 0) || (dx != 0 && dz != 0) || (dy != 0 && dz != 0))
                            continue;

                        // Make a new neighbour using current node the offsets
                        Node neighbor = new Node(node.x + dx, node.y + dy, node.z + dz);

                        // Add the neighbor if it's covered or explored
                        if (grid[neighbor.x, neighbor.y, neighbor.z] == CellStatus.covered ||
                            grid[neighbor.x, neighbor.y, neighbor.z] == CellStatus.explored) 
                        {
                            neighbors.Add(neighbor);
                        }
                    }
                }
            }

            return neighbors;
        }

        private static float Heuristic(Node nodeA, Node nodeB)
        {
            // Use Manhattan distance as heuristic
            var dx = Mathf.Abs(nodeA.x - nodeB.x);
            var dy = Mathf.Abs(nodeA.y - nodeB.y);
            var dz = Mathf.Abs(nodeA.z - nodeB.z);

            return dx + dy + dz;
        }

        private static List<Cell> ReconstructPath(Node goalNode)
        {
            List<Cell> path = new List<Cell>();
            Node current = goalNode;

            while (current != null)
            {
                path.Add(new Cell(current.x, current.y, current.z));
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }
    }
}
