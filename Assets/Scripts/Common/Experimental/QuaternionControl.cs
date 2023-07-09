using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaternionControl : MonoBehaviour
{
    Transform cube; 
    public float xSpeed, ySpeed;
    public float distance;
    float x, y;
    // Start is called before the first frame update
    void Start()
    {
        cube = GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
            y += Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;

            y = Mathf.Clamp(y, -90, 90);

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            cube.rotation = rotation;
        }
    }
}
