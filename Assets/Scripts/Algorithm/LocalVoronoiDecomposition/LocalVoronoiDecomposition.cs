using MAES3D.Agent;
using MAES3D.Algorithm;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using MAES3D;

namespace MAES3D.Algorithm.LocalVoronoiDecomposition 
{
    public class LocalVoronoiDecomposition : IAlgorithm 
    {
        private IAgentController _controller;

        private Dictionary<Cell, int> _occlusionPoints;

        private Cell _previousCell;

        private bool _searchMode;

        public LocalVoronoiDecomposition() 
        {
            _occlusionPoints = new Dictionary<Cell, int>();
        }

        public void SetController(IAgentController controller) 
        {
            _controller = controller;
            _previousCell = Utility.CoordinateToCell(_controller.GetPosition());
            _searchMode = false;
        }

        public void UpdateLogic() 
        {
            List<Cell> cells = _controller.GetVisibleCells();
            Cell currentCell = Utility.CoordinateToCell(_controller.GetPosition());
            List<Vector3> visibleAgents = _controller.GetVisibleAgentPositions();

            if (_previousCell != currentCell || _controller.GetCurrentStatus() == Status.Idle) 
            {
                foreach (Cell c in cells) {
                    float tempDist = Vector3.Distance(Utility.CoordinateToCell(_controller.GetPosition()).middle, c.middle);

                    if (_controller.GetLocalExplorationMap()[c.x, c.y, c.z] == CellStatus.explored) 
                    {
                        if (tempDist <= 6.75) 
                        {
                            _controller.GetLocalExplorationMap()[c.x, c.y, c.z] = CellStatus.covered;
                        }

                        bool isClosest = true;

                        foreach (Vector3 v in visibleAgents) 
                        {
                            if (Vector3.Distance(Utility.CoordinateToCell(v).middle, c.middle) < tempDist) 
                            {
                                isClosest = false;
                            }
                        }

                        if (_searchMode && isClosest) 
                        {
                            _searchMode = false;
                            _controller.MoveToCellAsync(currentCell);
                        }
                    }
                }
            }

            if (_controller.GetCurrentStatus() == Status.Idle) 
            {
                /*
                * Used for testing using Seed 1 with a single drone and tempDist set to 10
                if (Utility.CoordinateToCell(_controller.GetPosition()) == Utility.CoordinateToCell(new Vector3(17, 40, 94))) 
                {
                    Debug.Log("Moving back to start!");
                    _controller.MoveToCellAsync(new Cell(10, 8, 8));
                    return;
                }*/


                Cell destination = null;
                CellStatus[,,] currentView = _controller.GetCurrentView();
                List<Vector3> visibleAgents = _controller.GetVisibleAgentPositions();

                _searchMode = false;

                /* EXPLORATION MODE 
                 * FIND NEAREST AVAILABLE CELL */
                destination = ExplorationMode(cells, currentView, visibleAgents) ??
                              SearchMode(cells, currentView, visibleAgents);

                if (destination != null) 
                {
                    if (_searchMode) 
                    {
                        _occlusionPoints[destination]++;
                    }

                    Debug.DrawLine(_controller.GetPosition(), destination.middle, Color.red, 5);
                    _controller.MoveToCellAsync(destination);
                }
            }

            _previousCell = currentCell;
        }

        private Cell ExplorationMode(List<Cell> cells, CellStatus[,,] currentView, List<Vector3> visibleAgents) 
        {
            Cell destination = null;
            float distance = 100000f;

            foreach (Cell c in cells) 
            {
                if (_controller.GetLocalExplorationMap()[c.x, c.y, c.z] == CellStatus.explored) 
                {
                    float tempDist = Vector3.Distance(Utility.CoordinateToCell(_controller.GetPosition()).middle, c.middle);
                    bool isClosest = true;

                    foreach (Vector3 v in visibleAgents) 
                    {
                        if (Vector3.Distance(Utility.CoordinateToCell(v).middle, c.middle) < tempDist) 
                        {
                            isClosest = false;
                        }
                    }

                    if (tempDist < distance && isClosest) 
                    {
                        destination = c;
                        distance = tempDist;
                    }
                }
            }

            return destination;
        }

        private Cell SearchMode(List<Cell> cells, CellStatus[,,] currentView, List<Vector3> visibleAgents) 
        {
            List<Cell> cellsToCheck = new List<Cell>();

            Cell destination = null;
            Cell leastRecentlyVisited = null;

            float distance = 1000f;
            float leastRecentlyVisitedDistance = 1000;

            int leastRecentlyVisitedTime = 10000000;

            _searchMode = true;

            foreach (Cell currentCell in cells) 
            {
                if (currentView[currentCell.x, currentCell.y, currentCell.z] == CellStatus.wall) {
                    List<Cell> OcclusionPointsForCell = GetOCsForCell(currentView, currentCell);
                    cellsToCheck.AddRange(OcclusionPointsForCell);

                    foreach (Cell occlusionPoint in OcclusionPointsForCell) 
                    {
                        if (!_occlusionPoints.ContainsKey(occlusionPoint)) 
                        {
                            _occlusionPoints.Add(occlusionPoint, 0);
                        }
                    }
                }
            }

            foreach (Cell c in cellsToCheck) {
                if (currentView[c.x, c.y, c.z] != CellStatus.wall) {
                    float tempDist = Vector3.Distance(Utility.CoordinateToCell(_controller.GetPosition()).middle, c.middle);
                    bool isClosest = true;

                    foreach (Vector3 v in visibleAgents) {
                        if (Vector3.Distance(Utility.CoordinateToCell(v).middle, c.middle) < tempDist) 
                        {
                            isClosest = false;
                        }
                    }

                    if (isClosest) {
                        /* Finds occlusion points that hasn't been visited yet 
                        * Otherwise find the least recently visited occlusion point */
                        if (tempDist < distance && _occlusionPoints[c] == 0) 
                        {
                            distance = tempDist;
                            destination = c;
                        }
                        else if (_occlusionPoints[c] < leastRecentlyVisitedTime ||
                                (_occlusionPoints[c] == leastRecentlyVisitedTime && leastRecentlyVisitedDistance < tempDist)) 
                        {
                            leastRecentlyVisitedDistance = tempDist;
                            leastRecentlyVisited = c;
                        }
                    }
                }
            }

            if (destination == null) 
            {
                destination = leastRecentlyVisited;
            }

            return destination;
        }

