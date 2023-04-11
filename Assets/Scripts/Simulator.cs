using MAES3D;
using MAES3D.Agent;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class Simulator : MonoBehaviour{
    private Simulation _simulation;
    public GameObject SimulationPrefab;
    float elapsedTime = 0;
    float saveTimer = 0;
    List<GeneratedSettings> settingsList;
    int currentSettingsIndex = 0;
    bool finished = false;

    private JsonWriter2 _jsonWriter;

    private void Start() {
        settingsList = GenerateAutomatedTests();
        ApplySettings();
        SetupSimulation();
        // StatWriter.InitializeStatFile(currentSettingsIndex);

        _jsonWriter = new JsonWriter2(settingsList[currentSettingsIndex], _simulation.map.GetNumberOfExplorableTiles(), settingsList[currentSettingsIndex].seed);
        _jsonWriter.AddData(0, 0, false);

        currentSettingsIndex++;
    }

    void FixedUpdate() {

        if (finished) return;

        bool finishedExploring = false;
        if (_simulation.ExplorationManager.ExploredRatio >= 99.7f || elapsedTime > SimulationSettings.duration) {
            finishedExploring = true;
            _jsonWriter.AddData((int)System.Math.Round(elapsedTime), _simulation.ExplorationManager.ExploredRatio, true);
        }

        if (!finishedExploring) {
            _simulation?.ExecuteStep();

            elapsedTime += Time.fixedDeltaTime;
            saveTimer += Time.fixedDeltaTime;

            if(saveTimer >= 5 || finishedExploring) {
                // StatWriter.AddResults(currentSettingsIndex, elapsedTime, _simulation.ExplorationManager.ExploredRatio);
                _jsonWriter.AddData((int)System.Math.Round(elapsedTime), _simulation.ExplorationManager.ExploredRatio, false);
                saveTimer %= 5;
            }

            if(finishedExploring) {
                elapsedTime = SimulationSettings.duration;
            }
        }
        else {
            DestroySimulation();
            if (currentSettingsIndex < settingsList.Count) 
            {
                ApplySettings();
                SetupSimulation();
            
                if (settingsList[currentSettingsIndex - 1].algorithm == SimulationSettings.algorithm &&
                    settingsList[currentSettingsIndex - 1].Height == SimulationSettings.Height &&
                    settingsList[currentSettingsIndex - 1].agentCount == SimulationSettings.agentCount) 
                {
                    _jsonWriter.InitTest(_simulation.map.GetNumberOfExplorableTiles(), settingsList[currentSettingsIndex].seed);
                    _jsonWriter.AddData(0, 0, false);
                }
                else 
                {
                    _jsonWriter.EndFile();
                    _jsonWriter = new JsonWriter2(settingsList[currentSettingsIndex], _simulation.map.GetNumberOfExplorableTiles(), settingsList[currentSettingsIndex].seed);
                }

                elapsedTime = 0;
                saveTimer = 0;
                currentSettingsIndex++;
            }
            else 
            {
                _jsonWriter.EndFile();
                finished = true;
            }
        }
    }

    private void ApplySettings() {
        GeneratedSettings current = settingsList[currentSettingsIndex];

        SimulationSettings.Height = current.Height;
        SimulationSettings.Width = current.Width;
        SimulationSettings.Depth = current.Depth;
        SimulationSettings.smoothingIterations = current.smoothingIterations;
        SimulationSettings.useRandomSeed = current.useRandomSeed;
        SimulationSettings.seed = current.seed;
        SimulationSettings.agentCount = current.agentCount;
        SimulationSettings.algorithm = current.algorithm;
        SimulationSettings.duration = current.duration;
        SimulationSettings.timeScale = current.timeScale;
    }

    public List<GeneratedSettings> GenerateAutomatedTests() {
        int[] algos = { 2 };
        int[] sizes = { 50, 75 };
        int[] agentCounts = { 5, 10 };

        int[] setSeeds = { 688446, 881082, 715672, 360565, 402211, 
                           781547, 234175, 510916, 902487, 103226, 
                           718267, 140175, 423719, 622131, 278169, 
                           517123, 595283, 415280, 204763, 664104 };

        List<int> seeds = new List<int>(setSeeds);
        //int seedAmount = 20;
        //for (int i = 0; i < seedAmount; i++) {
        //    seeds.Add(Random.Range(100000, 1000000));
        //}

        int duration = 30 * 60;

        float timeScale = 2f;

        List<GeneratedSettings> retList = new List<GeneratedSettings>();

        foreach (int algo in algos) { 
            foreach (int size in sizes) {
                foreach(int agentCount in agentCounts) {
                    foreach (int seed in seeds) {
                        retList.Add(new GeneratedSettings() {
                            Height = size,
                            Width = size,
                            Depth = size,
                            smoothingIterations = 20,
                            useRandomSeed = false,
                            seed = seed,
                            agentCount = agentCount,
                            algorithm = algo,
                            duration = duration,
                            timeScale = timeScale
                        });
                    }
                }
            }
        }

        return retList;

    }

    public void SetupSimulation() {

        elapsedTime = 0;
        saveTimer = 0;

        var _simulationGameObject = Instantiate(SimulationPrefab, transform);
        _simulation = _simulationGameObject.GetComponent<Simulation>();
        //Destroy(_simulationGameObject, duration); 
        _simulation.SetupScenario();
        Time.timeScale = SimulationSettings.timeScale;
    }

    public void DestroySimulation() {
        Destroy(GameObject.Find("Simulation(Clone)"));
    }
}
