using MAES3D.Agent;
using MAES3D.Algorithm;
using MAES3D.Algorithm.RandomBalisticWalk;
using MAES3D.Algorithm.LocalVoronoiDecomposition;
using MAES3D.Algorithm.DualStageViewpointPlanner;
using System.Collections.Generic;
using UnityEngine;

namespace MAES3D {
    public class Simulation : MonoBehaviour {

        public GameObject AgentPrefab;
        public GameObject MapPrefab;

        public ExplorationManager ExplorationManager;
        public CommunicationManager CommunicationManager;

        private List<SubmarineAgent> _agents;
        
        private bool disabled = false;

        public void ExecuteStep() {
            if (!disabled){
                //Perform every LCCM step for each agent in a synchroized manner

                //Look
                ExplorationManager.UpdateMaps(_agents);

                //Compute
                foreach (SubmarineAgent agent in _agents) {
                    agent.LogicUpdate();
                }

                //Communicate
                CommunicationManager.ShareMaps(_agents);

                //Move
                foreach (SubmarineAgent agent in _agents) {
                    agent.MovementUpdate();
                }

            }

        }

        public void SetupScenario() {
            GameObject gameObject = Instantiate(MapPrefab, parent: transform);
            Chunk map = (Chunk) gameObject.GetComponent(typeof(Chunk));

            GameObject cameraObject = GameObject.FindWithTag("MainCamera");
            CameraController cameraController;

            if (cameraObject != null)
            {
                cameraController = cameraObject.GetComponent<CameraController>();
                cameraController.SetTargetOffset(map.transform);
            }

            _agents = new List<SubmarineAgent>();
            switch(SimulationSettings.algorithm) 
            {
                case 0:
                    for (int i = 0; i < SimulationSettings.agentCount; i++) {
                        _agents.Add(SpawnAgent(new RandomBalisticWalk(), map.SpawnPositions[i].middle, i));
                    }
                    break;
                case 1:
                    for (int i = 0; i < SimulationSettings.agentCount; i++) {
                        _agents.Add(SpawnAgent(new LocalVoronoiDecomposition(), map.SpawnPositions[i].middle, i));
                    }
                    break;
                case 2:
                    for (int i = 0; i < SimulationSettings.agentCount; i++) {
                        _agents.Add(SpawnAgent(new DualStageViewpointPlanner(), map.SpawnPositions[i].middle, i));
                    }
                    break;
                default:
                    Debug.Log("Selected Algorithm does not exist");
                    break;
            }
            ExplorationManager = new ExplorationManager();
            CommunicationManager = new CommunicationManager(_agents, 5);
        }

        private SubmarineAgent SpawnAgent(IAlgorithm algorithm, Vector3 position, int id) {
            GameObject agentGameObject = Instantiate(AgentPrefab, parent: transform);
            SubmarineAgent agent = agentGameObject.GetComponent<SubmarineAgent>();

            agent.Id = id;
            agent.Algorithm = algorithm;
            agent.Algorithm.SetController(agent.Controller);

            agent.transform.position = position;

            return agent;
        }
        
        void OnDisable(){
            disabled = true;
            // Gets ready for next simulation
            SimulationSettings.Instance++;
        }
    }
}
