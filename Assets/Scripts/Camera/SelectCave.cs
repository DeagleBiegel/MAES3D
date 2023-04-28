using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MAES3D 
{
    public class SelectCave : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown("escape"))
            {
                CameraController cameraController = GameObject.Find("Main Camera").GetComponent<CameraController>();

                if (!cameraController.IsTransitioning()) 
                {
                    // Set the cave as the selected object for the camera
                    cameraController.SetTargetOffset(transform, new Vector3(SimulationSettings.Width / 2, SimulationSettings.Height / 2, SimulationSettings.Depth / 2));

                    // Change UI show the cave
                    UIBehaviour UI = GameObject.Find("UI").GetComponent<UIBehaviour>();
                    UI.SetAgentIndex(-1);
                    UI.ChangeCam();
                }
            }
        }
    }
}
