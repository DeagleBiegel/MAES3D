using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace MAES3D.Agent {
    public class ExplorationManager {

        private List<Vector3> _relativeRayTargets;
        private List<ObservationLine> _observationLines;

        private bool[,,] _voxelMap;
        private bool[,,] _exploredMap; 
        private bool[,,] _hasBeenSeen;
        
        private int _exploredTiles;
        private int _explorableTiles;

        private int agentTicker = 0;

        public float ExploredRatio => _exploredTiles * 100 / _explorableTiles;

        public ExplorationManager() 
        {
            _observationLines = CalculateObservationLines();

            Chunk chunk = GameObject.FindObjectOfType(typeof(Chunk)) as Chunk;
            _voxelMap = chunk.GetVoxelMap();
            _exploredMap = new bool[_voxelMap.GetLength(0), _voxelMap.GetLength(1), _voxelMap.GetLength(2)];
            _exploredTiles = 0;
            _explorableTiles = chunk.GetNumberOfExplorableTiles();
        }

        public void UpdateMaps(List<SubmarineAgent> agents) 
        {
            //Sensing
            int agentIndexToUpdate = agentTicker++ % agents.Count;
            UpdateMap(agents[agentIndexToUpdate]);

            foreach (SubmarineAgent agent in agents) 
            {
                agent.Controller.ExplorationMap.LookForAgents(agent, agents);
            }
        }

        private void UpdateMap(SubmarineAgent agent) 
        {
            agent.Controller.ExplorationMap.ResetCurrentView();
            Vector3 agentPosition = agent.Controller.GetPosition();
            _hasBeenSeen = new bool[_voxelMap.GetLength(0), _voxelMap.GetLength(1), _voxelMap.GetLength(2)];

            foreach (ObservationLine observationLine in _observationLines) 
            {
                UpdateCells(agentPosition, observationLine, agent);
            }

            Cell currentCell = Utility.CoordinateToCell(agentPosition);
            agent.Controller.ExplorationMap.UpdateCell(currentCell, CellStatus.covered);
            SimulationSettings.progress = (_exploredTiles * 100) / _explorableTiles;
        }

        private void UpdateCells(Vector3 agentPosition, ObservationLine line, SubmarineAgent agent) 
        {
            Cell agentCell = Utility.CoordinateToCell(agentPosition);
            Cell targetCell = new Cell(agentCell.x + line.targetCellOffset.x,
                                       agentCell.y + line.targetCellOffset.y,
                                       agentCell.z + line.targetCellOffset.z);

            float manhattanDistance = Cell.ManhattanDistance(agentCell, targetCell);

            Vector3 tMax = new Vector3
            (
                (line.sign[0] == 0 || line.sign[0] == 1)
                    ? (1 - agentPosition.x % 1) * line.tMaxPart.x
                    : agentPosition.x % 1 * line.tMaxPart.x,
                (line.sign[1] == 0 || line.sign[1] == 1)
                    ? (1 - agentPosition.y % 1) * line.tMaxPart.y
                    : agentPosition.y % 1 * line.tMaxPart.y,
                (line.sign[2] == 0 || line.sign[2] == 1)
                    ? (1 - agentPosition.z % 1) * line.tMaxPart.z
                    : agentPosition.z % 1 * line.tMaxPart.z
            );

            Cell currentCell = agentCell;

            for (int i = 0; i <= 1000; i++) 
            {
                if (tMax.x < tMax.y) 
                {
                    if (tMax.x < tMax.z) 
                    { // tMaxX < tMaxY og tMaxZ
                        currentCell.x += line.sign[0];
                        tMax.x += line.delta.x;
                    }
                    else 
                    { // tMaxZ < tMaxX < tMaxY 
                        currentCell.z += line.sign[2];
                        tMax.z += line.delta.z;
                    }
                }
                else 
                {
                    if (tMax.y < tMax.z) 
                    { // tMaxY < tMaxX og tMaxZ 
                        currentCell.y += line.sign[1];
                        tMax.y += line.delta.y;
                    }
                    else 
                    { // tMaxZ < tMaxY < tMaxX 
                        currentCell.z += line.sign[2];
                        tMax.z += line.delta.z;
                    }
                }

                Cell cell = new Cell(currentCell.x, currentCell.y, currentCell.z);

                if (_hasBeenSeen[cell.x, cell.y, cell.z] == false) 
                {
                    if (_voxelMap[cell.x, cell.y, cell.z] == true)
                    {
                        agent.Controller.ExplorationMap.UpdateCell(cell, CellStatus.wall);
                        break;
                    }
                    else 
                    {
                        agent.Controller.ExplorationMap.UpdateCell(cell, CellStatus.explored);

                        if (_exploredMap[cell.x, cell.y, cell.z] == false) 
                        {
                            _exploredMap[cell.x, cell.y, cell.z] = true;
                            _exploredTiles++;
                        }
                    }

                    _hasBeenSeen[cell.x, cell.y, cell.z] = true;
                }
            }
        }

        private void UpdateMapOLD(SubmarineAgent agent) {
            agent.Controller.ExplorationMap.ResetCurrentView();

            Vector3 agentPosition = agent.Controller.GetPosition();

            foreach (Vector3 relativeTarget in _relativeRayTargets) {
                List<Cell> coveredCells = GetCellsBetweenPoints(agentPosition, relativeTarget);
                
                foreach (Cell cell in coveredCells) {
                    if (_voxelMap[cell.x, cell.y, cell.z] == true) {
                        agent.Controller.ExplorationMap.UpdateCell(cell, CellStatus.wall);
                    }
                    else {
                        agent.Controller.ExplorationMap.UpdateCell(cell, CellStatus.explored);

                        if (_exploredMap[cell.x, cell.y, cell.z] == false) {
                            _exploredMap[cell.x, cell.y, cell.z] = true;
                            _exploredTiles++;
                        }
                    }
                }
            }
            Cell currentCell = Utility.CoordinateToCell(agentPosition);
            agent.Controller.ExplorationMap.UpdateCell(currentCell, CellStatus.covered);
            SimulationSettings.progress = ExploredRatio;

        }

        public void MergeNearbyMaps(List<SubmarineAgent> agents, float maxRange = 20 /*TODO*/) {

            foreach (SubmarineAgent agent in agents) {
                Vector3 ownPosition = agent.Controller.GetPosition();

                List<SubmarineAgent> tempList = new List<SubmarineAgent>(agents);
                tempList.Remove(agent);

                foreach (SubmarineAgent otherAgent in tempList) {
                    Vector3 otherAgentPosition = otherAgent.Controller.GetPosition();

                    if (Vector3.Distance(ownPosition, otherAgentPosition) <= maxRange) {

                        if (!Physics.Linecast(ownPosition, otherAgentPosition, LayerMask.GetMask("Drones"))) {
                            agent.Controller.ExplorationMap.CombineWithMap(otherAgent.Controller.ExplorationMap.GetMap());
                        }
                    }
                }
            }
        }

        private List<Vector3> CalculateRayTargetPoints(float radius) {
            List<Vector3> gridPoints = new List<Vector3>();

            Vector3 currentPoint = new Vector3(0, radius, 0);

            //q1
            for (currentPoint.z = 0; currentPoint.z <= radius; currentPoint.z++) {
                while (currentPoint.y >= currentPoint.x && currentPoint.y >= currentPoint.z) {
                    float tempDist = Vector3.Distance(Vector3.zero, currentPoint);
                    if (tempDist <= radius && tempDist > radius - 1) {
                        gridPoints.Add(currentPoint);
                        gridPoints.Add(new Vector3(currentPoint.y, currentPoint.x, currentPoint.z));
                        gridPoints.Add(new Vector3(currentPoint.z, currentPoint.x, currentPoint.y));
                        currentPoint.x++;
                    }
                    else {
                        currentPoint.y--;
                    }
                }
                currentPoint.x = 0;
                currentPoint.y = radius;
            }

            int tempCount = gridPoints.Count;
            for (int i = 0; i < tempCount; i++) {
                gridPoints.Add(new Vector3(gridPoints[i].x, -gridPoints[i].y, gridPoints[i].z));    //q2
                gridPoints.Add(new Vector3(-gridPoints[i].x, -gridPoints[i].y, gridPoints[i].z));   //q3
                gridPoints.Add(new Vector3(-gridPoints[i].x, gridPoints[i].y, gridPoints[i].z));    //q4
                gridPoints.Add(new Vector3(gridPoints[i].x, gridPoints[i].y, -gridPoints[i].z));    //q5             
                gridPoints.Add(new Vector3(gridPoints[i].x, -gridPoints[i].y, -gridPoints[i].z));   //q6
                gridPoints.Add(new Vector3(-gridPoints[i].x, -gridPoints[i].y, -gridPoints[i].z));  //q7
                gridPoints.Add(new Vector3(-gridPoints[i].x, gridPoints[i].y, -gridPoints[i].z));   //q8
            }

            return gridPoints;
        }

        private List<ObservationLine> CalculateObservationLines() 
        {
            /*
            IcosahedronGenerator icosahedron = new IcosahedronGenerator();
            icosahedron.Subdivide(4);
            */

            float goldenRatio = (1 + Mathf.Pow(5, 0.5f)) / 2f;
            List<Vector3> points = new List<Vector3>();

            int n = 2562 * 2;

            for (int i = 0; i < n; i++) 
            {
                float theta = 2 * Mathf.PI * i / goldenRatio;
                float phi = Mathf.Acos(1 - 2 * (i + 0.5f) / n);
                points.Add
                (
                    new Vector3
                    (
                        Mathf.Cos(theta) * Mathf.Sin(phi),
                        Mathf.Sin(theta) * Mathf.Sin(phi),
                        Mathf.Cos(phi)
                    )
                );
            }

            List<ObservationLine> lines = new List<ObservationLine>();

            foreach (Vector3 rayTarget in points) 
            {
                lines.Add(new ObservationLine(rayTarget));
            }

            return lines;
        }

        private List<Cell> GetObservedCellsOnLine(Vector3 agentPosition, ObservationLine line, SubmarineAgent agent) 
        {
            Cell agentCell = Utility.CoordinateToCell(agentPosition);
            Cell targetCell = new Cell(agentCell.x + line.targetCellOffset.x,
                                    agentCell.y + line.targetCellOffset.y,
                                    agentCell.z + line.targetCellOffset.z);

            float manhattanDistance = Cell.ManhattanDistance(agentCell, targetCell);

            Vector3 tMax = new Vector3
            (
                (line.sign[0] == 0 || line.sign[0] == 1)
                    ? (1 - agentPosition.x % 1) * line.tMaxPart.x
                    : agentPosition.x % 1 * line.tMaxPart.x,
                (line.sign[1] == 0 || line.sign[1] == 1)
                    ? (1 - agentPosition.y % 1) * line.tMaxPart.y
                    : agentPosition.y % 1 * line.tMaxPart.y,
                (line.sign[2] == 0 || line.sign[2] == 1)
                    ? (1 - agentPosition.z % 1) * line.tMaxPart.z
                    : agentPosition.z % 1 * line.tMaxPart.z
            );

            //Traverse the grid and add each visited cell to the list
            List<Cell> traversedCells = new List<Cell>();
            Cell currentCell = agentCell;

            for (int i = 0; i <= 1000; i++) 
            {
                if (tMax.x < tMax.y) 
                {
                    if (tMax.x < tMax.z) 
                    { // tMaxX < tMaxY og tMaxZ
                        currentCell.x += line.sign[0];
                        tMax.x += line.delta.x;
                    }
                    else 
                    { // tMaxZ < tMaxX < tMaxY 
                        currentCell.z += line.sign[2];
                        tMax.z += line.delta.z;
                    }
                }
                else 
                {
                    if (tMax.y < tMax.z) 
                    { // tMaxY < tMaxX og tMaxZ 
                        currentCell.y += line.sign[1];
                        tMax.y += line.delta.y;
                    }
                    else 
                    { // tMaxZ < tMaxY < tMaxX 
                        currentCell.z += line.sign[2];
                        tMax.z += line.delta.z;
                    }
                }

                /*
                agent.HasBeenSeen removed.
                if (!agent.HasBeenSeen[currentCell.x, currentCell.y, currentCell.z]) 
                {
                    traversedCells.Add(new Cell(currentCell.x, currentCell.y, currentCell.z));
                    agent.HasBeenSeen[currentCell.x, currentCell.y, currentCell.z] = true;
                }

                if (_voxelMap[currentCell.x, currentCell.y, currentCell.z])
                {
                    break;
                }
                */
            }
            
            return traversedCells;
        }

        private bool IsCellViewableFromPosition(Cell cell, Vector3 observationPosition) {

            Cell observationCell = Utility.CoordinateToCell(observationPosition);

            int[] sign = {Math.Sign(observationCell.x - cell.x),
                          Math.Sign(observationCell.y - cell.y),
                          Math.Sign(observationCell.z - cell.z)};

            if (sign[0] != 0 && _voxelMap[cell.x + sign[0], cell.y, cell.z] == false ||
                sign[1] != 0 && _voxelMap[cell.x, cell.y + sign[1], cell.z] == false ||
                sign[2] != 0 && _voxelMap[cell.x, cell.y, cell.z + sign[2]] == false) {
                return true;
            }
            else { 
                return false;
            }
        }

        private List<Cell> GetCellsBetweenPoints(Vector3 agentPosition, Vector3 targetPosition) {
            Vector3 ray;
            Cell targetCell;

            if (Physics.Raycast(agentPosition, targetPosition, out RaycastHit hit, targetPosition.magnitude)) {
                ray = hit.point - agentPosition;
                targetCell = Utility.CoordinateToCell(hit.point - hit.normal * 0.5f);
            }
            else {
                ray = targetPosition;
                targetCell = Utility.CoordinateToCell(agentPosition + targetPosition);
            }

            Cell agentCell = Utility.CoordinateToCell(agentPosition);
            float manhattenDistance = Mathf.Abs(targetCell.x - agentCell.x) + Mathf.Abs(targetCell.y - agentCell.y) + Mathf.Abs(targetCell.z - agentCell.z);
            
            //Caluclate the vectors that are required to pass one gridline on each axis
            Vector3 Xscaler = Vector3.one * 1 / (ray.x == 0 ? ray.x + 0.0001f : ray.x);
            Vector3 Yscaler = Vector3.one * 1 / (ray.y == 0 ? ray.y + 0.0001f : ray.y);
            Vector3 Zscaler = Vector3.one * 1 / (ray.z == 0 ? ray.z + 0.0001f : ray.z);

            //Get the travel length required to pass one gridline on each axis
            float deltaX = Vector3.Scale(ray, Xscaler).magnitude;
            float deltaY = Vector3.Scale(ray, Yscaler).magnitude;
            float deltaZ = Vector3.Scale(ray, Zscaler).magnitude;

            //Calculate the traveled direction on each axis
            int stepX = Math.Sign(ray.x);
            int stepY = Math.Sign(ray.y);
            int stepZ = Math.Sign(ray.z);

            //The travel length required to pass the first gridline on each axis
            float tMaxX = ray.x >= 0 
                ? (1 - agentPosition.x % 1) * Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(1, 0, 0))))
                : agentPosition.x % 1       * Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(-1, 0, 0))));

            float tMaxY = ray.y >= 0 
                ? (1 - agentPosition.y % 1) * Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(0, 1, 0))))
                : agentPosition.y % 1       * Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(0, -1, 0))));

            float tMaxZ = ray.z >= 0
                ? (1 - agentPosition.z % 1) * Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(0, 0, 1))))
                : agentPosition.z % 1       * Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(0, 0, -1))));

            //Traverse the grid and add each visited cell to the list
            List<Cell> traversedCells = new List<Cell>();
            Cell currentCell = agentCell;

            for (int i = 0; i < manhattenDistance; i++) {
                if (tMaxX < tMaxY) {
                    if (tMaxX < tMaxZ) { // tMaxX < tMaxY og tMaxZ
                        currentCell.x += stepX;
                        tMaxX += deltaX;
                    }
                    else { // tMaxZ < tMaxX < tMaxY 
                        currentCell.z += stepZ;
                        tMaxZ += deltaZ;
                    }
                }
                else {
                    if (tMaxY < tMaxZ) { // tMaxY < tMaxX og tMaxZ 
                        currentCell.y += stepY;
                        tMaxY += deltaY;
                    }
                    else { // tMaxZ < tMaxY < tMaxX 
                        currentCell.z += stepZ;
                        tMaxZ += deltaZ;
                    }
                }

                if(currentCell.x < 0 || currentCell.y < 0 || currentCell.z < 0 || currentCell.x > SimulationSettings.Width || currentCell.y > SimulationSettings.Height || currentCell.z > SimulationSettings.Depth) {
                    break;
                }

                traversedCells.Add(new Cell(
                    currentCell.x,
                    currentCell.y,
                    currentCell.z)
                );
            }

            return traversedCells;
            
        }
    }

    internal class ObservationLine {

        public readonly int[] sign = new int[3];
        public readonly Vector3 delta = new Vector3();
        public readonly Vector3 tMaxPart = new Vector3();

        public readonly Cell targetCellOffset;

        public ObservationLine(Vector3 ray) {
            //Caluclate the vectors that are required to pass one gridline on each axis
            Vector3 Xscaler = Vector3.one * 1 / (ray.x == 0 ? ray.x + 0.0001f : ray.x);
            Vector3 Yscaler = Vector3.one * 1 / (ray.y == 0 ? ray.y + 0.0001f : ray.y);
            Vector3 Zscaler = Vector3.one * 1 / (ray.z == 0 ? ray.z + 0.0001f : ray.z);

            //Get the travel length required to pass one gridline on each axis
            delta.x = Vector3.Scale(ray, Xscaler).magnitude;
            delta.y = Vector3.Scale(ray, Yscaler).magnitude;
            delta.z = Vector3.Scale(ray, Zscaler).magnitude;

            //Calculate the traveled direction on each axis
            sign[0] = Math.Sign(ray.x);
            sign[1] = Math.Sign(ray.y);
            sign[2] = Math.Sign(ray.z);

            tMaxPart.x = (sign[0] == 0 || sign[0] == 1)
                ? Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(1, 0, 0))))
                : Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(-1, 0, 0))));

            tMaxPart.y = (sign[1] == 0 || sign[1] == 1)
                ? Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(0, 1, 0))))
                : Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(0, -1, 0))));

            tMaxPart.z = (sign[2] == 0 || sign[2] == 1)
                ? Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(0, 0, 1))))
                : Mathf.Sin(Mathf.Deg2Rad * 90) / Mathf.Sin(Mathf.Deg2Rad * (90f - Vector3.Angle(ray, new Vector3(0, 0, -1))));

            targetCellOffset = Utility.CoordinateToCell(ray);
        }
    }
}
