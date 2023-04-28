using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEngine;

namespace MAES3D.Agent {
    public class ExplorationMap {

        private CellStatus[,,] _map;
        private CellStatus[,,] _currentView;

        private CellStatus[,,] _blankMap;

        private List<Cell> _visibleCells;
        private List<Vector3> _visibleAgentPositions;
        private List<SubmarineAgent> _visibleAgents;

        public ExplorationMap(int mapSizeX, int mapSizeY, int mapSizeZ) {

            _blankMap = new CellStatus[mapSizeX, mapSizeY, mapSizeZ];
            for (int x = 0; x < _blankMap.GetLength(0); x++) {
                for (int y = 0; y < _blankMap.GetLength(1); y++) {
                    for (int z = 0; z < _blankMap.GetLength(2); z++) {
                        _blankMap[x, y, z] = CellStatus.unexplored;
                    }
                }
            }
                
            _map = _blankMap.Clone() as CellStatus[,,];
            _currentView = _blankMap.Clone() as CellStatus[,,];
            _visibleCells = new List<Cell>();
            _visibleAgentPositions = new List<Vector3>();
            _visibleAgents = new List<SubmarineAgent>();
        }

        public CellStatus[,,] GetMap() {
            return _map;
        }

        public CellStatus[,,] GetCurrentView() {
            return _currentView;
        }

        public CellStatus[,,] ResetCurrentView() {
            _currentView = _blankMap.Clone() as CellStatus[,,];
            _visibleCells.Clear();
            return _currentView;
        }

        public List<Vector3> GetVisibleAgentPositions() {
            return _visibleAgentPositions;
        }

        public List<SubmarineAgent> GetVisibleAgents() {
            return _visibleAgents;
        }

        public void LookForAgents(SubmarineAgent self, List<SubmarineAgent> others) {

            _visibleAgentPositions.Clear();
            _visibleAgents.Clear();

            List<SubmarineAgent> otherAgents = new List<SubmarineAgent>(others);
            otherAgents.Remove(self);

            foreach (SubmarineAgent otherAgent in otherAgents) {
                Vector3 otherAgentPosition = otherAgent.Controller.GetPosition();

                if (!Physics.Linecast(self.Controller.GetPosition(), otherAgentPosition, LayerMask.GetMask("Map"))) {
                    _visibleAgentPositions.Add(otherAgent.Controller.GetPosition());
                    _visibleAgents.Add(otherAgent);
                }
            }
        }

        public void UpdateCell(Cell cell, CellStatus status) {
            if (_currentView[cell.x, cell.y, cell.z] != CellStatus.covered) {
                _currentView[cell.x, cell.y, cell.z] = status;
            }
            if (_map[cell.x, cell.y, cell.z] != CellStatus.covered) {
                _map[cell.x, cell.y, cell.z] = status;
            }
            _visibleCells.Add(cell);
        }

        public void UpdateMap(CellStatus[,,] currentView) {
            _currentView = currentView.Clone() as CellStatus[,,];
            
            for (int x = 0; x < _currentView.GetLength(0); x++) {
                for (int y = 0; y < _currentView.GetLength(1); y++) {
                    for (int z = 0; z < _currentView.GetLength(2); z++) {
                        if (_map[x, y, z] != CellStatus.covered) {
                            _map[x, y, z] = _currentView[x, y, z];
                        }
                    }
                }
            }
            
        }

        public List<Cell> GetVisibleCells() {
            return _visibleCells;
        }

        public CellStatus GetCellStatus(Cell cell, bool GetCurrentView = false) {
            if(GetCurrentView == true) {
                return _currentView[cell.x, cell.y, cell.z];
            }
            else {
                return _map[cell.x, cell.y, cell.z];
            }
        }

        public void CombineWithMap(CellStatus[,,] otherMap) {
            
            for (int x = 0; x < otherMap.GetLength(0); x++) {
                for (int y = 0; y < otherMap.GetLength(1); y++) {
                    for (int z = 0; z < otherMap.GetLength(2); z++) {

                        switch (otherMap[x,y,z]) {
                            case CellStatus.explored:
                            case CellStatus.covered:
                                _map[x,y,z] = CellStatus.explored;
                                break;
                            case CellStatus.wall:
                                _map[x, y, z] = CellStatus.wall;
                                break;

                        }
                    }
                }
            }
        }
    }
}
