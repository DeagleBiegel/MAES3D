using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MAES3D.Agent;
using MAES3D;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Networking;

public class UIBehaviour : MonoBehaviour
{
    public Simulator Sim;
    public GameObject mainCamera;

    private float timeLeft = 0;
    private float saveResultTimer = 0;

    public UIDocument CaveUI;
    public UIDocument AgentUI;
    public UIDocument CameraUI;

    private List<SubmarineAgent> agents;
    private int agentIndex; // -1 = Cave, 0-9 = Agents

    private CameraController cameraController;
    
    //CaveUI Interactables
    private DropdownField dropdownMapGenerators;
    private Button btnImportMap;
    private DropdownField dropdownAlgorithm;
    private    SliderInt agentCount;
    private    SliderInt duration;
    private    SliderInt mapHeight;
    private    SliderInt mapWidth;
    private    SliderInt mapDepth;
    private    MinMaxSlider sphereRadius;
    private    Toggle toggleSeed;
    private    TextField txtSeed;
    private    ProgressBar progBar;
    private    Button btnStart;
    private    Button btnStop;
    
    //CameraUI Interactable
    private    Button btnPrevCam;
    private    Button btnCave;
    private    Button btnNextCam;
    private Toggle toggleUnexplored;
    private GameObject unexploredMap;


    private void OnEnable(){
        cameraController = mainCamera.GetComponent<CameraController>();
        UpdateButtons(CaveUI);

    }

