using MAES3D.Algorithm;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

namespace MAES3D.Agent {
    public class SubmarineAgent : MonoBehaviour {

        public AgentController Controller;
        public IAlgorithm Algorithm;
        public int Id;

        private List<Vector3> _visitedPosition;

        private void Awake() {
            Controller = new AgentController(transform);
            _visitedPosition = new List<Vector3>();
        }

        public void LogicUpdate() {
            Algorithm.UpdateLogic();

            _visitedPosition.Add(Controller.GetPosition());
            
            /*
            for (int i = 1; i < _visitedPosition.Count; i++) {
                Vector3 c = _visitedPosition[i];

                Debug.DrawLine(c, _visitedPosition[i - 1]);
            }
            */
        }

        public void MovementUpdate() {
            Controller.UpdateMovement();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;

            for (int i = 1; i < _visitedPosition.Count; i++) {
                Vector3 c = _visitedPosition[i];

                Debug.DrawLine(c, _visitedPosition[i - 1]);
            }

            CellStatus[,,] map = Controller.GetLocalExplorationMap();

            for (int x = 1; x < map.GetLength(0)-1; x++) 
            {
                for (int y = 1; y < map.GetLength(1)-1; y++) 
                {
                    for (int z = 1; z < map.GetLength(2)-1; z++) 
                    {
                        if (map[x, y, z] == CellStatus.covered) 
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireSphere(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), 0.2f);
                        }
                        else if (map[x, y, z] == CellStatus.unexplored) 
                        {
                            if (map[x + 1, y, z] == CellStatus.explored ||
                                map[x - 1, y, z] == CellStatus.explored ||
                                map[x, y + 1, z] == CellStatus.explored ||
                                map[x, y - 1, z] == CellStatus.explored ||
                                map[x, y, z + 1] == CellStatus.explored ||
                                map[x, y, z - 1] == CellStatus.explored)
                            {                                
                                Gizmos.color = Color.green;
                                Gizmos.DrawWireSphere(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), 0.2f);
                            }
                        }
                    }
                }
            }
        }
    }
}
