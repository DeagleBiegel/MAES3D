#nullable enable
using UnityEngine;
using MAES3D.Agent.Task;
using System;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace MAES3D.Agent {

    public class AgentController : IAgentController {

        private Transform _transform;

        private ITask? _currentTask;
        private Status _currentStatus;
        private CollisionType _currentCollision;

        private float _moveSpeed = 1.5f;
        private float _turnSpeed = 100f;

        public ExplorationMap ExplorationMap;

        private Stack<Cell> visitedCells = new Stack<Cell>();

        public AgentController(Transform transform) {
            _transform = transform;
            ExplorationMap = new ExplorationMap(SimulationSettings.Width, SimulationSettings.Height, SimulationSettings.Depth);
        }

        public Status GetCurrentStatus() {
            return _currentStatus;
        }

        public CollisionType GetCurrentCollision() {
            return _currentCollision;
        }

        public Vector3 GetPosition() {
            return _transform.position;
        }

        public float GetAngle() {
            return _transform.eulerAngles.y;
        }

        public void UpdateMovement() {

            if (_currentTask == null) {
                _currentStatus = Status.Idle;
            }
            else {
                _currentStatus = Status.Moving;
            }

            MoveInstruction? instruction = _currentTask?.GetInstruction();

            if (instruction != null) {
                ApplyMovement(instruction);
            }

            var isComplete = _currentTask?.IsComplete() ?? false;

            if (isComplete) {
                _currentTask = null;
            }
        }

        private void ApplyMovement(MoveInstruction instruction) {

            Vector3 vel = _transform.forward * instruction.HorizontalSpeed + _transform.up * instruction.VerticalSpeed;

            Cell cellBefore = Utility.CoordinateToCell(_transform.position);

            if (Physics.SphereCast(GetPosition(), 0.4f, vel.normalized, out RaycastHit hit, vel.magnitude)) {

                CollisionType colType;
                if (hit.normal.y < Mathf.Sin(Mathf.Deg2Rad * -45)) {
                    colType = CollisionType.ceiling;
                }
                else if (hit.normal.y > Mathf.Sin(Mathf.Deg2Rad * 45)) {
                    colType = CollisionType.floor;
                }
                else {
                    colType = CollisionType.wall;
                }

                _currentCollision = colType;
                _currentTask = null;
                _currentStatus = Status.Idle;
            }
            else {
                _transform.position += vel;
                _currentCollision = CollisionType.none;
                _transform.rotation *= Quaternion.Euler(0, instruction.TurnSpeed, 0);
            }
            
            
            Cell cellAfter = Utility.CoordinateToCell(_transform.position);

            if(cellBefore != cellAfter) {
                visitedCells.Push(cellBefore);
            }

            if (ExplorationMap.GetMap()[cellAfter.x, cellAfter.y, cellAfter.z] is CellStatus.wall
                or CellStatus.unexplored 
                && visitedCells.Count != 0) {
                visitedCells.Pop();
                visitedCells.Pop();
                visitedCells.Pop();
                _transform.position = visitedCells.Peek().middle;
            }
        }

        public void TeleportTo(Cell cell) {

            TeleportTo(cell.middle);
        }

        public void TeleportTo(Vector3 position) {
            _transform.position = position;
        }

        public void MoveToPosition(Vector3 targetPosition) {
            Cell targetCell = Utility.CoordinateToCell(targetPosition);

            if (ExplorationMap.GetCellStatus(targetCell) == CellStatus.unexplored) {
                Debug.LogWarning($"An wants to move to the position {targetPosition} but it is unexplored\n" +
                                 $"\tCannot perform movement");
                return;
            }

            MoveToCell(targetCell);
        }

        public void MoveToCell(Cell targetCell) {

            if (ExplorationMap.GetCellStatus(targetCell) == CellStatus.unexplored) {
                Debug.LogWarning($"An wants to move to the cell {targetCell} but it is unexplored\n" +
                                 $"\tCannot calculate path to target");
                Debug.Break();
                return;
            }

            List<Cell> fullpath = AStar.FindPath(GetPosition(), targetCell.middle, GetLocalExplorationMap());

            //Could not find a path
            if (fullpath.Count == 0) {
                return;
            }

            List<Cell> shortPath = new List<Cell>();

            Vector3 currentPosition = GetPosition();
            for (int i = 1; i < fullpath.Count; i++) {
                Cell cell = fullpath[i];

                Vector3 direction = cell.middle - currentPosition;
                float distance = Vector3.Distance(currentPosition, cell.middle);

                if (Physics.SphereCast(currentPosition, 0.4f/*HAS to be same size as agents collider*/, direction, out _, distance)) {
                    currentPosition = fullpath[i - 1].middle;
                    shortPath.Add(fullpath[i - 1]);
                }
            }
            shortPath.Add(fullpath[fullpath.Count - 1]);

            List<Vector3> relativeTargets = new List<Vector3>();
            foreach (Cell cell in shortPath) {
                relativeTargets.Add(cell.middle - GetPosition());
            }

            _currentTask = new CompositeMovementTask(relativeTargets, _moveSpeed, _turnSpeed, _transform);
        }

        public void Move(float angle, float distance = 0) {
            if (distance == 0) {
                _currentTask = new InfiniteMovementTask(angle, _moveSpeed);
            }
            else {
                _currentTask = new MovementTask(angle, distance, _moveSpeed);
            }
        }

        public void MoveDiagonal(Vector2 relativeTargetCoordinate) {

            float distance = relativeTargetCoordinate.magnitude;

            float angle = Mathf.Pow(distance, 2) + Mathf.Pow(relativeTargetCoordinate.x, 2) - Mathf.Pow(relativeTargetCoordinate.y, 2);
            angle = Mathf.Acos(angle / (2 * distance * relativeTargetCoordinate.x));

            Move(angle, distance);
        }

        public void MoveForwards(float distance = 0) {
            Move(0, distance);
        }

        public void MoveUp(float distance = 0) {
            Move(90, distance);
        }

        public void MoveDown(float distance = 0) {
            Move(-90, distance);
        }

        public void Turn(float degrees) {
            if (degrees == 0) {
                _currentTask = new InfiniteTurnTask(_turnSpeed);
            }
            else {
                _currentTask = new TurnTask(degrees, _turnSpeed);
            }
        }

        public void TurnTo(float targetAngle) {
            float currentAngle = GetAngle() % 360;
            Turn(currentAngle - targetAngle);
        }
        
        public void StopCurrentTask() {
            throw new System.NotImplementedException();
        }
    
        public CellStatus[,,] GetLocalExplorationMap() {
            return ExplorationMap.GetMap();
        }

        public CellStatus[,,] GetCurrentView() {
            return ExplorationMap.GetCurrentView();
        }

        public List<Cell> GetVisibleCells() {
            return ExplorationMap.GetVisibleCells();
        }

        public List<Vector3> GetVisibleAgentPositions() {
            return ExplorationMap.GetVisibleAgentPositions();
        }

    }
}