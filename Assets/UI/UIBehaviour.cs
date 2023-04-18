using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MAES3D.Agent;
using MAES3D;
using UnityEngine.UIElements;


public class UIBehaviour : MonoBehaviour
{
    public Simulator Sim;
    public GameObject mainCamera;

    private float timeLeft = 0;
    private float saveResultTimer = 0;

    public UIDocument CaveUI;
    public UIDocument AgentUI;
    public UIDocument CameraControls;

    private List<SubmarineAgent> agents;
    private int agentIndex;


    private void OnEnable(){

        VisualElement CaveRoot = CaveUI.rootVisualElement;

        DropdownField dropdownAlgorithm = CaveRoot.Q<DropdownField>("DropdownAlgorithm");

        SliderInt agentCount = CaveRoot.Q<SliderInt>("AgentCount");
        SliderInt duration = CaveRoot.Q<SliderInt>("SimDuration");
        SliderInt mapHeight = CaveRoot.Q<SliderInt>("MapHeight");
        SliderInt mapWidth = CaveRoot.Q<SliderInt>("MapWidth");
        SliderInt mapDepth = CaveRoot.Q<SliderInt>("MapDepth");

        Toggle toggleSeed = CaveRoot.Q<Toggle>("ToggleRandomSeed");
        TextField txtSeed = CaveRoot.Q<TextField>("TxtSeed");

        ProgressBar progBar = CaveRoot.Q<ProgressBar>("ProgressBar");

        Button btnStart = CaveRoot.Q<Button>("ButtonStart");
        Button btnStop = CaveRoot.Q<Button>("ButtonStop");

        Button btnPrevCam = CameraControls.rootVisualElement.Q<Button>("ButtonPrev");
        Button btnCave = CameraControls.rootVisualElement.Q<Button>("ButtonCave");
        Button btnNextCam = CameraControls.rootVisualElement.Q<Button>("ButtonNext");


        // Fixing TextFieldBugs
        /**/
        List<VisualElement> textFields = new UQueryBuilder<VisualElement>(CaveRoot).Name("unity-text-field").ToList();
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
            if ((Simulation)FindObjectOfType(typeof(Simulation)) != null)
            {
                Debug.Log("Tried to start a simulation while another simulation is running.");
                return;
            }
            CameraControls.enabled = true; // Enables the camera controls UI

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

            mainCamera.transform.position = new Vector3(-(mapWidth.value * 0.25f), mapHeight.value * 1.25f, -(mapDepth.value * 0.25f));
            mainCamera.transform.LookAt(new Vector3(mapWidth.value * 0.5f, mapHeight.value * 0.5f, mapDepth.value * 0.5f));
            mainCamera.transform.Translate(Vector3.right * (Mathf.Sqrt(mapWidth.value ^ 2 * mapDepth.value ^ 2) * 0.2f), Space.Self);

            agents = new List<SubmarineAgent>(FindObjectsOfType<SubmarineAgent>());
            agentIndex = 0;

        };
        btnStop.clickable.clicked += () => {
            CameraControls.enabled = false; // Disables the camera controls UI

            timeLeft = 0;
            Sim.DestroySimulation();

        };

        btnPrevCam.clickable.clicked += () => 
        {
            Debug.Log("Prev Clicked");

            ChangeCam(-1);
        };

        btnCave.clickable.clicked += () => 
        {
            Debug.Log("Cave Clicked");

            ChangeCam(0);
        };

        btnNextCam.clickable.clicked += () => 
        {
            Debug.Log("Next Clicked");

            ChangeCam(1);
        };
    }

    void FixedUpdate(){
        if (CaveUI.enabled)
        {
            CaveUI.rootVisualElement.Q<ProgressBar>("ProgressBar").value = MathF.Floor(SimulationSettings.progress);
            CaveUI.rootVisualElement.Q<ProgressBar>("ProgressBar").title = string.Format("{0:00}:{1:00}",MathF.Floor(timeLeft/60),MathF.Floor(timeLeft)%60) + $" - {SimulationSettings.progress}%";
        }
        else if (AgentUI.enabled)
        {
            AgentUI.rootVisualElement.Q<ProgressBar>("ProgressBar").value = MathF.Floor(SimulationSettings.progress);
            AgentUI.rootVisualElement.Q<ProgressBar>("ProgressBar").title = string.Format("{0:00}:{1:00}",MathF.Floor(timeLeft/60),MathF.Floor(timeLeft)%60) + $" - {SimulationSettings.progress}%";    
        }
        else
        {
            Debug.LogWarning("No UI is enabled");
        }

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


    private void ChangeCam(int movement){

        if (mainCamera != null)
        {
            //Debug.Log($"click: agentIndex = {agentIndex}");
            CameraController cameraController = mainCamera.GetComponent<CameraController>();
            GameObject chunk = GameObject.Find("Chunk(Clone)");


            switch (movement)
            {
                case 0:
                    ChangeToUI(CaveUI);
                    cameraController.SetTargetOffset(chunk.transform, new Vector3(SimulationSettings.Width / 2, SimulationSettings.Height / 2, SimulationSettings.Depth / 2));
                    agentIndex = 0;
                    return;

                case 1:
                    agentIndex++;
                    if (agentIndex > agents.Count)
                    {
                        agentIndex = 0;
                        cameraController.SetTargetOffset(chunk.transform, new Vector3(SimulationSettings.Width / 2, SimulationSettings.Height / 2, SimulationSettings.Depth / 2));
                        ChangeToUI(CaveUI);
                    }
                    else
                    {
                        cameraController.SetTarget(agents[agentIndex-1].transform);
                        ChangeToUI(AgentUI);
                    }
                    return;

                case -1:
                    agentIndex--;
                    if (agentIndex == 0)
                    {
                        cameraController.SetTargetOffset(chunk.transform, new Vector3(SimulationSettings.Width / 2, SimulationSettings.Height / 2, SimulationSettings.Depth / 2));
                        ChangeToUI(CaveUI);
                    }
                    else if (agentIndex > 0)
                    {
                        cameraController.SetTarget(agents[agentIndex-1].transform);
                        ChangeToUI(AgentUI);
                    }
                    else
                    {
                        agentIndex = agents.Count;
                        cameraController.SetTarget(agents[agentIndex-1].transform);
                        ChangeToUI(AgentUI);
                    }
                    return;
                default:
                    Debug.LogError("Invalid CameraMovement");
                    Debug.Break();
                    return;
            }
        }
    }

    private void ChangeToUI(UIDocument newUI){
        if (newUI == AgentUI)
        {
            CaveUI.enabled = false;
            AgentUI.enabled = true;
        }
        else if (newUI == CaveUI)
        {
            AgentUI.enabled = false;
            CaveUI.enabled = true;
        }
        else
        {
            Debug.LogError("Not Valid UI Selected");
            Debug.Break();
        }
    }

    private void AddResults(int duration, int progress, int instance){
        TextWriter tw = new StreamWriter(Application.dataPath + $"/Results/{AlgorithmIndexToString(SimulationSettings.algorithm)}_x{SimulationSettings.Width}y{SimulationSettings.Height}z{SimulationSettings.Depth}_I{SimulationSettings.Instance}.csv", true);
        tw.WriteLine($"{duration},{progress}");
        tw.Close();
    }

    private void UpdateAgentUI(int agentID, String task, Vector3 position, Vector3 target, string algoInfo = null){
        

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
