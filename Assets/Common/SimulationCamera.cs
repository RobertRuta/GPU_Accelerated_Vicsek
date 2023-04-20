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
    public float inertiaDampening = 5.0f;

    private float x = 0.0f;
    private float y = 0.0f;
    float dx, dy;
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
        float zoom_input = Input.GetAxis("Mouse ScrollWheel");
        float zoom = -zoom_input*zoomSpeed;

        Vector3 separation = transform.position - target;
        transform.position += separation.normalized*zoom;


        if (Input.GetMouseButton(0)){
            dx = Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            dy = Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;           

        }
        else{
            dx = Mathf.Lerp(dx, 0, inertiaDampening * Time.deltaTime);
            dy = Mathf.Lerp(dy, 0, inertiaDampening * Time.deltaTime);
        }

        x += dx;
        y -= dy;
        y = Mathf.Clamp(y, -89, 89);

        float distance = separation.magnitude;
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 position = rotation * initPosition + target;

        transform.rotation = rotation;
        transform.position = position;
        

        transform.LookAt(target);
    }
}