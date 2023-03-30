using System.Collections.Generic;
using MAES3D.Agent;
using MAES3D.Algorithm;
using UnityEngine;
using System.Linq;

namespace MAES3D.Algorithm.DualStageViewpointPlanner {
    public class DualStageViewpointPlanner : IAlgorithm
    {
        private IAgentController _controller;

        public List<Vector3> viewpoints = new List<Vector3>();

        public List<Cell> localFrontiers = new List<Cell>();
        
        public List<Cell> globalFrontiers = new List<Cell>();

        public Vector3 lastExplorationDirection = new Vector3(0,0,1);

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
                BuildRRT(FindFrontiers(5));
                done = true;
            }
            Debug.Break();
        }

        public void ExploreStage(){
            
            //Set S_lb, root position P_rob and F_local
            //Update F_LS
            //V <- DynamicRRT()
            BuildRRT(FindFrontiers(5));
            //BestGain <- 0
            //FindBestGainViewpoint();
            //for i from 1 to N do
            //    Compute Gain(Bi)
            //    if Gain(Bi) > BestGain then
            //        BestGain ← Gain(Bi)
            //        BestBranch ← Bi
            //    end
            //end
            //Previous Tree ← Current Tree
            
        }

        public void DynamicExpansion(){
            /*
            Set Slb, root position Prob and FS
            Prune(Previous Tree)
            Rebuild(Previous Tree)
            while Nnew < N do
                Sample u ∼ U[0, 1]
                if u <= θ then
                    Random Sample viewpoints in Slb
                else
                    Random Sample viewpoints around FS
                end
            end
            */
        }

        public void RelocationStage(){
            /*
            Update Fglobal and G
            Flag ← F alse and Dist ← 0
            for i from N to 1 do
                for j from M to 1 do
                    if Fi in FOV(vi) then
                        vS ← vi, FGS ← Fi
                        Dist ← Dist(Fi, vi), Flag ← T rue
                        break;
                    end
                    if Flag is True then
                        break;
                    end
                end
            end
            if Flag is True then
                for i from N to 1 do
                    if FGS in FOV(vi) then
                        if Dist(FGS, vi) < Dist then
                            vS ← vi
                        end
                    end
                end
            else
                Exploration Complete.
            end
            */
        }

        public RRTnode FindBestGainViewpoint(RRT tree){
            RRTnode bestCell = new RRTnode(new Vector3(0,0,0));
            
            return calculateGain(tree.root, bestCell);
        }

        public RRTnode calculateGain(RRTnode treeNode, RRTnode currBestNode, float prevGain = 0, float currBestGain = 0, int branchLength = 0){
            foreach (RRTnode child in treeNode.children)
            {
                float gain = 0;
                if (gain + prevGain > currBestGain)
                    currBestGain = gain + prevGain;
                    currBestNode = child;
                calculateGain(child, currBestNode, gain + prevGain, currBestGain, branchLength+1);                
            }
            return currBestNode;
        }

        public void BuildRRT(List<Cell> frontiers, int iterations = 500)
        {
            float threshold = 0.30f; //x% chance to select a point next to frontier
            for (int i = 0; i < iterations; i++)
            {
                Vector3 targetPoint; 
                if (Random.Range(0f,1f) < threshold)
                {
                    Cell selectedFrontier = frontiers[Random.Range(0,frontiers.Count)];
                    targetPoint = selectedFrontier.middle + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                }
                else
                {                    
                    targetPoint = RRTmap.root.position + new Vector3(Random.Range(-10f, 10f), Random.Range(-10f, 10f), Random.Range(-10f, 10f));
                }
                AddPointToClosestNode(RRTmap.root, targetPoint);

            }
            RRTmap.DrawTree(RRTmap.root);
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

        public List<Cell> FindFrontiers(int frontierAmount){
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
                        bool clusterCheck = false; // Checks if another frontier is nearby
                        foreach (Cell frontier in localFrontiers)  
                        {
                            if ((cell.toVector - frontier.toVector).magnitude < 5) // Checks if another frontier is nearby
                                clusterCheck = true;
                                continue;
                        }
                        if(!clusterCheck)
                            localFrontiers.Add(cell);
                    }
                }
            } 
            foreach (Cell c in FindBestFrontiers(localFrontiers, frontierAmount)) //FindBestFrontiers(localFrontiers, frontierAmount) || localFrontiers
            {
                c.DrawCell();
            }
            return FindBestFrontiers(localFrontiers, frontierAmount);
        }

        public List<Cell> FindBestFrontiers(List<Cell> frontiers, int amount){ //find x best cells in the drone's direction
            Dictionary<Cell, float> bestFrontiers = new Dictionary<Cell, float>();
            
            foreach (Cell frontier in frontiers)
            {
                Vector3 frontierDirection = (frontier.middle - _controller.GetPosition()).normalized;

                float similarity = Vector3.Angle(frontierDirection, lastExplorationDirection.normalized);
                similarity = (lastExplorationDirection - frontierDirection).magnitude;
                if (bestFrontiers.Count < amount)
                {
                    bestFrontiers.Add(frontier, similarity);
                }
                else
                {
                    float maxSimilarity = bestFrontiers.Values.Max();
                    if ( maxSimilarity > similarity)
                    {
                        var largestKey = bestFrontiers.FirstOrDefault(x => x.Value == maxSimilarity).Key;
                        bestFrontiers.Remove(largestKey);
                        bestFrontiers.Add(frontier, similarity);
                    }

                }
            }
            return new List<Cell>(bestFrontiers.Keys);
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
        public float stepSize = 1f;
        public float goalRadius = 0.5f;

        public RRTnode root;

        public RRT(Vector3 Position){
            root = new RRTnode(Position);

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
        public void DrawTree(RRTnode node, bool alternate = true){
            if(node == null) return;
            // Visit the current node
            foreach(RRTnode child in node.children){
                if(alternate)
                    Debug.DrawLine(node.position, child.position, Color.white, 5000);
                else
                    Debug.DrawLine(node.position, child.position, Color.black, 5000);
                DrawTree(child, !alternate);
            }
            // Traverse the right subtree
            // Not applicable since this is a tree, not a binary tree
        }
    }
}