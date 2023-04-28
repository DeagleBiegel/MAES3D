using System.Collections;
using System.Collections.Generic;
using MAES3D.Agent;
using UnityEngine;

namespace MAES3D 
{
    public class SelectAgent : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            // Click on a drone to make it the target
            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit))
                {
                    SubmarineAgent hitDrone = hit.transform.GetComponent<SubmarineAgent>();

                    if (hit.transform.gameObject == gameObject)
                    {
                        // Changed target for camera
                        CameraController cameraController = GameObject.Find("Main Camera").GetComponent<CameraController>();
                        cameraController.SetTarget(hit.transform);

                        // Change UI show the drone
                        UIBehaviour UI = GameObject.Find("UI").GetComponent<UIBehaviour>();
                        UI.SetAgentIndex(hitDrone.Id);
                        UI.ChangeCam();
                    }
                }
            }
        }
    }
}
