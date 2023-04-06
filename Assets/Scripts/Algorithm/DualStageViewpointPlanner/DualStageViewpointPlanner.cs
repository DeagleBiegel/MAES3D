using System.Collections.Generic;
using MAES3D.Agent;
using MAES3D.Algorithm;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using TreeEditor;
using Unity.VisualScripting.FullSerializer;

namespace MAES3D.Algorithm.DualStageViewpointPlanner {
    public class DualStageViewpointPlanner : IAlgorithm
    {
        private IAgentController _controller;

        public List<Vector3> viewpoints = new List<Vector3>();

        public List<Cell> localFrontiers = new List<Cell>();
        
        public List<Cell> globalFrontiers = new List<Cell>();

        public Vector3 lastExplorationDirection = new Vector3(0,0,1);

        private RRT globalGraph;

        private RRT localGraph;

        public bool done = false; // Delete Later
        public int count = 0; // Delete Later

        public Dictionary<RRTnode, float> viewpointGains = new Dictionary<RRTnode, float>();



        float lambda2 = 5;
        float frontierRadius = 1f;
        float planningHorizon = 15f;

        public void SetController(IAgentController controller) {
            _controller = controller;
            

        }

        public void UpdateLogic()
        {   
            if(!done){
                localGraph = new RRT(_controller.GetPosition());
                ExploreStage();
                done = true;
            }
            Debug.Log(count);
            Debug.Break();
        }

        public void ExploreStage(){
            
            // Set S_lb, root position P_rob and F_local
            // Update F_LS
            // V <- DynamicRRT()
            //DynamicExpansion(); // <- Mangler ny rootLocation

            /* ---Det her for Stack Overflow---
            RRTnode bestBranch = FindBestGainViewpoint(localGraph.root);
            Cell bestCell = Utility.CoordinateToCell(bestBranch.position);
            bestCell.DrawCell();
            */

            // BestGain <- 0
            // for i from 1 to N do
            //    Compute Gain(Bi)
            //    if Gain(Bi) > BestGain then
            //        BestGain ← Gain(Bi)
            //        BestBranch ← Bi
            //    end
            // end
            // Previous Tree ← Current Tree
            
        }

        public void DynamicExpansion(RRTnode newLocation){
            // Set Slb, root position Prob and FS
            SetNewRootPosition(localGraph, newLocation); // Set new root position

            // Prune(Previous Tree)
            PruneGraph(localGraph.root);
            /*
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
            BuildRRT(FindFrontiers(5));
            //localGraph.DrawTree(localGraph.root); // Draws the localGraph Tree
        }   

        public void RelocationStage(){ // When there is no local frontiers within the planning horizon, the planner switches from the exploration stage to the relocation stage
            /*
            Update Fglobal and G
            Flag ← False and Dist ← 0
            */
            
            /*
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
            */

            /*
            if Flag is True then
                for i from N to 1 do
                    if FGS in FOV(vi) then
                        if Dist(FGS, vi) < Dist then
                            vS ← vi
                        end
                    end
                end
            else
            */
            /*
                Exploration Complete.
            end
            */
        }

        private void PruneGraph(RRTnode rootNode){
            foreach (RRTnode child in rootNode.children) // remove child if out of planning horizon
            {
                if(!IsInPlanningHorizon(child)){
                    child.RemoveFromParent();
                }
            }
            foreach (RRTnode child in rootNode.children) // Continue pruning on children inside planning horizon
            {
                PruneGraph(child);
            }
        }

        private void SetNewRootPosition(RRT graph, RRTnode newRoot){
            List<RRTnode> branch = new List<RRTnode>();
            RRTnode currNode = newRoot;
            RRTnode target = graph.root;

            while (currNode != target) // Find the entire branch from new root location to old root location
            {
                branch.Add(currNode.parent);
                currNode = currNode.parent;
            }
            branch.Reverse();
            foreach (RRTnode node in branch) // traverse through the branch and reverse the order of child to parent
            {
                target.RemoveChild(node);
                node.AddChild(target);
                target = node;
            }
            currNode.AddChild(currNode.parent);
            graph.root = newRoot;
        }

