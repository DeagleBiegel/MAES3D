using MAES3D.Agent;
using MAES3D.Algorithm;
using MAES3D.Algorithm.RandomBalisticWalk;
using MAES3D.Algorithm.LocalVoronoiDecomposition;
using MAES3D.Algorithm.DualStageViewpointPlanner;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager;

namespace MAES3D {
    public class Simulation : MonoBehaviour {

        public GameObject AgentPrefab;
        public GameObject MapPrefab;
        public GameObject ExplorationPrefab;

        public ExplorationManager ExplorationManager;
        public NewCommunicationManager CommunicationManager;

        private List<SubmarineAgent> _agents;
        
        private bool disabled = false;

        //Perform every LCCM step for each agent in a synchroized manner
        public void ExecuteStep() {
            // Paused
            if (disabled) return;

            //Look
            ExplorationManager.UpdateMaps(_agents);

            //Compute
            foreach (SubmarineAgent agent in _agents)
                agent.LogicUpdate();

            //Communicate
            CommunicationManager.ShareMaps();

            //Move
            foreach (SubmarineAgent agent in _agents)
                agent.MovementUpdate();
        }

        public void SetupScenario() {
            GameObject gameObject = Instantiate(MapPrefab, parent: transform);
            //Chunk mapc = (Chunk) gameObject.GetComponent(typeof(Chunk));

            MapGenerator mapGen;
            switch (SimulationSettings.mapGen)
            {
                case 0:
                    mapGen = new RandomConnectedSpheres();
                    break;
                case 1:
                    mapGen = new SmoothedNoise();
                    break;
                case 2:
                    mapGen = new MapImporter();
                    break;
                default:
                    Debug.LogError("Something went wrong in map generator selection");
                    Debug.Break();
                    return;
            }
            Map map = (Map)gameObject.GetComponent(typeof(Map));
            map.InitMap(mapGen.GenerateMap());

            if (!(mapGen is MapImporter)) {
                map.ExportMap();
            }

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
            CommunicationManager = new NewCommunicationManager(_agents);

            Instantiate(ExplorationPrefab, parent: transform);
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