    void FixedUpdate(){
        if (CaveUI.enabled)
        {
            CaveUI.rootVisualElement.Q<ProgressBar>("ProgressBar").value = MathF.Floor(SimulationSettings.progress);
            CaveUI.rootVisualElement.Q<ProgressBar>("ProgressBar").title = string.Format("{0:00}:{1:00}",MathF.Floor(timeLeft/60),MathF.Floor(timeLeft)%60) + $" - {SimulationSettings.progress}%";
        }
        else if (AgentUI.enabled)
        {
            UpdateAgentUI(agents[agentIndex]);
            AgentUI.rootVisualElement.Q<ProgressBar>("ProgressBar").value = MathF.Floor(SimulationSettings.progress);
            AgentUI.rootVisualElement.Q<ProgressBar>("ProgressBar").title = string.Format("{0:00}:{1:00}",MathF.Floor(timeLeft/60),MathF.Floor(timeLeft)%60) + $" - {SimulationSettings.progress}%";    
        }
        else
        {
            //Debug.LogWarning("No UI is enabled");
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

    public void SetAgentIndex(int index){
        agentIndex = index;
    }

    public void UpdateUI() 
    {
        UIDocument UI = agentIndex == -1 ? CaveUI : AgentUI;
        ChangeToUI(UI);
    }

    public void ChangeCam(){

        if (mainCamera != null)
        {
            //Debug.Log($"click: agentIndex = {agentIndex}");
            GameObject chunk = GameObject.Find("Chunk(Clone)");

            if (agentIndex == -1)
            {
                ChangeToUI(CaveUI);
                cameraController.SetTargetOffset(chunk.transform);
            }
            else if (agentIndex > agents.Count-1)
            {
                agentIndex = -1;
                ChangeToUI(CaveUI);
                cameraController.SetTargetOffset(chunk.transform);
            }
            else if (agentIndex < -1)
            {
                agentIndex = agents.Count-1;
                cameraController.SetTarget(agents[agentIndex].transform);
                ChangeToUI(AgentUI);
            }
            else
            {  
                cameraController.SetTarget(agents[agentIndex].transform);
                ChangeToUI(AgentUI);
            }
        }
    }

    private void ChangeToUI(UIDocument newUI){
        if (newUI == AgentUI)
        {
            CaveUI.enabled = false;
            AgentUI.enabled = true;
            UpdateButtons(AgentUI);
        }
        else if (newUI == CaveUI)
        {
            AgentUI.enabled = false;
            CaveUI.enabled = true;
            UpdateButtons(CaveUI);
        }
        else
        {
            //Debug.LogError("Not Valid UI Selected");
            Debug.Break();
        }
    }

    private void UpdateButtons(UIDocument UIDoc){
        if (UIDoc == CameraUI)
        {
            btnPrevCam = CameraUI.rootVisualElement.Q<Button>("ButtonPrev");
            btnCave = CameraUI.rootVisualElement.Q<Button>("ButtonCave");
            btnNextCam = CameraUI.rootVisualElement.Q<Button>("ButtonNext");
            toggleUnexplored = CameraUI.rootVisualElement.Q<Toggle>("ToggleUnexplored");           
            
            btnPrevCam.clickable.clicked += () => 
            {
                if (cameraController.IsTransitioning())
                    return;

                //Debug.Log("Prev Clicked");
                agentIndex--;
                ChangeCam();
            };

            btnCave.clickable.clicked += () => 
            {
                if (cameraController.IsTransitioning())
                    return;
                //Debug.Log("Cave Clicked");
                agentIndex = -1;
                ChangeCam();
            };

            btnNextCam.clickable.clicked += () => 
            {
                if (cameraController.IsTransitioning())
                    return;
                //Debug.Log("Next Clicked");
                agentIndex++;
                ChangeCam();
            };

            toggleUnexplored.RegisterValueChangedCallback(v =>{
                unexploredMap ??= GameObject.Find("Explored(Clone)");
                unexploredMap.SetActive(toggleUnexplored.value);
            });
        }
        else if (UIDoc == CaveUI)
        {
            //Debug.Log("CaveUI Updated");
            VisualElement CaveRoot = CaveUI.rootVisualElement;

            //Update References to Interactables
            dropdownMapGenerators = CaveRoot.Q<DropdownField>("DropdownMapGenerator");
            btnImportMap = CaveRoot.Q<Button>("ButtonImportMap");
            btnImportMap.SetEnabled(false);

            dropdownAlgorithm = CaveRoot.Q<DropdownField>("DropdownAlgorithm");
            agentCount = CaveRoot.Q<SliderInt>("AgentCount");
            duration = CaveRoot.Q<SliderInt>("SimDuration");
            mapHeight = CaveRoot.Q<SliderInt>("MapHeight");
            mapWidth = CaveRoot.Q<SliderInt>("MapWidth");
            mapDepth = CaveRoot.Q<SliderInt>("MapDepth");
            toggleSeed = CaveRoot.Q<Toggle>("ToggleRandomSeed");
            txtSeed = CaveRoot.Q<TextField>("TxtSeed");
            progBar = CaveRoot.Q<ProgressBar>("ProgressBar");
            btnStart = CaveRoot.Q<Button>("ButtonStart");
            btnStop = CaveRoot.Q<Button>("ButtonStop");

            GroupBox NewMapGen = CaveRoot.Q<GroupBox>("NewMapGenerator");
            GroupBox OldMapGen = CaveRoot.Q<GroupBox>("OldMapGenerator");
            OldMapGen.SetEnabled(false);

            sphereRadius = CaveRoot.Q<MinMaxSlider>("SphereRadius");

            // Fixing TextFieldBugs
            FixVisualBug();

            //Update Interactables when interacted with
            //Advanced Settings
            dropdownMapGenerators.RegisterValueChangedCallback(v =>{
                switch (dropdownMapGenerators.index)
                {
                    case 0:
                        btnImportMap.SetEnabled(false);
                        NewMapGen.SetEnabled(true);
                        OldMapGen.SetEnabled(false);
                        break;
                    case 1:
                        btnImportMap.SetEnabled(false);
                        NewMapGen.SetEnabled(false);
                        OldMapGen.SetEnabled(true);
                        break;
                    case 2:
                        btnImportMap.SetEnabled(true);
                        NewMapGen.SetEnabled(false);
                        OldMapGen.SetEnabled(false);
                        break;

                    default:
                        Debug.LogError("Dropdown Out of Range");
                        break;
                }
            });
            btnImportMap.clickable.clicked += () => {
                //OpenFileExplorer();

            };


            //General Settings
            agentCount.RegisterValueChangedCallback(v =>{
                agentCount.Q<TextField>("unity-text-field").label = agentCount.value.ToString();
            });
            duration.RegisterValueChangedCallback(v =>{
                duration.Q<TextField>("unity-text-field").label = duration.value.ToString();
            });

            //Map Generator General
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
            //Map Generator Random Cell Construction

            //Map Generator Sphere connection (new)
            sphereRadius.RegisterValueChangedCallback(v =>{
                CaveRoot.Q<Label>("MiniMax").text = $"{sphereRadius.minValue.ToString("n0")} - {sphereRadius.maxValue.ToString("n0")}";
                
                sphereRadius.minValue = (int)sphereRadius.minValue;
                sphereRadius.maxValue = (int)sphereRadius.maxValue;
            });

            //Simulation Controls
            btnStart.clickable.clicked += () => {
                //Debug.Log("Start Clicked");

                if ((Simulation)FindObjectOfType(typeof(Simulation)) != null)
                {
                    //Debug.Log("Tried to start a simulation while another simulation is running.");
                    return;
                }
                CameraUI.enabled = true; // Enables the camera controls UI
                UpdateButtons(CameraUI);


                // Adds values from VisualElements to SimulationSettings
                SimulationSettings.algorithm = dropdownAlgorithm.index;
                SimulationSettings.agentCount = agentCount.value;
                SimulationSettings.duration = duration.value*60; // in minutes
                SimulationSettings.useRandomSeed = toggleSeed.value;
                if (SimulationSettings.useRandomSeed)
                {
                    UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
                    SimulationSettings.seed = UnityEngine.Random.Range(100000, 1000000);
                }
                else {
                    int seed = Int32.Parse(txtSeed.value);
                    UnityEngine.Random.InitState(seed);
                    SimulationSettings.seed = seed;
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
                agents.Reverse();
                agentIndex = -1;
            };
            btnStop.clickable.clicked += () => {
                //Debug.Log("Stop Clicked");
                CameraUI.enabled = false; // Disables the camera controls UI

                timeLeft = 0;
                Sim.DestroySimulation();

            };
        }
        else if (UIDoc == AgentUI)
        {
            
        }
        else
        {
            //Debug.LogError("Not Valid UI Selected");
            Debug.Break();
        }
    }

    private void AddResults(int duration, int progress, int instance){
        TextWriter tw = new StreamWriter(Application.dataPath + $"/Results/{AlgorithmIndexToString(SimulationSettings.algorithm)}_x{SimulationSettings.Width}y{SimulationSettings.Height}z{SimulationSettings.Depth}_I{SimulationSettings.Instance}.csv", true);
        tw.WriteLine($"{duration},{progress}");
        tw.Close();
    }

    public void UpdateAgentUI(SubmarineAgent agent)
    {

        VisualElement AgentRoot = AgentUI.rootVisualElement;
        Label id = AgentRoot.Q<Label>("ID-label");
        Label task = AgentRoot.Q<Label>("Task-label");
        Label position = AgentRoot.Q<Label>("Position-label");
        Label speed = AgentRoot.Q<Label>("Speed-label");
        Label algoTF = AgentRoot.Q<Label>("Algorithm-info");
        Vector3 currPos = agents[agentIndex] == null ? Vector3.zero : agents[agentIndex].Controller.GetPosition();
  
        id.text = agents[agentIndex].Id.ToString();
        if (agents[agentIndex].Controller.GetCurrentTask() != null)
            task.text = agents[agentIndex].Controller.GetCurrentTask().ToString().Replace("MAES3D.Agent.Task.", ""); // Trim the start
        position.text = $"({currPos.x.ToString("n2")}, {currPos.y.ToString("n2")}, {currPos.z.ToString("n2")})";

        speed.text = (agent.Controller.GetSpeed() * 1/Time.fixedDeltaTime).ToString("n2");

        algoTF.text = agent.Algorithm.GetInformation();

    }

    private void FixVisualBug(){
        dropdownAlgorithm.index = SimulationSettings.algorithm;
        agentCount.value = SimulationSettings.agentCount;
        duration.value = ((int)SimulationSettings.duration / 60);
        mapHeight.value = SimulationSettings.Height;
        mapWidth.value = SimulationSettings.Width;
        mapDepth.value = SimulationSettings.Depth;
        toggleSeed.value = SimulationSettings.useRandomSeed;

        List<VisualElement> textFields = new UQueryBuilder<VisualElement>(CaveUI.rootVisualElement).Name("unity-text-field").ToList();
        foreach (TextField tf in textFields){
            tf.label = tf.value;
        }
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
/*
    private void OpenFileExplorer(){
        string path;
        path = EditorUtility.OpenFilePanel("Select a Voxel-Map", "", "vmap");
        StartCoroutine(GetMap(path));
    }
    IEnumerator GetMap(string paths){
        UnityWebRequest www = UnityWebRequest.Get("file:///" + paths);

        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogWarning(www.error);
        }
        else
        {
            var myMap = ((DownloadHandlerFile)www.downloadHandler);
            //use myMap here
        }
    }
*/
}
