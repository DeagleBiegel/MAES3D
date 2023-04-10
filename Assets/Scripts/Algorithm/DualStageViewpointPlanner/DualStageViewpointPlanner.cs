using System.Collections.Generic;
using MAES3D.Agent;
using MAES3D.Algorithm;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using TreeEditor;
using Unity.VisualScripting.FullSerializer;
using System;

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

        private RRTnode location = null;

        private Cell prevCell;

        private bool firstTick = true;

        private bool relocated = false;

        private bool done = false; // delete later

        private int count = 0; // delete later

        private bool initializing = true;

        private float time;

        float lambda2 = 5;
        float frontierRadius = 1f;
        float planningHorizon = 15f;

        public void SetController(IAgentController controller) {
            _controller = controller;
        }

        public void UpdateLogic()
        {               
            time += Time.fixedDeltaTime;
            Cell currCell = Utility.CoordinateToCell(_controller.GetPosition());

            if (_controller.GetVisibleCells().Count == 0) 
            {
                return;
            }
            else 
            {
                initializing = false;
            }

            if (firstTick)
            {
                localGraph = new RRT(_controller.GetPosition());
                globalGraph = new RRT(_controller.GetPosition());
                firstTick = false;
            }

            if (currCell != prevCell)
            {
                FindFrontiersWhileMoving();
            }
            
            
            if (_controller.GetCurrentStatus() == Status.Idle)
            {
                time = 0;
                RRTnode newDestination = ExplorationStage(location, relocated);

                if (location != newDestination && localFrontiers.Count != 0)
                {
                    //newDestination.DrawNode(Color.magenta);
                    _controller.MoveToCellAsync(Utility.CoordinateToCell(newDestination.position));
                    location = newDestination;
                }
                else if (localFrontiers.Count == 0) // If there are no more local frontiers after exploration stage
                {
                    UpdateGlobalFrontiers();

                    if (globalFrontiers.Count == 0) // If there are no more global frontiers after exploration stage
                    {    
                        Debug.Log("Simulation Over: Cannot find any more frontiers");

                        //CheckOtherAgentsFrontiers();

                        if (globalFrontiers.Count == 0) 
                            done = true;
                    }
                    else
                    {
                        Debug.Log("Relocation Stage");
                        Cell globalFrontierDestination = RelocationStage();
                        //globalFrontierDestination.DrawCell(Color.magenta);
                        _controller.MoveToCellAsync(globalFrontierDestination);
                        newDestination = globalGraph.FindNearestNode(globalFrontierDestination.toVector);
                        location = newDestination;
                        relocated = true;
                    }
                }

            }

            prevCell = currCell;
            /*
            if(_controller.GetCurrentStatus() == Status.Idle){
                ExplorationStage(destination);
                _controller.MoveToCellAsync(Utility.CoordinateToCell(CalculateBestDestination().position));
                done = true;
            }
            */
            //Debug.Break();
            
        }

        public void GetFrontiersFromAgent(List<Cell> frontiers) 
        {
            foreach(Cell cell in frontiers) 
            {
                if (!globalFrontiers.Contains(cell)) 
                {
                    globalFrontiers.Add(cell);
                }
            }
        }

        public void CheckOtherAgentsFrontiers() 
        {
               List<SubmarineAgent> agents = new List<SubmarineAgent>(GameObject.FindObjectsOfType<SubmarineAgent>());
               bool updated = false;
               
               foreach(SubmarineAgent agent in agents) 
               {
                    if (_controller.GetPosition() == agent.Controller.GetPosition())
                        continue;

                    if (!Physics.Raycast(_controller.GetPosition(), agent.Controller.GetPosition(), out RaycastHit hit, (_controller.GetPosition() - agent.Controller.GetPosition()).magnitude)) 
                    {
                        DualStageViewpointPlanner algo = agent.Algorithm as DualStageViewpointPlanner;

                        foreach(Cell cell in algo.globalFrontiers) 
                        {
                            if (!globalFrontiers.Contains(cell)) 
                            {
                                globalFrontiers.Add(cell);
                                updated = true;
                            }
                        }
                    }
               }

               if(updated) 
               {
                    UpdateGlobalFrontiers();
               }
        }

        public RRTnode ExplorationStage(RRTnode location = null, bool relocated = false){
            
            // Set S_lb, root position P_rob and F_local
            // Update F_LS
            // V <- DynamicRRT()

            DynamicExpansion(location, relocated); // <- Mangler ny rootLocation

            // BestGain <- 0
            // for i from 1 to N do
            //    Compute Gain(Bi)
            //    if Gain(Bi) > BestGain then
            //        BestGain ← Gain(Bi)
            //        BestBranch ← Bi
            //    end
            // end
            // Previous Tree ← Current Tree            

            return CalculateBestDestination(); // destination 
        }

        public void DynamicExpansion(RRTnode location, bool relocated = false){ //Changes Rootnode to given RRTnode, Prunes the local planning horizon, rebuilds the graph from the new root
            if (location != null)
            {    
                if (relocated)
                {
                    localGraph = globalGraph.DeepCopy();
                    location = localGraph.FindNearestNode(location.position);
                }

                //Debug.Log("NewRoot + Prune");
                // Set Slb, root position Prob and FS
                SetNewRootPosition(localGraph, location); // Set new root position, if newLocation == null it does nothing
                // Prune(Previous Tree)
                PruneGraph(localGraph);

            }
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
            /*
            localGraph.DrawTree(localGraph.root, true, 10); // Draws the localGraph Tree
            localGraph.root.DrawNode(Color.blue);
            globalGraph.DrawTree2(globalGraph.root, true, 5); // Draws the globalGraph Tree
            globalGraph.root.DrawNode(Color.cyan);
            */
        }   

        public Cell RelocationStage(){
            float dist = float.PositiveInfinity;
            Cell bestFrontier = null;
            foreach (Cell frontier in globalFrontiers)
            {
                float newdist = (_controller.GetPosition() - frontier.toVector).magnitude;
                if (newdist < dist)
                {
                    newdist = dist;
                    bestFrontier = frontier;
                }
            }
            return bestFrontier;
        }
        
        public RRTnode RelocationStage2(){ // When there is no local frontiers within the planning horizon, the planner switches from the exploration stage to the relocation stage
            
            //Update Fglobal and G // This is being updated in 
            UpdateGlobalFrontiers();

            //Flag ← False and Dist ← 0
            
            bool flag = false; // Has a frontier and a viewpoint within line of sight of each other been found? 
            float dist = 0; // Distance between selected viewpoint and selected frontier

            RRTnode selectedViewpoint = null;
            Cell selectedFrontier = null;
            
            /*
            for i from N to 1 do
                for j from M to 1 do
                    if Fi in FOV(vi) then
                        vS ← vi, FGS ← Fi
                        Dist ← Dist(Fi, vi), Flag ← True
                        break;
                    end
                    if Flag is True then
                        break;
                    end
                end
            end
            */
            for (int i = globalFrontiers.Count - 1; i >= 0; i--) // Frontiers in global frontier
            {
                for (int j = 0; j < globalGraph.viewPoints.Count - 1; j--) // viewpoints in Global Graph
                {
                    if (IsInFoW(globalFrontiers[i], globalGraph.viewPoints[j])) // "F_i" in field of view of "v_i"
                    {
                        selectedViewpoint = globalGraph.viewPoints[j];
                        selectedFrontier = globalFrontiers[i];
                        dist = (selectedFrontier.toVector - selectedViewpoint.position).magnitude;
                        flag = true;
                        break;
                    }
                }
                if (flag)
                {
                    break;
                }
            }
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
            if (flag)
            {
                for (int i = 0; i >= globalGraph.viewPoints.Count-1; i++)
                {
                    if (IsInFoW(selectedFrontier, globalGraph.viewPoints[i]))
                    {
                        if ((selectedFrontier.toVector - selectedViewpoint.position).magnitude < dist )
                        {
                            selectedViewpoint = globalGraph.viewPoints[i];
                        }
                    }
                }
            }
            /*
                Exploration Complete.
            end
            */
            if (!(selectedViewpoint == null))
            {
                return selectedViewpoint;
            }
            else
            {
                Debug.Log("Algorithm Completed");
                return selectedViewpoint;
            }
        }

        public void UpdateGlobalFrontiers(){
            for (int i = globalFrontiers.Count-1; i >= 0; i--)
            {
                if (!ValidFrontier(globalFrontiers[i]))
                {
                    globalFrontiers.RemoveAt(i);
                }
            }
        }
        private RRTnode CalculateBestDestination(){
            // ---Det her for Stack Overflow---
            RRTnode bestBranch = FindBestGainViewpoint(localGraph.root);
            /*
            Cell bestCell = Utility.CoordinateToCell(bestBranch.position);
            bestCell.DrawCell();
            */
            return bestBranch;
        }

        private bool IsInFoW(Cell frontier, RRTnode viewpoint){
            
            return !Physics.SphereCast(viewpoint.position, 0.4f, frontier.toVector, out _, (viewpoint.position - frontier.toVector).magnitude);
        }

        private void PruneGraph(RRT graph){
            /*
            */
            for (int i = 0; i < graph.viewPoints.Count; i++)
            {
                RRTnode viewpoint = graph.viewPoints[i];
                /*
                */
                if (!IsInPlanningHorizon(graph, viewpoint))
                {
                    //viewpoint.DrawNode();
                    if (!(viewpoint.parent == null))
                    {
                        viewpoint.RemoveChildFromParent();
                    }
                }
            }
        }

        private void SetNewRootPosition(RRT graph, RRTnode newRoot, bool relocated = false){
            List<RRTnode> branch = new List<RRTnode>();
            RRTnode currNode = newRoot;
            RRTnode target = graph.root;
            /*
            if (relocated)
            {
                currNode = globalGraph.FindNearestNode(newRoot.position);
            }
            */

            while (currNode != target) // Find the entire branch from new root location to old root location
            {
                branch.Add(currNode);
                currNode = currNode.parent;
            }
            branch.Reverse();
            foreach (RRTnode node in branch) // traverse through the branch and reverse the order of child to parent
            {
                node.AddChild(target);
                node.parent.RemoveChild(node);
                node.parent = null;
                target = node;
            }
            graph.root = newRoot;
        }

        private bool IsInPlanningHorizon(RRT graph, RRTnode node){
            if (node.position.x - graph.root.position.x > planningHorizon || node.position.x - graph.root.position.x < -planningHorizon ||
                node.position.y - graph.root.position.y > planningHorizon || node.position.y - graph.root.position.y < -planningHorizon ||
                node.position.z - graph.root.position.z > planningHorizon || node.position.z - graph.root.position.z < -planningHorizon)
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
            try 
            {
                Dictionary<RRTnode, float> viewpointGains = new Dictionary<RRTnode, float>();
                CalculateBestGainViewpoint(rootNode, viewpointGains);
                RRTnode n = viewpointGains.OrderByDescending(x => x.Value).First().Key;
                return n;
            }
            catch
            {
                return null;
            }
        }

        public void CalculateBestGainViewpoint(RRTnode treeNode, Dictionary<RRTnode, float> dictViewpointGains, int branchLength = 0){

            foreach (RRTnode child in treeNode.children)
            {
                float childBestGain = GetBranchGain(child, branchLength);

                CalculateBestGainViewpoint(child, dictViewpointGains, branchLength + 1);
                dictViewpointGains.Add(child, childBestGain);
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
            float gainmod = Mathf.Exp(-currentBranchLength * 0.01f);

            //if(gain != 0 && gain * currentBranchLength != 0)
            //    Debug.Log($"\tnode: {node.position}\n\t\tgRaw: {gain} * {gainmod} \n\t\tleng: {currentBranchLength}\n\t\tgRet: {gain * gainmod}");

            return gain * gainmod;
            //return VectorGain(node) * currentBranchLength;// * Mathf.Exp(-currentBranchLength * lambda2);
        }

        public float VectorGain(RRTnode node, int range = 1) {
            Vector3 pos = node.position;
            Cell posCell = Utility.CoordinateToCell(pos);
            Cell positionCell = Utility.CoordinateToCell(pos);

            float vectorGain = 0;
            for (int x = -range; x <= range; x++) {
                for (int y = -range; y <= range; y++) {
                    for (int z = -range; z <= range; z++) {
                        if (x == 0 && y == 0 && z == 0) break;

                        Cell targetCell = new Cell(x + positionCell.x, y + positionCell.y, z + positionCell.z);

                        if(Vector3.Distance(posCell.middle, targetCell.middle) <= range) {
                            // TODO Add raycast check
                            CellStatus status = _controller.GetLocalExplorationMap()[targetCell.x, targetCell.y, targetCell.z];
                            if (status == CellStatus.unexplored) {
                                vectorGain += 10;
                            }
                            else if (status == CellStatus.wall) {
                                vectorGain -= 1;
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

        public void BuildRRT(List<Cell> frontiers, int iterations = 200)
        {   
            float threshold = 0.30f; // x% chance to select a point next to frontier
            for (int i = 0; i < iterations; i++)
            {
                Vector3 targetPoint; 
                if (UnityEngine.Random.Range(0f,1f) < threshold && frontiers.Count != 0)
                {
                    Cell selectedFrontier = frontiers[UnityEngine.Random.Range(0,frontiers.Count)];
                    targetPoint = selectedFrontier.middle + new Vector3(UnityEngine.Random.Range(-frontierRadius, frontierRadius), UnityEngine.Random.Range(-frontierRadius, frontierRadius), UnityEngine.Random.Range(-frontierRadius, frontierRadius));//Random.insideUnitSphere * frontierRadius;
                }
                else
                {                    
                    targetPoint = localGraph.root.position + new Vector3(UnityEngine.Random.Range(-planningHorizon, planningHorizon), UnityEngine.Random.Range(-planningHorizon, planningHorizon), UnityEngine.Random.Range(-planningHorizon, planningHorizon));
                }
                AddPointToClosestNode(targetPoint);
            }
        }
        
        public void AddPointToClosestNode(Vector3 p){
            /*--- Find and add node to local tree---*/
            // Find the closest node in the tree to p
            RRTnode closestNode = localGraph.FindNearestNode(p);

            RRTnode newNode = new RRTnode(localGraph.Steer(closestNode, p));
            if(_controller.GetExplorationStatusOfCell(Utility.CoordinateToCell(newNode.position)) == CellStatus.explored){
                // Add p as a child of the closest node
                //Debug.Log(newNode.position);
                closestNode.AddChild(newNode);
            }

            /*---Find and add node to global tree---*/
            RRTnode closestGlobalNode = globalGraph.FindNearestNode(closestNode.position);
            RRTnode newglobalNode = new RRTnode(globalGraph.Steer(closestGlobalNode, p));
            if(_controller.GetExplorationStatusOfCell(Utility.CoordinateToCell(newglobalNode.position)) == CellStatus.explored){
                // Add p as a child of the closest node
                //Debug.Log(newglobalNode.position);
                closestGlobalNode.AddChild(newglobalNode);
            }
        }

        public void FindFrontiersWhileMoving(){
            List<Cell> viewableCells = _controller.GetVisibleCells();
            foreach (Cell frontier in viewableCells)
            {
                if (ValidFrontier(frontier) && !globalFrontiers.Contains(frontier))
                {
                    globalFrontiers.Add(frontier);
                }
            }
        }

        public List<Cell> FindFrontiers(int frontierAmount){
            localFrontiers = new List<Cell>();
            List<Cell> viewableCells = _controller.GetVisibleCells();
            foreach (Cell cell in viewableCells)
            {
                if (_controller.GetExplorationStatusOfCell(cell) == CellStatus.explored)
                {    
                    if (ValidFrontier(cell))
                    {
                        localFrontiers.Add(cell);
                    }
                }
            } 

            List<Cell> bestFrontiers = FindBestFrontiers(localFrontiers, frontierAmount);
            /*
            foreach (Cell c in bestFrontiers) // FindBestFrontiers(localFrontiers, frontierAmount) || localFrontiers
            {
                c.DrawCell(Color.green);
            }
            */
            return bestFrontiers;
        }

        private bool ValidFrontier(Cell cell){
            if (_controller.GetLocalExplorationMap()[cell.x, cell.y, cell.z] == CellStatus.wall)
            {
                return false;
            }

            if (_controller.GetExplorationStatusOfCell(new Cell(cell.x + 1, cell.y, cell.z), false) == CellStatus.unexplored ||
                _controller.GetExplorationStatusOfCell(new Cell(cell.x - 1, cell.y, cell.z), false) == CellStatus.unexplored ||
                _controller.GetExplorationStatusOfCell(new Cell(cell.x, cell.y + 1, cell.z), false) == CellStatus.unexplored ||
                _controller.GetExplorationStatusOfCell(new Cell(cell.x, cell.y - 1, cell.z), false) == CellStatus.unexplored ||
                _controller.GetExplorationStatusOfCell(new Cell(cell.x, cell.y, cell.z + 1), false) == CellStatus.unexplored ||
                _controller.GetExplorationStatusOfCell(new Cell(cell.x, cell.y, cell.z - 1), false) == CellStatus.unexplored)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public List<Cell> FindBestFrontiers(List<Cell> frontiers, int amount){ // find x best cells in the drone's direction and store rest of the frontiers in global frontiers
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
            List<Cell> selectedFrontiers = new List<Cell>(bestFrontiers.Keys);

            foreach (Cell frontier in frontiers)
            {

                if (!selectedFrontiers.Contains(frontier))
                {
                    
                    globalFrontiers.Add(frontier);
                }
             
            }

            return selectedFrontiers;
        }      
        
          
    }

    public class RRTnode{
        public RRT graph;
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
            if (child == null) {
                throw new ArgumentNullException(nameof(child), "Child node cannot be null.");
            }
            
            if (children.Contains(child)) {
                throw new ArgumentException("Child node has already been added.", nameof(child));
            }

            if (graph == null) {
                throw new InvalidOperationException("Graph object has not been set.");
            }
            
            child.parent = this;
            children.Add(child);
            child.graph = this.graph;
            child.graph.viewPoints.Add(child);
        }

        public void RemoveChild(RRTnode child){ // Removes given child from "this" node
            if (child == null) {
                throw new ArgumentNullException(nameof(child));
            }

            if (!this.children.Contains(child)) {
                throw new InvalidOperationException("The specified node is not a child of this node.");
            }

            this.children.Remove(child);
            child.parent = null;
            child.graph.viewPoints.Remove(child);
        }

        public void RemoveChildFromParent(){
            
            if (this.parent == null) {
                throw new ArgumentNullException(nameof(this.parent), "Parent node cannot be null.");
            }
            this.parent.RemoveChild(this);
        }



        public void RemoveFromParent(){ // Removes "this" child from parent
            if (this.parent == null) {
                throw new ArgumentNullException(this.position.ToString());
            }
            this.parent.RemoveChild(this);
        }

        public void DrawNode(Color? c = null, float duration = 10){
            Color color = c ?? Color.red;
            Vector3 newPos = position + new Vector3(-0.5f, -0.5f, -0.5f);
            Debug.DrawLine(newPos + new Vector3(0.25f, 0.25f, 0.25f), newPos + new Vector3(0.75f, 0.75f, 0.75f), color, duration);
            Debug.DrawLine(newPos + new Vector3(0.75f, 0.25f, 0.25f), newPos + new Vector3(0.25f, 0.75f, 0.75f), color, duration);
            Debug.DrawLine(newPos + new Vector3(0.25f, 0.25f, 0.75f), newPos + new Vector3(0.75f, 0.75f, 0.25f), color, duration);
            Debug.DrawLine(newPos + new Vector3(0.75f, 0.25f, 0.75f), newPos + new Vector3(0.25f, 0.75f, 0.25f), color, duration);
        }

    }

    public class RRT
    {
        public float stepSize = 1f;

        public RRTnode root;

        public List<RRTnode> viewPoints = new List<RRTnode>();

        public RRT(Vector3 Position){
            root = new RRTnode(Position);
            root.graph = this;
            viewPoints.Add(root);

        }

        public Vector3 Steer(RRTnode node, Vector3 target)
        {
            Vector3 dir = target - node.position;
            float length = Mathf.Min(stepSize, dir.magnitude);

            return node.position + dir.normalized * length;
        }

        public RRT DeepCopy(){
            RRT copy = (RRT)this.MemberwiseClone();
            
            // create new instances of reference types
            copy.root = new RRTnode(this.root.position);
            copy.root.graph = copy;
            copy.viewPoints = new List<RRTnode>();
            
            // recursively copy the tree structure
            CopyTree(copy.root, this.root);
            
            return copy;
        }

        private void CopyTree(RRTnode copyNode, RRTnode originalNode)
        {
            foreach (RRTnode child in originalNode.children)
            {
                RRTnode copyChild = new RRTnode(child.position);
                copyNode.AddChild(copyChild);
                CopyTree(copyChild, child);
            }
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

        
        public void DrawTree(RRTnode node, bool alternate = true, float duration = 10){
            if(node == null) return;
            // Visit the current node
            foreach(RRTnode child in node.children){
                if(alternate)
                    Debug.DrawLine(node.position, child.position, Color.white, duration);
                else
                    Debug.DrawLine(node.position, child.position, Color.black, duration);
                DrawTree(child, !alternate, duration);
            }
            // Traverse the right subtree
            // Not applicable since this is a tree, not a binary tree
        }
        public void DrawTree2(RRTnode node, bool alternate = true, float duration = 10){
            if(node == null) return;
            // Visit the current node
            foreach(RRTnode child in node.children){
                if(alternate)
                    Debug.DrawLine(node.position, child.position, Color.yellow, duration);
                else
                    Debug.DrawLine(node.position, child.position, Color.grey, duration);
                DrawTree2(child, !alternate, duration);
            }
            // Traverse the right subtree
            // Not applicable since this is a tree, not a binary tree
        }
    }
}