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
        //General
    private    SliderInt agentCount;
    private    SliderInt duration;
    private DropdownField dropdownAlgorithm;
    private DropdownField dropdownMapGenerators;
    private    ProgressBar progBar;
    private    Toggle toggleSeed;
    private    TextField txtSeed;
    private    Button btnStart;
    private    Button btnStop;

        //SmoothedNoise()
    private SliderInt SN_initialFillRatio;
    private SliderInt SN_smoothingIterations;
    private    SliderInt SN_mapHeight;
    private    SliderInt SN_mapWidth;
    private    SliderInt SN_mapDepth;


        //RandomConnectedSpheres()
    private SliderInt RCS_fillRatio;
    private    MinMaxSlider RCS_sphereRadius;
    private SliderInt RCS_sphereConnections;
    private SliderInt RCS_smoothingIterations;
    private    SliderInt RCS_mapHeight;
    private    SliderInt RCS_mapWidth;
    private    SliderInt RCS_mapDepth;


        //ImportMap()
    private Button btnImportMap;
    
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
            GameObject map = GameObject.Find("Map(Clone)");

            if (agentIndex == -1)
            {
                ChangeToUI(CaveUI);
                cameraController.SetTargetOffset(map.transform);
            }
            else if (agentIndex > agents.Count-1)
            {
                agentIndex = -1;
                ChangeToUI(CaveUI);
                cameraController.SetTargetOffset(map.transform);
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
            QueryButtons(CaveRoot);

            GroupBox newMapGen = CaveRoot.Q<GroupBox>("NewMapGenerator");
            GroupBox oldMapGen = CaveRoot.Q<GroupBox>("OldMapGenerator");
            GroupBox seedGB = CaveRoot.Q<GroupBox>("SeedGroupBox");
            oldMapGen.SetEnabled(false);

            // Fixing TextFieldBugs
            FixVisualBug();

            //Update Interactables when interacted with
            //MapGenLayout
            dropdownMapGenerators.RegisterValueChangedCallback(v =>{
                SimulationSettings.mapGen = dropdownMapGenerators.index;
                switch (dropdownMapGenerators.index)
                {
                    case 0:
                        btnImportMap.SetEnabled(false);
                        newMapGen.SetEnabled(true);
                        oldMapGen.SetEnabled(false);
                        seedGB.SetEnabled(true);
                        break;
                    case 1:
                        btnImportMap.SetEnabled(false);
                        newMapGen.SetEnabled(false);
                        oldMapGen.SetEnabled(true);
                        seedGB.SetEnabled(true);
                        break;
                    case 2:
                        btnImportMap.SetEnabled(true);
                        newMapGen.SetEnabled(false);
                        oldMapGen.SetEnabled(false);
                        seedGB.SetEnabled(false);
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
            toggleSeed.RegisterValueChangedCallback(v =>{
                if (txtSeed.focusable = !toggleSeed.value)
                    txtSeed.value = "Insert Seed";
                else
                    txtSeed.value = "Random Seed";
            });
            //SmoothedNoise()
            SN_smoothingIterations.RegisterValueChangedCallback(v =>{
                SN_smoothingIterations.Q<TextField>("unity-text-field").label = SN_smoothingIterations.value.ToString();
            });
            SN_initialFillRatio.RegisterValueChangedCallback(v =>{
                SN_initialFillRatio.Q<TextField>("unity-text-field").label = SN_initialFillRatio.value.ToString();
            });
            SN_mapHeight.RegisterValueChangedCallback(v =>{
                SN_mapHeight.Q<TextField>("unity-text-field").label = SN_mapHeight.value.ToString();
            });
            SN_mapWidth.RegisterValueChangedCallback(v =>{
                SN_mapWidth.Q<TextField>("unity-text-field").label = SN_mapWidth.value.ToString();
            });
            SN_mapDepth.RegisterValueChangedCallback(v =>{
                SN_mapDepth.Q<TextField>("unity-text-field").label = SN_mapDepth.value.ToString();
            });

            //RandomConnectedSpheres()
            RCS_mapHeight.RegisterValueChangedCallback(v =>{
                RCS_mapHeight.Q<TextField>("unity-text-field").label = RCS_mapHeight.value.ToString();
            });
            RCS_mapWidth.RegisterValueChangedCallback(v =>{
                RCS_mapWidth.Q<TextField>("unity-text-field").label = RCS_mapWidth.value.ToString();
            });
            RCS_mapDepth.RegisterValueChangedCallback(v =>{
                RCS_mapDepth.Q<TextField>("unity-text-field").label = RCS_mapDepth.value.ToString();
            });
            RCS_sphereRadius.RegisterValueChangedCallback(v =>{
                CaveRoot.Q<Label>("MiniMax").text = $"{RCS_sphereRadius.minValue.ToString("n0")} - {RCS_sphereRadius.maxValue.ToString("n0")}";
                
                RCS_sphereRadius.minValue = (int)RCS_sphereRadius.minValue;
                SimulationSettings.RCS_minRadius = (int)RCS_sphereRadius.minValue;
                RCS_sphereRadius.maxValue = (int)RCS_sphereRadius.maxValue;
                SimulationSettings.RCS_maxRadius = (int)RCS_sphereRadius.maxValue;
            });
            RCS_smoothingIterations.RegisterValueChangedCallback(v =>{
                RCS_smoothingIterations.Q<TextField>("unity-text-field").label = RCS_smoothingIterations.value.ToString();
            });

            RCS_fillRatio.RegisterValueChangedCallback(v =>{
                RCS_fillRatio.Q<TextField>("unity-text-field").label = RCS_fillRatio.value.ToString();
            });
            RCS_sphereConnections.RegisterValueChangedCallback(v =>{
                RCS_sphereConnections.Q<TextField>("unity-text-field").label = RCS_sphereConnections.value.ToString();
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


                switch (dropdownMapGenerators.index)
                {
                    case 0:
                        SimulationSettings.Height = RCS_mapHeight.value;
                        SimulationSettings.Width = RCS_mapWidth.value;
                        SimulationSettings.Depth = RCS_mapDepth.value;
                        SimulationSettings.RCS_smoothingIterations = RCS_smoothingIterations.value;
                        SimulationSettings.RCS_ratioToClear = RCS_fillRatio.value;
                        SimulationSettings.RCS_connectionsToMake = RCS_sphereConnections.value;
                        break;
                    case 1:
                        SimulationSettings.SN_initialFillRatio = SN_initialFillRatio.value;
                        SimulationSettings.smoothingIterations = SN_smoothingIterations.value;
                        SimulationSettings.Height = SN_mapHeight.value;
                        SimulationSettings.Width = SN_mapWidth.value;
                        SimulationSettings.Depth = SN_mapDepth.value;
                        break;
                    default:
                        Debug.LogError("Something went wrong with map generator selection");
                        return;
                }




                
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

                mainCamera.transform.position = new Vector3(-(RCS_mapWidth.value * 0.25f), RCS_mapHeight.value * 1.25f, -(RCS_mapDepth.value * 0.25f));
                mainCamera.transform.LookAt(new Vector3(RCS_mapWidth.value * 0.5f, RCS_mapHeight.value * 0.5f, RCS_mapDepth.value * 0.5f));
                mainCamera.transform.Translate(Vector3.right * (Mathf.Sqrt(RCS_mapWidth.value ^ 2 * RCS_mapDepth.value ^ 2) * 0.2f), Space.Self);

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
        RCS_mapHeight.value = SimulationSettings.Height;
        RCS_mapWidth.value = SimulationSettings.Width;
        RCS_mapDepth.value = SimulationSettings.Depth;
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

    private void QueryButtons(VisualElement root){
            //General
            agentCount = root.Q<SliderInt>("AgentCount");
            duration = root.Q<SliderInt>("SimDuration");
            dropdownAlgorithm = root.Q<DropdownField>("DropdownAlgorithm");
            dropdownMapGenerators = root.Q<DropdownField>("DropdownMapGenerator");
            toggleSeed = root.Q<Toggle>("ToggleRandomSeed");
            txtSeed = root.Q<TextField>("TxtSeed");
            progBar = root.Q<ProgressBar>("ProgressBar");
            btnStart = root.Q<Button>("ButtonStart");
            btnStop = root.Q<Button>("ButtonStop");

            //SmoothedNoise()
            SN_initialFillRatio = root.Q<SliderInt>("SN_InitialFillRatio");
            SN_smoothingIterations = root.Q<SliderInt>("SN_SmoothingIterations");
            SN_mapHeight = root.Q<SliderInt>("SN_MapHeight");
            SN_mapWidth = root.Q<SliderInt>("SN_MapWidth");
            SN_mapDepth = root.Q<SliderInt>("SN_MapDepth");


            //RandomConnectedSpheres()
            RCS_fillRatio = root.Q<SliderInt>("RCS_FillRatio");
            RCS_sphereRadius = root.Q<MinMaxSlider>("SphereRadius");
            RCS_sphereConnections = root.Q<SliderInt>("SphereConnections");
            RCS_smoothingIterations = root.Q<SliderInt>("RCS_SmoothingIterations");
            RCS_mapHeight = root.Q<SliderInt>("RCS_MapHeight");
            RCS_mapWidth = root.Q<SliderInt>("RCS_MapWidth");
            RCS_mapDepth = root.Q<SliderInt>("RCS_MapDepth");

            //ImportMap()
            btnImportMap = root.Q<Button>("ButtonImportMap");
            btnImportMap.SetEnabled(false);


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
