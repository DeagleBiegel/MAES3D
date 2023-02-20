using MAES3D.Agent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;

namespace MAES3D {

    internal class AStarCell{
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        public Cell cell;
        public AStarCell parent;

        public AStarCell(Cell cell, AStarCell parent = null) {
            this.cell = cell;
            this.parent = parent;
            gCost = 0;
            hCost = 0;
        }
    }

    public static class AStar {

        public static List<Cell> FindPath(Vector3 startPosition, Vector3 targetPosition, CellStatus[,,] map) {            
            Cell startCell = Utility.CoordinateToCell(startPosition);
            Cell targetCell = Utility.CoordinateToCell(targetPosition);

            List<AStarCell> openList = new List<AStarCell>();
            List<AStarCell> closedList = new List<AStarCell>();

            openList.Add(new AStarCell(startCell));

            int safetyLimit = 10000;
            int safetyCount = 0;

            while (openList.Count != 0) {
                safetyCount++;

                if (safetyCount > safetyLimit) {
                    Debug.LogWarning($"AStar Error: Going from {startPosition} to {targetPosition} hit the safetylimit of {safetyLimit}\n" + 
                                     $"\tReturning nothing");
                    Debug.Break();
                    return new List<Cell>();
                }


                AStarCell qCell = openList[0];

                //Set current node as the node with lowest fcost
                int minIndex = 0;
                for (int i = 0; i < openList.Count; i++) {
                    AStarCell openCell = openList[i];
                    if (openCell.fCost < qCell.fCost) {
                        qCell = openCell;
                        minIndex = i;
                    }
                }

                //Remove cell from open list and add to closed list
                openList.RemoveAt(minIndex);
                closedList.Add(qCell);

                //Check if cell is the goal
                if (qCell.cell == targetCell) {
                    //Backtrack to get path and return
                    List<Cell> path = new List<Cell>();
                    AStarCell current = qCell;
                    while (current != null) {
                        path.Add(current.cell);
                        current = current.parent;
                    }
                    path.Reverse();

                    return path;

                }

                List<AStarCell> successors = GetSuccesors(qCell);
                foreach (AStarCell successor in successors) {
                    
                    bool shouldSkip = false;

                    //Skip if successor is not discoverd or not walkable
                    if (map[successor.cell.x, successor.cell.y, successor.cell.z] == CellStatus.unexplored
                        || map[successor.cell.x, successor.cell.y, successor.cell.z] == CellStatus.wall) {
                        continue;
                    }

                    //Skip if successor already exist in the closed list
                    foreach (AStarCell closedCell in closedList) {
                        if (successor.cell == closedCell.cell) {
                            shouldSkip = true;
                            break;
                        }
                    }
                    if (shouldSkip) continue;

                    //Assign g and h costs
                    successor.gCost = qCell.gCost + 1;
                    successor.hCost = Cell.ManhattanDistance(successor.cell, targetCell);

                    //Skip if successor is already on the closed list with a lower g cost
                    foreach (AStarCell openCell in openList) {
                        if (successor.cell == openCell.cell
                           && successor.gCost > openCell.gCost) {
                            shouldSkip = true;
                            break;
                        }
                    }
                    if (shouldSkip) continue;

                    openList.Add(successor);
                }
            }

            Debug.LogWarning($"AStar Error: Could not find path between {startPosition} and {targetPosition}. Returning empty path.");
            Debug.Break();
            return new List<Cell>();
        }

        private static List<AStarCell> GetSuccesors(AStarCell parent) {
            List<AStarCell> successors = new List<AStarCell> {
                //x+1 sucessor
                new AStarCell(new Cell(parent.cell.x + 1, parent.cell.y, parent.cell.z), parent),
                //x-1 sucessor
                new AStarCell(new Cell(parent.cell.x - 1, parent.cell.y, parent.cell.z), parent),
                //y+1 sucessor
                new AStarCell(new Cell(parent.cell.x, parent.cell.y + 1, parent.cell.z), parent),
                //y-1 sucessor
                new AStarCell(new Cell(parent.cell.x, parent.cell.y - 1, parent.cell.z), parent),
                //z+1 sucessor
                new AStarCell(new Cell(parent.cell.x, parent.cell.y, parent.cell.z + 1), parent),
                //z-1 sucessor
                new AStarCell(new Cell(parent.cell.x, parent.cell.y, parent.cell.z - 1), parent)
            };

            return successors;
        }

    }
}
