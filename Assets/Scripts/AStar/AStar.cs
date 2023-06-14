using System;
using System.Collections;
using System.Collections.Generic;
using MAES3D.Agent;
using UnityEngine;

namespace MAES3D 
{
    public class AStar
    {
        private static int MAX_ITERATIONS => SimulationSettings.AStarIterations;

        // Find the shortest path from start to target in a 3D grid
        public static List<Cell> FindPath(Vector3 start, Vector3 target, CellStatus[,,] grid)
        {
            // Create nodes for start and target positions
            var startNode = new Node((int) start.x, (int) start.y, (int) start.z);
            var targetNode = new Node((int) target.x, (int) target.y, (int) target.z);

            // Initialize open and closed sets for pathfinding
            var openSet = new HashSet<Node>();
            var closedSet = new HashSet<Node>();

            // Set the initial scores for the start node
            startNode.GScore = 0;
            startNode.FScore = Heuristic(startNode, targetNode);

            // Add the start node to the open set
            openSet.Add(startNode);

            // Main loop for the A* algorithm
            while (openSet.Count > 0) 
            {
                // Get the node with the lowest F score from the open set
                var current = GetLowestFScore(openSet);

                // If the current node is the target node, return the reconstructed path
                if (current == targetNode) 
                {
                    return ReconstructPath(current);
                }

                // Move the current node from the open set to the closed set
                openSet.Remove(current);
                closedSet.Add(current);

                // Iterate through the neighbors of the current node
                foreach (var neighbor in GetNeighbors(grid, current)) 
                {
                    // If the neighbor is already in the closed set, skip it
                    if (closedSet.Contains(neighbor)) 
                    {
                        continue;
                    }

                    // Calculate the tentative G score for the neighbor
                    var tentativeGScore = current.GScore + 1;

                    // Update the neighbor's information if it's not in the open set or has a lower tentative G score
                    if (!openSet.Contains(neighbor) || tentativeGScore < neighbor.GScore) 
                    {
                        neighbor.Parent = current;
                        neighbor.GScore = tentativeGScore;
                        neighbor.FScore = tentativeGScore + Heuristic(neighbor, targetNode);

                        // Add the neighbor to the open set if it's not already there
                        if (!openSet.Contains(neighbor)) 
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            // Return null if there's no valid path
            return null;
        }

        // Find the shortest path from start to target in a 3D grid as a CoRoutine
        public static IEnumerator FindPath(Vector3 start, Vector3 target, CellStatus[,,] grid, Action<List<Cell>> onComplete)
        {
            // Number of iterations the main loop has done
            int iterations = 0;

            // Create nodes for start and target positions
            var startNode = new Node((int) start.x, (int) start.y, (int) start.z);
            var targetNode = new Node((int) target.x, (int) target.y, (int) target.z);

            // Initialize open and closed sets for pathfinding
            var openSet = new HashSet<Node>();
            var closedSet = new HashSet<Node>();

            // Set the initial scores for the start node
            startNode.GScore = 0;
            startNode.FScore = Heuristic(startNode, targetNode);

            // Add the start node to the open set
            openSet.Add(startNode);

            // Main loop for the A* algorithm
            while (openSet.Count > 0) 
            {
                // Get the node with the lowest F score from the open set
                var current = GetLowestFScore(openSet);

                // If the current node is the target node, return the reconstructed path
                if (current == targetNode) 
                {
                    Debug.Log($"A* Path found! (Iterations: {iterations})");
                    onComplete(ReconstructPath(current));
                    yield break;
                }

                // Move the current node from the open set to the closed set
                openSet.Remove(current);
                closedSet.Add(current);

                // Iterate through the neighbors of the current node
                foreach (var neighbor in GetNeighbors(grid, current)) 
                {
                    // If the neighbor is already in the closed set, skip it
                    if (closedSet.Contains(neighbor)) 
                    {
                        continue;
                    }

                    // Calculate the tentative G score for the neighbor
                    var tentativeGScore = current.GScore + 1;

                    // Update the neighbor's information if it's not in the open set or has a lower tentative G score
                    if (!openSet.Contains(neighbor) || tentativeGScore < neighbor.GScore) 
                    {
                        neighbor.Parent = current;
                        neighbor.GScore = tentativeGScore;
                        neighbor.FScore = tentativeGScore + Heuristic(neighbor, targetNode);

                        // Add the neighbor to the open set if it's not already there
                        if (!openSet.Contains(neighbor)) 
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }

                iterations++;

                if (iterations % MAX_ITERATIONS == 0) 
                {
                    // Yield the current result to wait for the next frame
                    Debug.Log($"Yielding control to Unity! (Iterations: {iterations})");
                    yield return new WaitForFixedUpdate();
                }
            }

            // Return null if there's no valid path
            Debug.Log("No A* Path found!");
            onComplete(null);
        }

        // Get the node with the lowest F score from a set
        private static Node GetLowestFScore(HashSet<Node> set)
        {
            var lowestScore = float.MaxValue;
            Node lowestNode = null;

            // Iterate through the set and find the node with the lowest F score
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

        // Get a list of neighboring nodes for the given node in the grid
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

                        // Make a new neighbor using the current node's offsets
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

        // Calculate the heuristic value between two nodes using Manhattan distance
        private static float Heuristic(Node nodeA, Node nodeB)
        {
            var dx = Mathf.Abs(nodeA.x - nodeB.x);
            var dy = Mathf.Abs(nodeA.y - nodeB.y);
            var dz = Mathf.Abs(nodeA.z - nodeB.z);

            return dx + dy + dz;
        }

        // Reconstruct the path from the goal node back to the start node
        private static List<Cell> ReconstructPath(Node goalNode)
        {
            List<Cell> path = new List<Cell>();
            Node current = goalNode;

            // Trace back from the goal node to the start node using parent pointers
            while (current != null)
            {
                path.Add(new Cell(current.x, current.y, current.z));
                current = current.Parent;
            }

            // Reverse the path to get it in the correct order
            path.Reverse();
            return path;
        }
    }
}
            