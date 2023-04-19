using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimulationCamera : MonoBehaviour
{
    Vector3 target; // The target object to face
    public float xSpeed = 120.0f; // Horizontal rotation speed
    public float ySpeed = 120.0f; // Vertical rotation speed
    public float zoomSpeed = 10f;

    private float x = 0.0f;
    private float y = 0.0f;
    VicsekController sim;
    float boxWidth;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;


        sim = GameObject.Find("sim").GetComponent<VicsekController>();
        boxWidth = sim.box_width;

        target = Vector3.one * boxWidth/2;
        transform.LookAt(target);
        transform.position = Vector3.one*boxWidth/2 + new Vector3(-1f,0,-1f)*200f;
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
        float zoom = zoom_input*zoomSpeed;

        Vector3 separation = transform.position - target;
        transform.position += separation.normalized*zoom;


        if (Input.GetMouseButton(0))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;

            y = Mathf.Clamp(y, -89, 89);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            float distance = (transform.position - target).magnitude;
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target;

            transform.rotation = rotation;
            transform.position = position;
        }


        transform.LookAt(target);
    }
}