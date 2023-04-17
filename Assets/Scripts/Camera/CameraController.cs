using System.Collections;
using System.Collections.Generic;
using MAES3D.Agent;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public Vector3 centerOffset;

    public float rotationSpeed;
    public float zoomSpeed;
    public float minZoomDistance;
    public float maxZoomDistance;

    private float currentZoom;
    private float currentYRotation;
    private float currentXRotation;

    private void Start()
    {
        offset = new Vector3(0.5f, 0.5f, 0.5f);
        centerOffset = new Vector3(25f, 25f, 25f);

        rotationSpeed = 3.0f;
        zoomSpeed = 1.0f;
        minZoomDistance = 2.0f;
        maxZoomDistance = 100.0f;

        target.transform.position = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z);
        currentZoom = Vector3.Distance(transform.position, target.position);
        currentYRotation = transform.eulerAngles.y;
        currentXRotation = transform.eulerAngles.x;
    }

    private void Update()
    {
        // Default camera to 'Simulator' in case there is no target
        if (target == null) 
        {
            GameObject light = GameObject.Find("Simulator");
            SetTarget(light.transform);  
        }

        // Rotate camera around drone
        if (Input.GetMouseButton(1))
        {
            currentYRotation += Input.GetAxis("Mouse X") * rotationSpeed;
            currentXRotation -= Input.GetAxis("Mouse Y") * rotationSpeed;

            currentXRotation = Mathf.Clamp(currentXRotation, 0, 90);
        }

        // Click on a drone to make it the target
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                SubmarineAgent hitDrone = hit.transform.GetComponent<SubmarineAgent>();
                if (hitDrone != null)
                {
                    SetTarget(hit.transform);
                }
            }
        }

        // Zoom in/out using scroll wheel
        currentZoom -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoomDistance, maxZoomDistance);

        // Apply changes
        ApplyCameraTransform();
    }

    private void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0);
        Vector3 targetPosition = target.position + centerOffset;
        transform.position = targetPosition - (rotation * offset * currentZoom);
        transform.LookAt(targetPosition);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        centerOffset = new Vector3(0, 0, 0);

        /* Camera settings for a drone */
        currentZoom = 5;
        zoomSpeed = 3f;
    }

    public void SetTargetOffset(Transform newTarget, Vector3 newCenterOffset) 
    {
        target = newTarget;
        centerOffset = newCenterOffset;


        /* Camera settings for chunk */
        currentZoom = (SimulationSettings.Width + SimulationSettings.Height + SimulationSettings.Depth) / 3;
        zoomSpeed = 20F;
    }
}
