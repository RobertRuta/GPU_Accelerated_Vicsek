using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DispatchGPUCode : MonoBehaviour
{
    ComputeBuffer positionBuffer;
    ComputeBuffer argsBuffer;
    public ComputeShader positionShader;
    public int particleCount = 10000;
    public float radius = 5;
    public Mesh particleMesh;
    public Material particleMaterial;
    int subMeshIndex = 0;
    public Material mat;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    public Texture2D NoiseTexture; 

    // Start is called before the first frame update
    void Start()
    {
        positionBuffer = new ComputeBuffer(particleCount, 4*3);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        Vector3[] positionValues = new Vector3[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            // float theta = Mathf.Acos(1 - 2 * Random.Range(0.0f, Mathf.PI));
            // float phi = Random.Range(0.0f, 2 * Mathf.PI);
            // float x = radius * Mathf.Cos(phi)*Mathf.Sin(theta);
            // float z = radius * Mathf.Sin(phi)*Mathf.Sin(theta);
            // float y = radius * Mathf.Cos(theta);
            // positionValues[i] = new Vector3(x, y, z);
            positionValues[i] = new Vector3(0, 0, 0);
            // print("Draw " + i);
            // Graphics.DrawMesh(mesh, positionValues[i], Quaternion.identity, mat, 0);
        }

        positionBuffer.SetData(positionValues);
    }

    void Update()
    {
        int positionKernel = positionShader.FindKernel("DistributeOnSphere");
        positionShader.SetFloat("radius", radius);
        positionShader.SetFloat("time", Time.time);
        positionShader.SetInt("particle_count", particleCount);
        positionShader.SetBuffer(positionKernel, "positionBuffer", positionBuffer);
        positionShader.SetTexture(positionKernel, "NoiseTexture", NoiseTexture);
        positionShader.Dispatch(positionKernel, particleCount / 128, 1, 1);

        InitiateArgs();
        particleMaterial.SetBuffer("positionBuffer", positionBuffer);
        Graphics.DrawMeshInstancedIndirect(particleMesh, subMeshIndex, particleMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }


    void InitiateArgs()
    {
        // Indirect args
        if (particleMesh != null) {
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, particleMesh.subMeshCount - 1);
            args[0] = (uint)particleMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)particleCount;
            args[2] = (uint)particleMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)particleMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);
    }
}
