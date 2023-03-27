using System.Collections.Generic;
using MAES3D.Agent;
using MAES3D.Algorithm;
using UnityEngine;

namespace MAES3D.Algorithm.DualStageViewpointPlanner {
    public class DualStageViewpointPlanner : IAlgorithm
    {
        private IAgentController _controller;

        public List<Vector3> viewpoints = new List<Vector3>();

        public List<Cell> localFrontiers = new List<Cell>();
        
        public List<Cell> globalFrontiers = new List<Cell>();

        private RRT RRTmap;

        public bool done = false; // Delete Later

        public void SetController(IAgentController controller) {
            _controller = controller;
            

        }

        public void UpdateLogic()
        {   
            List<Cell> cells = _controller.GetVisibleCells();
            
            if (_controller.GetCurrentStatus() == Status.Idle) 
            {
                Cell destination = null;

                CellStatus[,,] currentView = _controller.GetCurrentView();
                List<Vector3> visibleAgents = _controller.GetVisibleAgentPositions();

                /* Remove visible agents that are further away than the agent's range */
                for (int i = visibleAgents.Count - 1; i > 0; i--) 
                {
                    if (Vector3.Distance(_controller.GetPosition(), visibleAgents[i]) > 1000) 
                    {
                        visibleAgents.RemoveAt(i);
                    }
                }

                if (destination == null) 
                {
                    //destination = SearchMode(cells, currentView, visibleAgents);

                    if (destination != null) 
                    {
                        
                    }
                }
            
                /* MOVE
                 * GO TO CELL */
                if (destination != null) 
                {
                    Debug.DrawLine(_controller.GetPosition(), destination.middle, Color.red, 5);
                    _controller.MoveToCell(destination);
                }
            }


            if(!done){
                RRTmap = new RRT(_controller.GetPosition());
                BuildRRT();
                FrontierFinder9001();
                done = true;
            }
            Debug.Break();
        }

        public void ExploreStage(){

        }

        public void DynamicExpansion(){

        }

        public void RelocationStage(){

        }

        public void BuildRRT()
        {
            for (int i = 0; i < RRTmap.maxIterations; i++)
            {
                Vector3 randomPoint = RRTmap.root.position + RRTmap.RandomPoint();

                AddPointToClosestNode(RRTmap.root, randomPoint);
            }
            RRTmap.TraversePreOrder(RRTmap.root);
        }
        public void AddPointToClosestNode(RRTnode root, Vector3 p){
            // Find the closest node in the tree to p
            RRTnode closestNode = RRTmap.FindNearestNode(root, p);

            RRTnode newNode = new RRTnode(RRTmap.Steer(closestNode, p));
            if(_controller.GetExplorationStatusOfCell(Utility.CoordinateToCell(newNode.position)) == CellStatus.explored){
                // Add p as a child of the closest node
                closestNode.children.Add(newNode);
            }
        }

        public void FrontierFinder9001(){
            List<Cell> viewableCells = _controller.GetVisibleCells();
            foreach (Cell cell in viewableCells)
            {
                if (_controller.GetExplorationStatusOfCell(cell) == CellStatus.explored)
                {    
                    if (_controller.GetExplorationStatusOfCell(new Cell(cell.x + 1, cell.y, cell.z), true) == CellStatus.unexplored ||
                        _controller.GetExplorationStatusOfCell(new Cell(cell.x - 1, cell.y, cell.z), true) == CellStatus.unexplored ||
                        _controller.GetExplorationStatusOfCell(new Cell(cell.x, cell.y + 1, cell.z), true) == CellStatus.unexplored ||
                        _controller.GetExplorationStatusOfCell(new Cell(cell.x, cell.y - 1, cell.z), true) == CellStatus.unexplored ||
                        _controller.GetExplorationStatusOfCell(new Cell(cell.x, cell.y, cell.z + 1), true) == CellStatus.unexplored ||
                        _controller.GetExplorationStatusOfCell(new Cell(cell.x, cell.y, cell.z - 1), true) == CellStatus.unexplored)
                    {
                        localFrontiers.Add(cell);
                    }
                }
            }
            foreach (Cell c in localFrontiers)
            {
                Debug.DrawLine(new Vector3(c.x-0.01f, c.y-0.01f, c.z-0.01f), c.middle, Color.red, 5000);
            }
        }        
    }

    public class RRTnode{
        public Vector3 position;
        public List<RRTnode> children;

        public float gain;

        public RRTnode(Vector3 Position){
            position = Position;
            children = new List<RRTnode>();
        }
    }

    public class RRT
    {
        public int maxIterations = 500;
        public float stepSize = 1f;
        public float goalRadius = 0.5f;

        public RRTnode root;

        public RRT(Vector3 Position){
            root = new RRTnode(Position);

        }

        public Vector3 RandomPoint()
        {
            float x = Random.Range(-10f, 10f);
            float y = Random.Range(-10f, 10f);
            float z = Random.Range(-10f, 10f);

            return new Vector3(x, y, z);
        }

        public Vector3 Steer(RRTnode node, Vector3 target)
        {
            Vector3 dir = target - node.position;
            float length = Mathf.Min(stepSize, dir.magnitude);

            return node.position + dir.normalized * length;
        }

        private void IsInWall(){

        }

        public RRTnode FindNearestNode(RRTnode root, Vector3 p){
            RRTnode closestNode = null;
            float closestDistance = float.MaxValue;
            Queue<RRTnode> queue = new Queue<RRTnode>();
            queue.Enqueue(root);
            while(queue.Count > 0){
                RRTnode current = queue.Dequeue();
                float distance = Vector3.Distance(current.position, p);
                if(distance < closestDistance){
                    closestNode = current;
                    closestDistance = distance;
                }
                foreach(RRTnode child in current.children){
                    queue.Enqueue(child);
                }
            }
            return closestNode;
        }
        public void TraversePreOrder(RRTnode node, bool alternate = true){
            if(node == null) return;
            // Visit the current node
            foreach(RRTnode child in node.children){
                if(alternate)
                    Debug.DrawLine(node.position, child.position, Color.white, 5000);
                else
                    Debug.DrawLine(node.position, child.position, Color.black, 5000);
                TraversePreOrder(child, !alternate);
            }
            // Traverse the right subtree
            // Not applicable since this is a tree, not a binary tree
        }
    }
}