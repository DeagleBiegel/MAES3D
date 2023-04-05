using MAES3D;
using MAES3D.Agent;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class Simulator : MonoBehaviour{
    private Simulation _simulation;
    public GameObject SimulationPrefab;

    void FixedUpdate() {
        _simulation?.ExecuteStep();
    }

    public void SetupSimulation(float duration) {
        var _simulationGameObject = Instantiate(SimulationPrefab, transform);
        _simulation = _simulationGameObject.GetComponent<Simulation>();
        Destroy(_simulationGameObject, duration); 
        _simulation.SetupScenario();
        Time.timeScale = 1f;
    }

    public void DestroySimulation() {
        Destroy(GameObject.Find("Simulation(Clone)"));
    }
}
