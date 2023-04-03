using System.Collections.Generic;
using UnityEngine;

namespace MAES3D.Agent {
    public interface IAgentController {

        public Status GetCurrentStatus();

        public CollisionType GetCurrentCollision();

        public CellStatus[,,] GetLocalExplorationMap();

        public CellStatus[,,] GetCurrentView();

        public List<Cell> GetVisibleCells();

        public List<Vector3> GetVisibleAgentPositions();

        public Vector3 GetPosition();

        public float GetAngle();

        void TeleportTo(Cell cell);

        void TeleportTo(Vector3 position);

        void MoveToPosition(Vector3 targetPosition);

        void MoveToCellAsync(Cell targetCell);

        void MoveToCell(Cell targetCell);

        void Move(float angle, float distance);

        void MoveDiagonal(Vector2 relativeTargetCoordinate);

        void MoveForwards(float distance = 0);

        void MoveUp(float distance = 0);

        void MoveDown(float distance = 0);

        void Turn(float degrees);

        void TurnTo(float targetAngle);

        void StopCurrentTask();
        
        public CellStatus GetExplorationStatusOfCell(Cell cell, bool GetCurrentView = false);
    }
}
