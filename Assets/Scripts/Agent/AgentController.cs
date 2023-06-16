#nullable enable
using UnityEngine;
using MAES3D.Agent.Task;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using System.Threading.Tasks;

namespace MAES3D.Agent {

    public class AgentController : IAgentController {

        private Transform _transform;

        private ITask? _currentTask;
        private Status _currentStatus;
        private CollisionType _currentCollision;

        private float _moveSpeed = 1.5f;
        private float _turnSpeed = 100f;

        MoveInstruction? instruction;

        public ExplorationMap ExplorationMap;

        private Stack<Cell> visitedCells = new Stack<Cell>();

        public AgentController(Transform transform) {
            _transform = transform;
            ExplorationMap = new ExplorationMap(SimulationSettings.Width, SimulationSettings.Height, SimulationSettings.Depth);
        }

        public Status GetCurrentStatus() {
            return _currentStatus;
        }

        public ITask? GetCurrentTask(){
            return _currentTask;
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

        public float GetSpeed(){
            if (instruction != null)
                return Mathf.Sqrt(Mathf.Pow(instruction.HorizontalSpeed, 2) + Mathf.Pow(instruction.VerticalSpeed,2));
            else
                return 0;
        }

        public void UpdateMovement() {

            if (_currentStatus == Status.Busy) {
                return;
            }
            else if (_currentTask == null) {
                _currentStatus = Status.Idle;
            }
            else {
                _currentStatus = Status.Moving;
            }
            

            instruction = _currentTask?.GetInstruction();

            if (instruction != null) {
                ApplyMovement(instruction);
            }

            var isComplete = _currentTask?.IsComplete() ?? false;

            if (isComplete) 
            {
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

        public void MoveToCellCoroutine(Cell targetCell) 
        {
            var position = GetPosition();

            //Check if target cell is explored
            if (ExplorationMap.GetCellStatus(targetCell) == CellStatus.unexplored) {
                Debug.LogWarning($"An wants to move to the cell {targetCell} but it is unexplored\n" +
                                 $"\tCannot calculate path to target");
                Debug.Break();
                return;
            }

            //If agent can go directly to goal
            Vector3 directionToGoal = targetCell.middle - position;
            float distanceToGoal = Vector3.Distance(position, targetCell.middle);
            if (!Physics.SphereCast(position, 0.4f, directionToGoal, out _, distanceToGoal)) {
                _currentTask = new CompositeMovementTask(new List<Vector3> { directionToGoal }, _moveSpeed, _turnSpeed, _transform);
                Debug.DrawLine(position, targetCell.middle, Color.cyan);
                //Debug.Break();
                return;
            }          

            // Set the status of the agent to busy while finding A* Path
            _currentStatus = Status.Busy; 

            Debug.Log("Starting A*");

            // We need a script with MonoBehavior for Coroutine, so we
            // get the a component attatched to the _transform
            var agent = _transform.GetComponent<SubmarineAgent>();

            agent.StartCoroutine(AStar.FindPath(position, targetCell.middle, GetLocalExplorationMap(), (path) => 
            {
                if (path == null || path.Count == 0) 
                {
                    // Handle case where there is no path
                    Debug.Log("No path found");
                    return;
                }

                // Set the fullPath variable to the result of the A* pathfinding coroutine
                List<Cell> fullPath = path;

                // Path found, can start moving
                _currentStatus = Status.Moving;

                //Make a more optimal shorter path
                List<Vector3> shortPath = new List<Vector3>();

                //Remove first element so the first cell is not the on the agent is in
                fullPath.RemoveAt(0);

                //!!
                //!!SHOULD do more itterations to fix a small "issue" with points lining up!!
                //!!
                Vector3 currentPosition = position;
                for (int i = 1; i < fullPath.Count; i++) {
                    Cell cell = fullPath[i];

                    Vector3 direction = cell.middle - currentPosition;
                    float distance = Vector3.Distance(currentPosition, cell.middle);

                    if (Physics.SphereCast(currentPosition, 0.4f/*HAS to be same size as agents collider*/, direction, out _, distance)) {
                        currentPosition = fullPath[i - 1].middle;
                        shortPath.Add(fullPath[i - 1].middle);
                    }
                }
                shortPath.Add(fullPath[fullPath.Count - 1].middle);

                //Make path into relative targets
                List<Vector3> relativePath = new List<Vector3>();
                relativePath.Add(shortPath[0] - position);
                for (int i = 1; i < shortPath.Count; i++) {
                    relativePath.Add(shortPath[i] - shortPath[i - 1]);
                }

                _currentTask = new CompositeMovementTask(relativePath, _moveSpeed, _turnSpeed, _transform);
            }));
        }

        public async void MoveToCellAsync(Cell targetCell) 
        {
            var position = GetPosition();

            //Check if target cell is explored
            if (ExplorationMap.GetCellStatus(targetCell) == CellStatus.unexplored) {
                Debug.LogWarning($"An wants to move to the cell {targetCell} but it is unexplored\n" +
                                 $"\tCannot calculate path to target");
                Debug.Break();
                return;
            }

            //If agent can go directly to goal
            Vector3 directionToGoal = targetCell.middle - position;
            float distanceToGoal = Vector3.Distance(position, targetCell.middle);
            if (!Physics.SphereCast(position, 0.4f, directionToGoal, out _, distanceToGoal)) {
                _currentTask = new CompositeMovementTask(new List<Vector3> { directionToGoal }, _moveSpeed, _turnSpeed, _transform);
                Debug.DrawLine(position, targetCell.middle, Color.cyan);
                //Debug.Break();
                return;
            }

            // Enters busy status while calculating AStar Path
            _currentStatus = Status.Busy;

            //If we cant go there directly, use AStar to find a path
            List<Cell> fullPath = await System.Threading.Tasks.Task.Run(() => AStar.FindPath(position, targetCell.middle, GetLocalExplorationMap()));

            // Path found, can start moving
            _currentStatus = Status.Moving;

            //Could not find a path
            if (fullPath == null) {
                return;
            }

            //Make a more optimal shorter path
            List<Vector3> shortPath = new List<Vector3>();

            //Remove first element so the first cell is not the on the agent is in
            fullPath.RemoveAt(0);

            //!!
            //!!SHOULD do more itterations to fix a small "issue" with points lining up!!
            //!!
            Vector3 currentPosition = position;
            for (int i = 1; i < fullPath.Count; i++) {
                Cell cell = fullPath[i];

                Vector3 direction = cell.middle - currentPosition;
                float distance = Vector3.Distance(currentPosition, cell.middle);

                if (Physics.SphereCast(currentPosition, 0.4f/*HAS to be same size as agents collider*/, direction, out _, distance)) {
                    currentPosition = fullPath[i - 1].middle;
                    shortPath.Add(fullPath[i - 1].middle);
                }
            }
            shortPath.Add(fullPath[fullPath.Count - 1].middle);

            //Make path into relative targets
            List<Vector3> relativePath = new List<Vector3>();
            relativePath.Add(shortPath[0] - position);
            for (int i = 1; i < shortPath.Count; i++) {
                relativePath.Add(shortPath[i] - shortPath[i - 1]);
            }

            _currentTask = new CompositeMovementTask(relativePath, _moveSpeed, _turnSpeed, _transform);
        }

        public void MoveToCell(Cell targetCell) {

            //Check if target cell is explored
            if (ExplorationMap.GetCellStatus(targetCell) == CellStatus.unexplored) {
                Debug.LogWarning($"An wants to move to the cell {targetCell} but it is unexplored\n" +
                                 $"\tCannot calculate path to target");
                Debug.Break();
                return;
            }

            //If agent can go directly to goal
            Vector3 directionToGoal = targetCell.middle - GetPosition();
            float distanceToGoal = Vector3.Distance(GetPosition(), targetCell.middle);
            if (!Physics.SphereCast(GetPosition(), 0.4f, directionToGoal, out _, distanceToGoal)) {
                _currentTask = new CompositeMovementTask(new List<Vector3> { directionToGoal }, _moveSpeed, _turnSpeed, _transform);
                Debug.DrawLine(GetPosition(), targetCell.middle, Color.cyan);
                //Debug.Break();
                return;
            }

            //If we cant go there directly, use AStar to find a path
            List<Cell> fullPath = AStar.FindPath(GetPosition(), targetCell.middle, GetLocalExplorationMap());

            //Could not find a path
            if (fullPath.Count == 0) {
                return;
            }

            //Make a more optimal shorter path
            List<Vector3> shortPath = new List<Vector3>();

            //Remove first element so the first cell is not the on the agent is in
            fullPath.RemoveAt(0);

            //!!
            //!!SHOULD do more itterations to fix a small "issue" with points lining up!!
            //!!
            Vector3 currentPosition = GetPosition();
            for (int i = 1; i < fullPath.Count; i++) {
                Cell cell = fullPath[i];

                Vector3 direction = cell.middle - currentPosition;
                float distance = Vector3.Distance(currentPosition, cell.middle);

                if (Physics.SphereCast(currentPosition, 0.4f/*HAS to be same size as agents collider*/, direction, out _, distance)) {
                    currentPosition = fullPath[i - 1].middle;
                    shortPath.Add(fullPath[i - 1].middle);
                }
            }
            shortPath.Add(fullPath[fullPath.Count - 1].middle);

            //Make path into relative targets
            List<Vector3> relativePath = new List<Vector3>();
            relativePath.Add(shortPath[0] - GetPosition());
            for (int i = 1; i < shortPath.Count; i++) {
                relativePath.Add(shortPath[i] - shortPath[i - 1]);
            }

            _currentTask = new CompositeMovementTask(relativePath, _moveSpeed, _turnSpeed, _transform);
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

        public CellStatus GetExplorationStatusOfCell(Cell cell, bool GetCurrentView = false){
            return ExplorationMap.GetCellStatus(cell, GetCurrentView);
        }
        

    }
}