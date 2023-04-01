using UnityEngine;
using System.Runtime.InteropServices;

public class VicsekController : MonoBehaviour {
    public int particleCount = 100000;
    public Mesh particleMesh;
    public Material particleMaterial;
    public int subMeshIndex = 0;

    private int cachedParticleCount = -1;
    private int cachedSubMeshIndex = -1;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    
    public ComputeShader ParticleCompute;
    int particleUpdateKernel;
    int gridUpdateKernel;
    int group_count;
    public float radius = 5;
    public float speed = 5;
    Vector3 box = new Vector3(100f, 100f, 100f);
    Vector3 grid_dims;
    struct Particle
    {
        public Vector4 position;
        public Vector4 velocity;
    }

    private ComputeBuffer particleBuffer;
    private ComputeBuffer particleIDBuffer;
    private ComputeBuffer cellIDBuffer;


    void Start() {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        particleUpdateKernel = ParticleCompute.FindKernel("ParticleUpdate");
        InitiateSim();
    }

    void Update() {
        // Update starting position buffer
        if (cachedParticleCount != particleCount || cachedSubMeshIndex != subMeshIndex)
            InitiateSim();

        // Pad input
        if (Input.GetAxisRaw("Horizontal") != 0.0f)
            particleCount = (int)Mathf.Clamp(particleCount + Input.GetAxis("Horizontal") * 40000, 1.0f, 5000000.0f);


        // Update Grid
        // ParticleCompute.Dispatch(gridUpdateKernel, group_count, 1, 1);
        
        // Update Particle Positions
        ParticleCompute.SetFloat("radius", radius);
        ParticleCompute.SetFloat("speed", speed);
        ParticleCompute.SetFloat("dt", Time.deltaTime);
        ParticleCompute.Dispatch(particleUpdateKernel, group_count, 1, 1);

        // Render
        Graphics.DrawMeshInstancedIndirect(particleMesh, subMeshIndex, particleMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }

    void OnGUI() {
        GUI.Label(new Rect(265, 25, 200, 30), "Instance Count: " + particleCount.ToString());
        particleCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)particleCount, 1.0f, 5000000.0f);
    }

    void InitiateSim() {
        // Ensure submesh index is in range
        group_count = Mathf.CeilToInt((float)particleCount / 128);
        
        ParticleCompute.SetInt("particle_count", particleCount);
        ParticleCompute.SetFloats("box", new [] {box.x, box.y, box.z});

        box = new Vector3((int)(box.x/radius) * radius, (int)(box.y/radius) * radius, (int)(box.z/radius) * radius);
        grid_dims = box/radius + Vector3.one;
        ParticleCompute.SetInts("grid_dims", new [] {(int)grid_dims.x, (int)grid_dims.y, (int)grid_dims.z});

        if (particleMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, particleMesh.subMeshCount - 1);

        // Positions
        if (particleBuffer != null)
            particleBuffer.Release();
        particleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
        cellIDBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)));
        particleIDBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)));
        Particle[] particles = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++) {
            float size = Random.Range(0.05f, 0.25f);
            particles[i].position = new Vector4(Random.Range(-50f, 50f), Random.Range(-50f, 50f), Random.Range(-50f, 50f), size);
            Vector3 vel = Random.onUnitSphere;
            particles[i].velocity = new Vector4(vel.x, vel.y, vel.z, size);
        }
        particleBuffer.SetData(particles);

        // ParticleCompute.SetBuffer(particleUpdateKernel, "particleBuffer", particleBuffer);
        ParticleCompute.SetBuffer(gridUpdateKernel, "particleIDs", particleIDBuffer);
        ParticleCompute.SetBuffer(gridUpdateKernel, "cellIDs", cellIDBuffer);
        ParticleCompute.SetBuffer(gridUpdateKernel, "particleBuffer", particleBuffer);

        particleMaterial.SetBuffer("particleBuffer", particleBuffer);

        // Indirect args
        if (particleMesh != null) {
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

        cachedParticleCount = particleCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    void OnDisable() {
        if (particleBuffer != null)
            particleBuffer.Release();
        particleBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }
}