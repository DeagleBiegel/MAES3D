using MAES3D.Algorithm.DualStageViewpointPlanner;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor.PackageManager;
using UnityEngine;

namespace MAES3D.Agent {
    public class NewCommunicationManager {

        private List<SubmarineAgent> _managedAgents;
        private float _algoCommInterval;
        private float _algoCommTick;

        private Dictionary<SubmarineAgent, Dictionary<SubmarineAgent, List<Cell>>> _newSinceLastSeen = new Dictionary<SubmarineAgent, Dictionary<SubmarineAgent, List<Cell>>>();
        private Dictionary<SubmarineAgent, List<SubmarineAgent>> _seenAgents = new Dictionary<SubmarineAgent, List<SubmarineAgent>>();


        public NewCommunicationManager(List<SubmarineAgent> managedAgents, float algoCommInterval = 5) {

            _algoCommInterval = algoCommInterval / Time.fixedDeltaTime;
            _algoCommTick = 0;

            _managedAgents = managedAgents;

            foreach (SubmarineAgent agent in _managedAgents) {
                Dictionary<SubmarineAgent, List<Cell>> _insideDict = new Dictionary<SubmarineAgent, List<Cell>>();
                foreach (SubmarineAgent insideAgent in _managedAgents) {
                    if (insideAgent == agent) continue;
                    _insideDict.Add(insideAgent, new List<Cell>());
                }
                _newSinceLastSeen.Add(agent, _insideDict);

                _seenAgents.Add(agent, new List<SubmarineAgent>());

            }
        }

        public void ShareMaps() {

            //General map sharing
            foreach (SubmarineAgent agent in _managedAgents) {

                //Update what cells have been seen for the first times last meet with every other agent
                List<Cell> seenCells = agent.Controller.ExplorationMap.newExploredCells;
                //Only if something new has been seen
                if (seenCells.Count != 0) {
                    foreach (SubmarineAgent otherAgent in _managedAgents) {
                        if (otherAgent == agent) continue;

                        _newSinceLastSeen[agent][otherAgent].AddRange(seenCells);
                    }
                    agent.Controller.ExplorationMap.newExploredCells.Clear();
                }

                //Share maps if it sees another agent
                List<SubmarineAgent> seenAgents = agent.Controller.ExplorationMap.GetVisibleAgents();
                foreach (SubmarineAgent seenAgent in seenAgents) {

                    if (!_seenAgents[agent].Contains(seenAgent)) {
                        _seenAgents[agent].Add(seenAgent);
                    }

                    //Skip if there is are no new cells
                    if (_newSinceLastSeen[agent][seenAgent].Count == 0) continue;

                    //Every cell the agent has seen since it last shared
                    foreach (Cell c in _newSinceLastSeen[agent][seenAgent]) {

                        //If the other agent has not seen this cell before
                        if (seenAgent.Controller.ExplorationMap.GetCellStatus(c) == CellStatus.unexplored) {
                            CellStatus observerCellStatus = agent.Controller.ExplorationMap.GetCellStatus(c);

                            //Update it in the other agents ExplorationMap
                            seenAgent.Controller.ExplorationMap.UpdateCell(c, observerCellStatus);

                            //Add it as a new one for every other agent the other agent can see in the future
                            foreach (SubmarineAgent otherAgent in _managedAgents) {
                                if (seenAgent == otherAgent) continue;
                                _newSinceLastSeen[seenAgent][otherAgent].Add(c);
                            }
                        }
                    }
                    _newSinceLastSeen[agent][seenAgent].Clear();
                }
            }

            //Custom algo sharing
            if (_algoCommInterval <= _algoCommTick) {
                foreach (SubmarineAgent agent in _managedAgents) {
                    foreach (SubmarineAgent seenAgent in _seenAgents[agent]) {
                        agent.Algorithm.Communicate(seenAgent);
                    }
                    _seenAgents[agent].Clear();
                }
                _algoCommTick = 0;
            }
            else {
                _algoCommTick++;
            }

        }


        private void EvaluateSharedMaps() {
            //Go though every agent
            foreach (SubmarineAgent agent in _managedAgents) {

                //Go though every agent that this agent can see
                List<SubmarineAgent> seenAgents = agent.Controller.ExplorationMap.GetVisibleAgents();
                foreach (SubmarineAgent seenAgent in seenAgents) {

                    if (seenAgent == agent) continue;

                    //Every cell the agent has seen since these two last merged
                    foreach (Cell c in _newSinceLastSeen[agent][seenAgent]) {

                        //If the other agent has not seen this cell before
                        if (seenAgent.Controller.ExplorationMap.GetCellStatus(c) == CellStatus.unexplored) {
                            CellStatus observerCellStatus = agent.Controller.ExplorationMap.GetCellStatus(c);

                            //Update it in the other agents ExplorationMap
                            seenAgent.Controller.ExplorationMap.UpdateCell(c, observerCellStatus);

                            //Add it as a new one for every other agent the other agent can see in the future
                            foreach (SubmarineAgent otherAgent in _managedAgents) {
                                if (seenAgent == otherAgent) continue;
                                _newSinceLastSeen[seenAgent][otherAgent].Add(c);
                            }
                        }
                    }
                }
            }
        }
    }
}
