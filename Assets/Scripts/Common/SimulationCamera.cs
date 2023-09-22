using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimulationCamera : MonoBehaviour
{
    Vector3 target; // The target object to face
    public float xSpeed = 120.0f; // Horizontal rotation speed
    public float ySpeed = 120.0f; // Vertical rotation speed
    public float zoomSpeed = 100f;
    public float inertialDamping = 5.0f;

    private float x = 0.0f;
    private float y = 0.0f;
    [SerializeField]
    float distance;
    float dx, dy;
    SimulationControl sim;
    float boxWidth;
    Vector3 initPosition;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        sim = GameObject.Find("sim").GetComponent<SimulationControl>();
        boxWidth = sim.boxWidth;

        target = Vector3.one * boxWidth/2;
        transform.LookAt(target);
        initPosition = Vector3.one*boxWidth/2 + new Vector3(-1,0,-1)*200f;
        transform.position = initPosition;
        
        distance = (transform.position - target).magnitude;
    }

    void Update()
    {
        // Check if interacting with an IMGUI element
        if (GUIUtility.hotControl != 0)
        {
            return;
        }

        boxWidth = sim.boxWidth;
        target = Vector3.one * boxWidth/2;

        // Zooming functionality
        float zoom_input = Input.GetAxis("Mouse ScrollWheel");  // Store middle mouse rolling input in variable
        float zoom = zoom_input * zoomSpeed;    // scale zoom by zoom speed
        distance -= zoom;   // add the zoom to distance
        
        // Allows for rotating the camera when LMB held
        if (Input.GetMouseButton(0)){   
            dx = Input.GetAxis("Mouse X") * xSpeed;
            dy = Input.GetAxis("Mouse Y") * ySpeed;           
        }
        // When LMB let go, give the rotation some inertia
        else{
            dx = Mathf.Lerp(dx, 0, inertialDamping);
            dy = Mathf.Lerp(dy, 0, inertialDamping);
        }

        // Add the input spin or inertia spin component to 2D rotation component
        x += dx;
        y -= dy;
        // Clamp the pitch of the camera such that it does not cross 90 degrees
        y = Mathf.Clamp(y, -90, 90);

        Quaternion rotation = Quaternion.Euler(y, x, 0);
        // Vector3 position = rotation * (initPosition.normalized * distance) + target;
        Vector3 position = rotation * new Vector3(-1.0f, -1.0f, -distance) + target;

        transform.rotation = rotation;
        transform.position = position;
    }
}