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

    private void Start() {
        settingsList = GenerateAutomatedTests();
        ApplySettings();
        SetupSimulation();
        StatWriter.InitializeStatFile(currentSettingsIndex);
    }

    void FixedUpdate() {
        if (elapsedTime < SimulationSettings.duration) {
            _simulation?.ExecuteStep();

            elapsedTime += Time.fixedDeltaTime;
            saveTimer += Time.fixedDeltaTime;
            if(saveTimer > 10) {
                StatWriter.AddResults(currentSettingsIndex, elapsedTime, _simulation.ExplorationManager.ExploredRatio);
                saveTimer = 0;
            }
        }
        else {
            DestroySimulation();
            if (currentSettingsIndex < settingsList.Count) {
                ApplySettings();
                SetupSimulation();
                StatWriter.InitializeStatFile(currentSettingsIndex);
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
        currentSettingsIndex++;
    }

    public List<GeneratedSettings> GenerateAutomatedTests() {
        int[] algos = { 0, 1 };
        int[] sizes = { 50, 75, 100 };
        int[] agentCounts = { 2, 3, 5, 10 };
        string[] seeds = { Random.value.ToString(), Random.value.ToString(), Random.value.ToString() };
        int duration = 1 * 60;

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
