using UnityEngine;
using GPUCompute;

public class Visualiser : MonoBehaviour
{
    public Material particleMaterial;
    public Mesh particleMesh;
    public int subMeshIndex;
    public float particleSize;
    public float colorIntensity;
    public bool enableHeading;

    Buffer<uint> argsBuffer;
    uint[] args = new uint[5]{0,0,0,0,0};
    SimulationControl sim;


    void Awake() {
        sim = GetComponent<SimulationControl>();
        argsBuffer = new Buffer<uint>(5, "args", args, ComputeBufferType.IndirectArguments);
    }


    public void RenderParticles(ComputeBuffer particleBuffer) {
        
        SetupIndirectArgs();
        particleMaterial.SetBuffer("particleBuffer", sim.particleBuffer.buffer);
        particleMaterial.SetFloat("_ColorIntensity", colorIntensity);
        particleMaterial.SetFloat("_ParticleSize", particleSize);
        int headingInt = 0;
        if (enableHeading)
            headingInt = 1;
        particleMaterial.SetInt("_EnableHeading", headingInt);
        Graphics.DrawMeshInstancedIndirect(particleMesh, subMeshIndex, particleMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer.buffer);
    }


    public void SetupIndirectArgs() {
        int particleCount = sim.particleCount;
        // Indirect args
        if (particleMesh != null) {
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, particleMesh.subMeshCount - 1);
            args[0] = (uint)particleMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)particleCount;
            args[2] = (uint)particleMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)particleMesh.GetBaseVertex(subMeshIndex);
        }
        else {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.buffer.SetData(args);
    }


    void OnDisable() {
        argsBuffer.Dispose();
    }


    public void LoadMesh(string meshName) {
        print("Loading mesh: " + meshName);
        string meshPath = "Meshes/" + meshName;
        Mesh loadedMesh = Resources.Load<Mesh>(meshPath);
        particleMesh = loadedMesh;
    }
}