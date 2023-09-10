using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Visualiser : MonoBehaviour
{
    public Mesh particleMesh;
    public int subMeshIndex;
    public Material particleMaterial;
    ComputeBuffer positionBuffer, velocityBuffer; 
    ComputeBuffer argsBuffer;
    SimulationControl sim;
    void Start()
    {
        sim = GetComponent<SimulationControl>();
        argsBuffer = new ComputeBuffer(5, 4);
    }

    public void RenderParticles() {
        
        int particleCount = sim.particleCount;
        // Indirect args
        uint[] args = new uint[5]{0,0,0,0,0};
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

        particleMaterial.SetBuffer("positionBuffer", sim.positionBuffer.buffer);
        // particleMaterial.SetBuffer("velocityBuffer", sim.velocityBuffer.buffer);

        Vector4[] positionArray = new Vector4[sim.particleCount];
        sim.positionBuffer.buffer.GetData(positionArray);   
        for (int i = 0; i < 100; i++)
            print(positionArray[i]);


        Graphics.DrawMeshInstancedIndirect(particleMesh, subMeshIndex, particleMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }
}
