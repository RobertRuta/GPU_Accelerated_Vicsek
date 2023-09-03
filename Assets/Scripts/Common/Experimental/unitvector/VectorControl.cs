using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class VectorControl : MonoBehaviour
{
    [Range(0, Mathf.PI)]
    public float theta = 0;
    [Range(0, 2*Mathf.PI)]
    public float phi = 0;
    public float calced_phi = 0;
    public float calced_theta = 0;
    public Vector3 vector;
    public Vector3 rotAxis;
    public float alpha;


    // Update is called once per frame
    void Start()
    {
        vector = new Vector3(0,0,0);
    }
    void Update()
    {
        float theta_deg = theta * 180/Mathf.PI;
        float phi_deg = phi * 180/Mathf.PI;
        transform.rotation = Quaternion.Euler(theta_deg, phi_deg, 0.0f);

        vector.x = Mathf.Sin(theta)*Mathf.Cos(phi)*transform.localScale.y;
        vector.y = Mathf.Cos(theta)*transform.localScale.y;
        vector.z = Mathf.Sin(theta)*Mathf.Sin(phi)*transform.localScale.y;

        calced_theta = Mathf.Acos(vector.y / vector.magnitude);
        calced_phi = Mathf.Acos(vector.x / Mathf.Sin(theta)*vector.magnitude);

        Vector3 rotatedVector = vector * Mathf.Cos(alpha) + Vector3.Cross(rotAxis, vector) * Mathf.Sin(alpha) + rotAxis * Vector3.Dot(vector, rotAxis) * (1-Mathf.Cos(alpha));
        

        
    }
}
