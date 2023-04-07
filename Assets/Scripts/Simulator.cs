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

    private JsonWriter _jsonWriter;

    private void Start() {
        settingsList = GenerateAutomatedTests();
        ApplySettings();
        SetupSimulation();
        // StatWriter.InitializeStatFile(currentSettingsIndex);

        _jsonWriter = new JsonWriter(settingsList[currentSettingsIndex], _simulation.map.GetNumberOfExplorableTiles());
        _jsonWriter.AddData(0, 0);

        currentSettingsIndex++;
    }

    void FixedUpdate() {

        if (finished) return;

        if (elapsedTime < SimulationSettings.duration) {
            _simulation?.ExecuteStep();

            elapsedTime += Time.fixedDeltaTime;
            saveTimer += Time.fixedDeltaTime;

            if(saveTimer >= 10) {
                // StatWriter.AddResults(currentSettingsIndex, elapsedTime, _simulation.ExplorationManager.ExploredRatio);
                _jsonWriter.AddData((int) elapsedTime, _simulation.ExplorationManager.ExploredRatio);
                saveTimer %= 10;
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
                    _jsonWriter.AddData(0, 0);
                }
                else 
                {
                    _jsonWriter.EndFile();
                    _jsonWriter = new JsonWriter(settingsList[currentSettingsIndex], _simulation.map.GetNumberOfExplorableTiles());
                }

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
        int[] algos = { 0 };
        int[] sizes = { 50 };
        int[] agentCounts = { 2, 3 };
        string[] seeds = { Random.value.ToString(), Random.value.ToString() };
        int duration = 1 * 20;

        float timeScale = 4f;

        List<GeneratedSettings> retList = new List<GeneratedSettings>();

        foreach (int algo in algos) { 
            foreach (int size in sizes) {
                foreach(int agentCount in agentCounts) {
                    foreach (string seed in seeds) {
                        retList.Add(new GeneratedSettings() {
                            Height = size,
                            Width = size,
                            Depth = size,
                            smoothingIterations = 20,
                            useRandomSeed = false,
                            seed = seed.GetHashCode(),
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
    }

    public void DestroySimulation() {
        Destroy(GameObject.Find("Simulation(Clone)"));
    }
}
