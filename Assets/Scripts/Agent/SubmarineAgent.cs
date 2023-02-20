using MAES3D.Algorithm;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

namespace MAES3D.Agent {
    public class SubmarineAgent : MonoBehaviour {

        public AgentController Controller;
        public IAlgorithm Algorithm;

        private List<Vector3> _visitedPosition;

        private void Awake() {
            Controller = new AgentController(transform);
            _visitedPosition = new List<Vector3>();
        }

        public void LogicUpdate() {
            Algorithm.UpdateLogic();

            _visitedPosition.Add(Controller.GetPosition());
            
            for (int i = 1; i < _visitedPosition.Count; i++) {
                Vector3 c = _visitedPosition[i];

                Debug.DrawLine(c, _visitedPosition[i - 1]);
            }
        }

        public void MovementUpdate() {
            Controller.UpdateMovement();
        }

        void OnDrawGizmosSelected()
        {
            /*
            Gizmos.color = Color.red;
            List<Cell> cells = Controller.GetVisibleCells();

            foreach (Cell c in cells)
                Gizmos.DrawWireSphere(new Vector3(c.x + 0.5f, c.y + 0.5f, c.z + 0.5f), 0.2f);
            */

            Gizmos.color = Color.white;

            for (int i = 1; i < _visitedPosition.Count; i++) {
                Vector3 c = _visitedPosition[i];

                Debug.DrawLine(c, _visitedPosition[i - 1]);
            }
        }
    }
}
