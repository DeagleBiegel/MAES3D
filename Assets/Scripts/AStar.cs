using MAES3D.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;

namespace MAES3D {

    internal class AStarNode {
        public int gCost;
        public float hCost;
        public float fCost => gCost + hCost;

        public Cell pos;
        public AStarNode parent;

        public AStarNode(Cell position) {
            this.pos = position;
        }
    }

    public static class AStar {

        public static List<Cell> FindPath(Vector3 startPosition, Vector3 targetPosition, CellStatus[,,] map) {            
            Cell startCell = Utility.CoordinateToCell(startPosition);
            Cell targetCell = Utility.CoordinateToCell(targetPosition);

            List<AStarNode> openList = new List<AStarNode>();
            List<AStarNode> closedList = new List<AStarNode>();

            openList.Add(new AStarNode(startCell));

            int safetyLimit = 10000;
            int safetyCount = 0;

            while (openList.Count > 0) {
                safetyCount++;
                if (safetyCount > safetyLimit) {
                    Debug.LogWarning($"AStar Error: Going from {startPosition} to {targetPosition} hit the safetylimit of {safetyLimit}\n" + 
                                     $"\tReturning nothing");

                    return new List<Cell>();
                }

                AStarNode currentNode = openList[0];
                int currentIndex = 0;

                //Set current node as the node with lowest fcost
                for (int i = 1; i < openList.Count; i++) {
                    if (openList[i].fCost < currentNode.fCost || (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost)) {
                        currentNode = openList[i];
                        currentIndex = i;
                    }
                }

                //Remove cell from open list and add to closed list
                openList.RemoveAt(currentIndex);
                closedList.Add(currentNode);

                //Check if cell is the goal
                if (currentNode.pos.x == targetCell.x && currentNode.pos.y == targetCell.y && currentNode.pos.z == targetCell.z) {
                    //Backtrack to get path and return
                    List<Cell> path = new List<Cell>();
                    AStarNode current = currentNode;

                    while (current != null) {
                        path.Add(current.pos);
                        current = current.parent;
                    }
                    path.Reverse();

                    Debug.Log(path.Count);
                    for (int i = 1; i < path.Count; i++) {
                        Debug.DrawLine(path[i - 1].middle, path[i].middle, Color.blue, 50);
                    }

                    return path;
                }

                List<AStarNode> neighbors = GetNeighbors(currentNode);
                foreach (AStarNode neighbor in neighbors) {

                    //Skip if successor already exist in the closed list
                    if (closedList.Contains(neighbor)) {
                        continue;
                    }


                    int tentativeG = currentNode.gCost;
                    //Skip if successor is not discoverd or not walkable
                    if (map[neighbor.pos.x, neighbor.pos.y, neighbor.pos.z] == CellStatus.unexplored
                        || map[neighbor.pos.x, neighbor.pos.y, neighbor.pos.z] == CellStatus.wall) {
                        tentativeG += 1;
                    }

                    if(!openList.Contains(neighbor) || tentativeG < neighbor.gCost) {
                        //Assign g and h costs
                        neighbor.gCost = tentativeG;
                        neighbor.hCost = Heuristic(neighbor.pos, targetCell);
                        neighbor.parent = currentNode;

                        if (!openList.Contains(neighbor)) {
                            openList.Add(neighbor);
                        }
                    }
                }
            }

            Debug.LogWarning($"AStar Error: Could not find path between {startPosition} and {targetPosition}. Returning empty path.");

            Debug.Log(closedList.Count);

            foreach (AStarNode node in closedList) {
                Debug.DrawLine(startPosition, node.pos.middle);
            }
            
            Debug.Break();
            return new List<Cell>();
        }

        private static List<AStarNode> GetNeighbors(AStarNode parent) {

            List<AStarNode> neighbors = new List<AStarNode>{
                new AStarNode(new Cell(parent.pos.x + 1, parent.pos.y, parent.pos.z)),
                new AStarNode(new Cell(parent.pos.x - 1, parent.pos.y, parent.pos.z)),
                new AStarNode(new Cell(parent.pos.x, parent.pos.y + 1, parent.pos.z)),
                new AStarNode(new Cell(parent.pos.x, parent.pos.y - 1, parent.pos.z)),
                new AStarNode(new Cell(parent.pos.x, parent.pos.y, parent.pos.z + 1)),
                new AStarNode(new Cell(parent.pos.x, parent.pos.y, parent.pos.z - 1))
            };

            return neighbors;
        }

        private static float Heuristic(Cell nodeA, Cell nodeB) {
            int dx = nodeA.x - nodeB.x;
            int dy = nodeA.y - nodeB.y;
            int dz = nodeA.z - nodeB.z;

            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

    }
}
