using System.Collections;
using System.Collections.Generic;
using MAES3D.Agent;
using MAES3D;
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
    public float transitionDuration;

    private bool isTransitioning;
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
        maxZoomDistance = 125.0f;

        transitionDuration = 0.5f;
        isTransitioning = false;

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

        // Mouse X position and the width of the screen
        float mouseX = Input.mousePosition.x;
        int windowWidth = Screen.width;

        // Mouse is on the UI at the right side of the screen (UI is 300px wide)
        if (mouseX <= windowWidth - 300) 
        {
            // Rotate camera around drone
            if (Input.GetMouseButton(1))
            {
                currentYRotation += Input.GetAxis("Mouse X") * rotationSpeed;
                currentXRotation -= Input.GetAxis("Mouse Y") * rotationSpeed;

                currentXRotation = Mathf.Clamp(currentXRotation, 0, 90);
            }

            // Zoom in/out using scroll wheel
            currentZoom -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoomDistance, maxZoomDistance);
        }

        // Apply changes
        ApplyCameraTransform();
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    private void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0);
        Vector3 targetPosition = target.position + centerOffset;
        transform.position = targetPosition - (rotation * offset * currentZoom);
        transform.LookAt(targetPosition);
    }

    private IEnumerator SmoothTransition(Transform newTarget, Vector3 newCenterOffset, float newZoom)
    {
        isTransitioning = true;
        float transitionTime = 0;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 startCenterOffset = centerOffset;
        float startZoom = currentZoom;

        while (transitionTime < transitionDuration)
        {
            transitionTime += Time.deltaTime;
            float t = transitionTime / transitionDuration;

            target = newTarget;
            centerOffset = Vector3.Lerp(startCenterOffset, newCenterOffset, t);
            currentZoom = Mathf.Lerp(startZoom, newZoom, t);

            ApplyCameraTransform();

            transform.position = Vector3.Lerp(startPosition, transform.position, t);
            transform.rotation = Quaternion.Slerp(startRotation, transform.rotation, t);

            // Give control back to Unity
            yield return null;
        }

        isTransitioning = false;
    }

    public void SetTarget(Transform newTarget)
    {
        if (!isTransitioning && target != newTarget)
        {
            zoomSpeed = 2f;
            float newZoom = 5;
            StartCoroutine(SmoothTransition(newTarget, Vector3.zero, newZoom));
        }
    }

    public void SetTargetOffset(Transform newTarget, Vector3 newCenterOffset) 
    {
        if (!isTransitioning && target != newTarget)
        {
            zoomSpeed = 10f;
            float newZoom = (SimulationSettings.Width + SimulationSettings.Height + SimulationSettings.Depth) / 3 + 20;
            StartCoroutine(SmoothTransition(newTarget, new Vector3(SimulationSettings.Width / 2, SimulationSettings.Height / 2, SimulationSettings.Depth / 2), newZoom));
        }
    }
}
