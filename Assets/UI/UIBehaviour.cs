using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public class UIBehaviour : MonoBehaviour
{
    public Simulator Sim;
    public GameObject[] cameras;
    private int cameraIndex = 0;

    private float timeLeft = 0;
    private float saveResultTimer = 0;

    private void OnEnable(){

        Sim = (Simulator)FindObjectOfType(typeof(Simulator));    


        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        DropdownField dropdownAlgorithm = root.Q<DropdownField>("DropdownAlgorithm");

        SliderInt agentCount = root.Q<SliderInt>("AgentCount");
        SliderInt duration = root.Q<SliderInt>("SimDuration");
        SliderInt mapHeight = root.Q<SliderInt>("MapHeight");
        SliderInt mapWidth = root.Q<SliderInt>("MapWidth");
        SliderInt mapDepth = root.Q<SliderInt>("MapDepth");

        Toggle toggleSeed = root.Q<Toggle>("ToggleRandomSeed");
        TextField txtSeed = root.Q<TextField>("TxtSeed");

        ProgressBar progBar = root.Q<ProgressBar>("ProgressBar");

        Button btnStart = root.Q<Button>("ButtonStart");
        Button btnStop = root.Q<Button>("ButtonStop");
        Button btnFastForward = root.Q<Button>("ButtonFastForward");






        // Fixing TextFieldBugs
        List<VisualElement> textFields = new UQueryBuilder<VisualElement>(root).Name("unity-text-field").ToList();
        foreach (TextField tf in textFields){
            tf.label = tf.value;
        }

        agentCount.RegisterValueChangedCallback(v =>{
            agentCount.Q<TextField>("unity-text-field").label = agentCount.value.ToString();
        });
        duration.RegisterValueChangedCallback(v =>{
            duration.Q<TextField>("unity-text-field").label = duration.value.ToString();
        });
        mapHeight.RegisterValueChangedCallback(v =>{
            mapHeight.Q<TextField>("unity-text-field").label = mapHeight.value.ToString();
        });
        mapWidth.RegisterValueChangedCallback(v =>{
            mapWidth.Q<TextField>("unity-text-field").label = mapWidth.value.ToString();
        });
        mapDepth.RegisterValueChangedCallback(v =>{
            mapDepth.Q<TextField>("unity-text-field").label = mapDepth.value.ToString();
        });

        toggleSeed.RegisterValueChangedCallback(v =>{
            if (txtSeed.focusable = !toggleSeed.value)
                txtSeed.value = "Insert Seed";
            else
                txtSeed.value = "Random Seed";
        });

        btnStart.clickable.clicked += () => {
            // Adds values from VisualElements to SimulationSettings
            SimulationSettings.algorithm = dropdownAlgorithm.index;
            SimulationSettings.agentCount = agentCount.value;
            SimulationSettings.duration = duration.value*60; // in minutes
            SimulationSettings.useRandomSeed = toggleSeed.value;
            if (!SimulationSettings.useRandomSeed)
            {
                SimulationSettings.seed = Int32.Parse(txtSeed.value);
            }
            SimulationSettings.Height = mapHeight.value;
            SimulationSettings.Width = mapWidth.value;
            SimulationSettings.Depth = mapDepth.value;
            
            // Starts the simulation
            Sim.SetupSimulation(SimulationSettings.duration);
            timeLeft = SimulationSettings.duration;

            // Start Taking Results
            TextWriter textWriter = new StreamWriter(Application.dataPath + $"/Results/{AlgorithmIndexToString(SimulationSettings.algorithm)}_x{SimulationSettings.Width}y{SimulationSettings.Height}z{SimulationSettings.Depth}_I{SimulationSettings.Instance}.csv", false);
            textWriter.WriteLine($"Algorithm,{AlgorithmIndexToString(SimulationSettings.algorithm)}");
            textWriter.WriteLine($"MapSize,x={SimulationSettings.Width},y={SimulationSettings.Height},z={SimulationSettings.Depth}");
            textWriter.WriteLine($"AgentAmount,{SimulationSettings.agentCount}");
            textWriter.WriteLine($"Duration(min),{SimulationSettings.duration/60}");
            textWriter.WriteLine($"Seed,{SimulationSettings.seed}");
            textWriter.WriteLine("Timestamp(s),Progress(%),");
            if (SimulationSettings.Instance == 0)
                textWriter.WriteLine("0, 0");
            textWriter.Close();

            GameObject mainCamera = cameras[0];
            mainCamera.transform.position = new Vector3(-(mapWidth.value * 0.25f), mapHeight.value * 1.25f, -(mapDepth.value * 0.25f));
            mainCamera.transform.LookAt(new Vector3(mapWidth.value * 0.5f, mapHeight.value * 0.5f, mapDepth.value * 0.5f));
            mainCamera.transform.Translate(Vector3.right * (Mathf.Sqrt(mapWidth.value ^ 2 * mapDepth.value ^ 2) * 0.2f), Space.Self);

        };
        btnStop.clickable.clicked += () => {
            timeLeft = 0;
            Sim.DestroySimulation();

        };
        btnFastForward.clickable.clicked += () => {
            if(cameraIndex < cameras.Length - 1){
                cameras[cameraIndex].SetActive(false);
                cameraIndex++;
                cameras[cameraIndex].SetActive(true);
            }
            else{
                cameras[cameraIndex].SetActive(false);
                cameraIndex = 0;
                cameras[cameraIndex].SetActive(true);
            }

        };        
    }

    void FixedUpdate(){
        GetComponent<UIDocument>().rootVisualElement.Q<ProgressBar>("ProgressBar").value = MathF.Floor(SimulationSettings.progress);
        GetComponent<UIDocument>().rootVisualElement.Q<ProgressBar>("ProgressBar").title = string.Format("{0:00}:{1:00}",MathF.Floor(timeLeft/60),MathF.Floor(timeLeft)%60) + $" - {SimulationSettings.progress}%";
        if(timeLeft > 0){
            timeLeft -= Time.fixedDeltaTime;
            if(saveResultTimer < 10)
                saveResultTimer += Time.fixedDeltaTime;
            else{
                saveResultTimer = 0;
                AddResults((int)(SimulationSettings.duration - timeLeft), (int)SimulationSettings.progress, SimulationSettings.Instance);
            }
        }
        else if (timeLeft == 0)
            timeLeft = 0;
        else{
            timeLeft = 0;
            AddResults((int)(SimulationSettings.duration - timeLeft), (int)SimulationSettings.progress, SimulationSettings.Instance-1);
        }




    }

    private void AddResults(int duration, int progress, int instance){
        TextWriter tw = new StreamWriter(Application.dataPath + $"/Results/{AlgorithmIndexToString(SimulationSettings.algorithm)}_x{SimulationSettings.Width}y{SimulationSettings.Height}z{SimulationSettings.Depth}_I{SimulationSettings.Instance}.csv", true);
        tw.WriteLine($"{duration},{progress}");
        tw.Close();
    }

    private String AlgorithmIndexToString(int algorithmIndex){
        switch (algorithmIndex)
        {
            case 0:
                return "RBW";
            case 1:
                return "LVD";
            case 2: 
                return "DSVP";
            default:
                return "No AlgorithmIndex Given";
        }

    }

}