        private bool IsInPlanningHorizon(RRTnode node){
            if (node.position.x - _controller.GetPosition().x > planningHorizon || node.position.x - _controller.GetPosition().x < -planningHorizon ||
                node.position.y - _controller.GetPosition().y > planningHorizon || node.position.y - _controller.GetPosition().y < -planningHorizon ||
                node.position.z - _controller.GetPosition().z > planningHorizon || node.position.z - _controller.GetPosition().z < -planningHorizon)
            {
                return false;
            }
            else {
                return true;
            }
        }


/*
        private void FindViewpointsInGraph(RRTnode root, List<RRTnode> viewpoints){
            List<RRTnode> sumViewPoints = new List<RRTnode>();
            foreach (RRTnode child in root.children)
            {
                FindViewpointsInGraph(child, sumViewPoints);
                sumViewPoints.Add(child);
            }
            foreach (RRTnode item in sumViewPoints)
            {
                Debug.Log(item.position);
            }
        }
*/

        public RRTnode FindBestGainViewpoint(RRTnode rootNode){
            CalculateBestGainViewpoint(rootNode);
            RRTnode n = viewpointGains.OrderByDescending(x => x.Value).First().Key;
            Debug.Log($"the best: {n.position}");
            return n;
        }

        public void CalculateBestGainViewpoint(RRTnode treeNode, int branchLength = 0){

            foreach (RRTnode child in treeNode.children)
            {
                float childBestGain = GetBranchGain(child, branchLength);

                CalculateBestGainViewpoint(child, branchLength + 1);
                viewpointGains.Add(child, childBestGain);
            }
            
        }

        public float GetBranchGain(RRTnode node, int currentBranchLength) {

            float viewpointGain = GetViewPointGain(node, currentBranchLength);
            float branchGain = GetBranchViewpointGain(node, currentBranchLength);
            return viewpointGain + branchGain; // * Mathf.Exp(DTW(Bi) * lambda1);
        }

        public float GetBranchViewpointGain(RRTnode node, int currentBranchLength) {
            if (node.parent == null) {
                return GetViewPointGain(node, currentBranchLength);
            }
            else {
                return GetViewPointGain(node, currentBranchLength) + GetBranchViewpointGain(node.parent, currentBranchLength - 1);
            }
        }

        public float GetViewPointGain(RRTnode node, int currentBranchLength) {
            float gain = VectorGain(node);
            return VectorGain(node) * currentBranchLength;// * Mathf.Exp(-currentBranchLength * lambda2);
        }

        public float VectorGain(RRTnode node, int range = 2) {
            Vector3 pos = node.position;
            Cell positionCell = Utility.CoordinateToCell(pos);

            float vectorGain = 0;
            for (int x = -range; x <= range; x++) {
                for (int y = -range; y <= range; y++) {
                    for (int z = -range; z <= range; z++) {
                        Cell targetCell = new Cell(x, y, z) + positionCell;
                        if(Vector3.Distance(pos, targetCell.middle) <= range) {
                            // TODO Add raycast check
                            if (_controller.GetLocalExplorationMap()[targetCell.x, targetCell.y, targetCell.z] == CellStatus.unexplored) {
                                vectorGain += 1;
                            }
                        }
                    }
                }
            }
            return vectorGain;
        }


        private float DTW(List<RRTnode> branch1, List<RRTnode> branch2) {
            // Aner ikke om det virker
            // Gør det nok 99% ikke tbh
            // Fordi de to branches bliver sammenlignet i global space i stedet for relativt til deres træ root
            // Ved ikke hvordan vi lige skal fikse det
            // Man kunne bare tage vinklen fra træets root til target node og sidste træs root til den target node(hvilket vi allerede har i lastExplorationDirection) og sige at det er similarity da vi i stidste ende bare er iterreseret i om de er samme retning (tror jeg)

            float[,] arr = new float[branch1.Count, branch2.Count];
            for (int i = 0; i < arr.GetLength(0); i++) {
                for (int j = 0; j < arr.GetLength(1); j++) {
                    arr[i, j] = float.PositiveInfinity;
                }
            }
            arr[0,0] = 0;

            for (int i = 1; i <= arr.GetLength(0); i++) {
                for (int j = 1; j <= arr.GetLength(1); j++) {
                    float cost = Vector3.Distance(branch1[i - 1].position, branch2[j - 1].position);
                    float lastMin = Mathf.Min(arr[i - 1, j], Mathf.Min(arr[i, j - 1], arr[i - 1, j - 1]));
                    arr[i, j] = cost + lastMin;
                }
            }
            return arr[arr.GetLength(0), arr.GetLength(1)];
        }

