using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float distance = 200.0f; // Distance from target
    public float xSpeed = 120.0f; // Horizontal rotation speed
    public float ySpeed = 120.0f; // Vertical rotation speed
    Vector3 target; // The target object to face

    private float x, y = 0.0f;
    private float dx, dy = 0.0f;
    public float inertialDamping;

    VicsekController sim;
    float boxWidth;
    Vector3 initPosition;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;


        sim = GameObject.Find("sim").GetComponent<VicsekController>();
        boxWidth = sim.box_width;

        target = Vector3.one * boxWidth/2;
        transform.LookAt(target);
        initPosition = Vector3.one*boxWidth/2 + new Vector3(-1,0,-1)*200f;
        transform.position = initPosition;
    }

    void Update()
    {
        // Check if interacting with an IMGUI element
        if (GUIUtility.hotControl != 0)
        {
            return;
        }

        boxWidth = sim.box_width;
        target = Vector3.one * boxWidth/2;
        distance = (transform.position - target).magnitude;

        // Zooming functionality
        float zoom_input = Input.GetAxis("Mouse ScrollWheel");  // Store middle mouse rolling input in variable
        float zoom = zoom_input * 50f;    // scale zoom by zoom speed
        distance -= zoom;   // add the zoom to distance

        // Allows for rotating the camera when LMB held
        if (Input.GetMouseButton(0)){   
            dx = Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            dy = Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;           
        }
        // When LMB let go, give the rotation some inertia
        else{
            dx = Mathf.Lerp(dx, 0, inertialDamping * Time.deltaTime);
            dy = Mathf.Lerp(dy, 0, inertialDamping * Time.deltaTime);
        }

        // Add the input spin or inertia spin component to 2D rotation component
        x += dx;
        y -= dy;
        // Clamp the pitch of the camera such that it does not cross 90 degrees
        y = Mathf.Clamp(y, -90, 90);

        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * new Vector3(-1.0f, -1.0f, -distance) + target;

        transform.rotation = rotation;
        transform.position = position;


    }
}