        private List<Cell> GetOCsForCell(CellStatus[,,] currentView, Cell currentCell) 
        {
            Vector3 position = _controller.GetPosition();
            Cell positionCell = Utility.CoordinateToCell(position);

            int signX = Math.Sign(positionCell.x - currentCell.x);
            int signY = Math.Sign(positionCell.y - currentCell.y);
            int signZ = Math.Sign(positionCell.z - currentCell.z);

            int signMagnitude = Math.Abs(signX) + Math.Abs(signY) + Math.Abs(signZ);

            List<Cell> occlusionCells = new List<Cell>();
            if (signMagnitude == 3) {
                if (currentView[currentCell.x + signX * -1, currentCell.y, currentCell.z] != CellStatus.wall) 
                {
                    AddOcclusionCell(occlusionCells, currentView, currentCell, signX * -1, 0, signZ);
                    AddOcclusionCell(occlusionCells, currentView, currentCell, signX * -1, signY, 0);
                }

                if (currentView[currentCell.x, currentCell.y + signY * -1, currentCell.z] != CellStatus.wall) 
                {
                    AddOcclusionCell(occlusionCells, currentView, currentCell, 0, signY * -1, signZ);
                    AddOcclusionCell(occlusionCells, currentView, currentCell, signX, signY * -1, 0);
                }

                if (currentView[currentCell.x, currentCell.y, currentCell.z + signZ * -1] != CellStatus.wall) 
                {
                    AddOcclusionCell(occlusionCells, currentView, currentCell, 0, signY, signZ * -1);
                    AddOcclusionCell(occlusionCells, currentView, currentCell, signX, 0, signZ * -1);
                }
            }
            else if (signMagnitude == 2) {
                if (signX == 0) {
                    if (currentView[currentCell.x + 1, currentCell.y, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 1, 0, signZ);
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 1, signY, 0);
                    }

                    if (currentView[currentCell.x - 1, currentCell.y, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, -1, 0, signZ);
                        AddOcclusionCell(occlusionCells, currentView, currentCell, -1, signY, 0);
                    }

                    if (currentView[currentCell.x, currentCell.y + signY * -1, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 0, signY * -1, signZ);
                    }

                    if (currentView[currentCell.x, currentCell.y, currentCell.z + signZ * -1] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 0, signY, signZ * -1);
                    }
                }
                else if (signY == 0) {
                    if (currentView[currentCell.x, currentCell.y + 1, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 0, 1, signZ);
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, 1, 0);
                    }

                    if (currentView[currentCell.x, currentCell.y - 1, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 0, -1, signZ);
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, -1, 0);
                    }

                    if (currentView[currentCell.x + signX * -1, currentCell.y, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX * -1, 0, signZ);
                    }

                    if (currentView[currentCell.x, currentCell.y, currentCell.z + signZ * -1] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, 0, signZ * -1);
                    }
                }
                else if (signZ == 0) {
                    if (currentView[currentCell.x, currentCell.y, currentCell.z + 1] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 0, signY, 1);
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, 0, 1);
                    }

                    if (currentView[currentCell.x, currentCell.y, currentCell.z - 1] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 0, signY, -1);
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, 0, -1);
                    }

                    if (currentView[currentCell.x + signX * -1, currentCell.y, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX * -1, signY, 0);
                    }

                    if (currentView[currentCell.x, currentCell.y + signY * -1, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, signY * -1, 0);
                    }

                }
            }
            else if (signMagnitude == 1) {
                if (signX == 0) {
                    if (currentView[currentCell.x + 1, currentCell.y, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, 1, signY, signZ);
                    }
                    if (currentView[currentCell.x - 1, currentCell.y, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, -1, signY, signZ);
                    }
                }
                if (signY == 0) {
                    if (currentView[currentCell.x, currentCell.y + 1, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, 1, signZ);
                    }
                    if (currentView[currentCell.x, currentCell.y - 1, currentCell.z] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, -1, signZ);
                    }
                }
                if (signZ == 0) {
                    if (currentView[currentCell.x, currentCell.y, currentCell.z + 1] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, signY, 1);
                    }
                    if (currentView[currentCell.x, currentCell.y, currentCell.z - 1] != CellStatus.wall) 
                    {
                        AddOcclusionCell(occlusionCells, currentView, currentCell, signX, signY, -1);
                    }
                }
            }

            return occlusionCells;
        }

        private bool IsCellTargetable(CellStatus cell) 
        {
            if (cell == CellStatus.wall || cell == CellStatus.unexplored) 
            {
                return false;
            }

            return true;
        }


        private void AddOcclusionCell(List<Cell> occlusionCells, CellStatus[,,] view, Cell wallCell, int signX, int signY, int signZ) 
        {
            Cell cellToAdd = new Cell(wallCell.x + signX, wallCell.y + signY, wallCell.z + signZ);

            if (IsCellTargetable(view[cellToAdd.x, cellToAdd.y, cellToAdd.z])) 
            {
                occlusionCells.Add(new Cell(wallCell.x + signX, wallCell.y + signY, wallCell.z + signZ));
            }
        }
    }
}