        public void BuildRRT(List<Cell> frontiers, int iterations = 500)
        {   
            float threshold = 0.30f; // x% chance to select a point next to frontier
            for (int i = 0; i < iterations; i++)
            {
                Vector3 targetPoint; 
                if (Random.Range(0f,1f) < threshold)
                {
                    Cell selectedFrontier = frontiers[Random.Range(0,frontiers.Count)];
                    targetPoint = selectedFrontier.middle + Random.insideUnitSphere * frontierRadius;
                }
                else
                {                    
                    targetPoint = localGraph.root.position + new Vector3(Random.Range(-planningHorizon, planningHorizon), Random.Range(-planningHorizon, planningHorizon), Random.Range(-planningHorizon, planningHorizon));
                }
                AddPointToClosestNode(targetPoint);
            }
        }
        
        public void AddPointToClosestNode(Vector3 p){
            // Find the closest node in the tree to p
            RRTnode closestNode = localGraph.FindNearestNode(p);

            RRTnode newNode = new RRTnode(localGraph.Steer(closestNode, p));
            if(_controller.GetExplorationStatusOfCell(Utility.CoordinateToCell(newNode.position)) == CellStatus.explored){
                // Add p as a child of the closest node
                closestNode.AddChild(newNode);
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
                        }
                        if(!clusterCheck)
                            localFrontiers.Add(cell);
                    }
                }
            } 
            foreach (Cell c in FindBestFrontiers(localFrontiers, frontierAmount)) // FindBestFrontiers(localFrontiers, frontierAmount) || localFrontiers
            {
                c.DrawCell(Color.green);
            }
            return FindBestFrontiers(localFrontiers, frontierAmount);
        }

        public List<Cell> FindBestFrontiers(List<Cell> frontiers, int amount){ // find x best cells in the drone's direction
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
        public RRTnode parent;

        public float gain;

        public RRTnode(Vector3 Position) {
            position = Position;
            children = new List<RRTnode>();
            parent = null;
        }

        public void AddChild(RRTnode child) {
            child.parent = this;
            children.Add(child);
        }
        public void RemoveChild(RRTnode child){ // Removes given child from "this" node
            this.children.Remove(child);
        }

        public void RemoveFromParent(){ // Removes "this" child from parent
            this.parent.children.Remove(this);
        }

        public void DrawNode(Color? c = null){
            Color color = c ?? Color.red;
            Vector3 newPos = position + new Vector3(-0.5f, -0.5f, -0.5f);
            Debug.DrawLine(newPos + new Vector3(0.25f, 0.25f, 0.25f), newPos + new Vector3(0.75f, 0.75f, 0.75f), color, 5000);
            Debug.DrawLine(newPos + new Vector3(0.75f, 0.25f, 0.25f), newPos + new Vector3(0.25f, 0.75f, 0.75f), color, 5000);
            Debug.DrawLine(newPos + new Vector3(0.25f, 0.25f, 0.75f), newPos + new Vector3(0.75f, 0.75f, 0.25f), color, 5000);
            Debug.DrawLine(newPos + new Vector3(0.75f, 0.25f, 0.75f), newPos + new Vector3(0.25f, 0.75f, 0.25f), color, 5000);
        }

    }

    public class RRT
    {
        public float stepSize = 1f;

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

        public RRTnode FindNearestNode(Vector3 p){
            RRTnode closestNode = null;
            float closestDistance = float.MaxValue;
            Queue<RRTnode> queue = new Queue<RRTnode>();
            queue.Enqueue(this.root);
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