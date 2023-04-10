using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.PackageManager;
using UnityEngine;
using MAES3D.Algorithm.DualStageViewpointPlanner;

namespace MAES3D.Agent {
    public class CommunicationManager {

        private float _mapEvaluationInterval;
        private float _mapEvaluationTick;
        private bool[][] _agentSeenMap;
        private List<SubmarineAgent> _managedAgents;
        private Dictionary<SubmarineAgent, int> _agentToIndexMap = new Dictionary<SubmarineAgent, int>();

        public CommunicationManager(List<SubmarineAgent> managedAgents, float mapEvaluationInterval = 5) {
            //Setup values for merge interval
            _mapEvaluationInterval = mapEvaluationInterval / Time.fixedDeltaTime;
            _mapEvaluationTick = 0;

            _managedAgents = managedAgents;

            //Setup dictinary for converting agents to index
            for (int i = 0; i < _managedAgents.Count; i++) {
                _agentToIndexMap.Add(_managedAgents[i], i);
            }

            //Populate AgentSeenMap with empty empty arrays
            _agentSeenMap = new bool[_managedAgents.Count][];
            for (int i = 0; i < _agentSeenMap.Length; i++) {
                _agentSeenMap[i] = new bool[_managedAgents.Count];
            }

        }

        public void ShareMaps(List<SubmarineAgent> agents) {
            //Update which agents has seen who
            UpdateSeenAgents();


            //Merge maps
            if (_mapEvaluationInterval <= _mapEvaluationTick) {
                EvaluateSharedMaps(agents);
                _mapEvaluationTick = 0;
            }
            else {
                _mapEvaluationTick++;
            }
        }

        private void EvaluateSharedMaps(List<SubmarineAgent> agents) {

            //Make temporary copy of each agents explored map
            List<CellStatus[,,]> mapCopies = new List<CellStatus[,,]>();
            for (int agentIndex = 0; agentIndex < agents.Count; agentIndex++) {
                CellStatus[,,] mapCopy = agents[agentIndex].Controller.ExplorationMap.GetMap().Clone() as CellStatus[,,];
                mapCopies.Add(mapCopy);
            }

            bool[][] shallMerge = _agentSeenMap.Clone() as bool[][];

            //Perform map merging
            for (int agentIndex = 0; agentIndex < _agentSeenMap.Length; agentIndex++) {
                for (int seenAgentIndex = 0; seenAgentIndex < _agentSeenMap[agentIndex].Length; seenAgentIndex++) {
                    
                    //Check if the agents maps should be combined
                    if (shallMerge[agentIndex][seenAgentIndex] == true) {

                        CellStatus[,,] agentMap = agents[agentIndex].Controller.ExplorationMap.GetMap();
                        CellStatus[,,] seenAgentMap = agents[seenAgentIndex].Controller.ExplorationMap.GetMap();

                        //Iterate through map
                        for (int x = 0; x < SimulationSettings.Width; x++) {
                            for (int y = 0; y < SimulationSettings.Height; y++) {
                                for (int z = 0; z < SimulationSettings.Depth; z++) {

                                    CellStatus agentCellStatus = mapCopies[agentIndex][x, y, z];
                                    CellStatus seenAgentCellStatus = mapCopies[seenAgentIndex][x, y, z];

                                    switch (agentCellStatus) {
                                        case CellStatus.unexplored:
                                            if (seenAgentCellStatus != CellStatus.unexplored) {
                                                agentMap[x, y, z] = seenAgentCellStatus;
                                            }
                                            break;
                                        case CellStatus.covered:
                                        case CellStatus.explored:
                                        case CellStatus.wall:
                                            if (seenAgentCellStatus != CellStatus.covered) {
                                                seenAgentMap[x, y, z] = agentCellStatus;
                                            }
                                            break;
                                    }

                                    switch (seenAgentCellStatus) {
                                        case CellStatus.unexplored:
                                            if (agentCellStatus != CellStatus.unexplored) {
                                                seenAgentMap[x, y, z] = agentCellStatus;
                                            }
                                            break;
                                        case CellStatus.explored:
                                        case CellStatus.covered:
                                        case CellStatus.wall:
                                            if (agentCellStatus != CellStatus.covered) {
                                                agentMap[x, y, z] = seenAgentCellStatus;
                                            }
                                            break;
                                    }
                                }
                            }
                        }

                        /* This agent */
                        DualStageViewpointPlanner agent = agents[agentIndex].Algorithm as DualStageViewpointPlanner;

                        /* Seen agent */
                        DualStageViewpointPlanner seenAgent = agents[seenAgentIndex].Algorithm as DualStageViewpointPlanner;

                        agent.GetFrontiersFromAgent(seenAgent.globalFrontiers);
                        seenAgent.GetFrontiersFromAgent(agent.globalFrontiers);
                        
                        //Avoid merging again when the seen agent is the current agent
                        shallMerge[agentIndex][seenAgentIndex] = false;
                        shallMerge[seenAgentIndex][agentIndex] = false;
                    }
                }
            }

            //As every map has been shared, clear temporary values
            mapCopies.Clear();
            _agentSeenMap = shallMerge.Clone() as bool[][];
        }

        private void UpdateSeenAgents() {
            for (int agentIndex = 0; agentIndex < _managedAgents.Count; agentIndex++) {
                SubmarineAgent agent = _managedAgents[agentIndex];
                List<SubmarineAgent> visibleAgents = agent.Controller.ExplorationMap.GetVisibleAgents();

                if (visibleAgents.Count != 0) {
                    foreach (SubmarineAgent visibleAgent in visibleAgents) {
                        _agentSeenMap[agentIndex][AgentToIndex(visibleAgent)] = true;
                    }
                }
            }
        }

        private List<SubmarineAgent> GetSeenAgents(SubmarineAgent agent) {
            List<SubmarineAgent> seenAgents = new List<SubmarineAgent>();

            bool[] seenAgentsArray = _agentSeenMap[AgentToIndex(agent)];
            for (int i = 0; i < seenAgentsArray.Length; i++) {
                bool hasSeen = seenAgentsArray[i];
                if (hasSeen == true) {
                    seenAgents.Add(agent);
                }
            }

            return seenAgents;
        }

        private int AgentToIndex(SubmarineAgent agent) {
            return _agentToIndexMap[agent];
        }

        private SubmarineAgent IndexToAgent(int index) {
            return _managedAgents[index];
        }

        public int testGetCoveredCount(CellStatus[,,] map) {
            int test = 0;

            for (int x = 0; x < SimulationSettings.Width; x++) {
                for (int y = 0; y < SimulationSettings.Height; y++) {
                    for (int z = 0; z < SimulationSettings.Depth; z++) {
                        if (map[x, y, z] == CellStatus.covered) {
                            test++;
                        }
                    }
                }
            }

            return test;
        }

    }
}
