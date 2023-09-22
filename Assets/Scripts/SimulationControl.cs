using UnityEngine;
using BufferSorter;
using vicsek;
using GPTCompute;
using System.Collections.Generic;

public class SimulationControl : MonoBehaviour {

    // SIMULATION PARAMETERS
    public int particleCount = 100000;
    public float boxWidth = 100f;
    public float radius = 5;
    public float speed = 5;
    public float timeStep = 1f/60f;
    [Range(0f, 1f)] public float noise = 1.0f;

    //
    public Vector2 radius_range;
    public float particleDensity, particleCellDensity;

    // Variables that can be moved elsewhere
    public ComputeShader ParticleCompute, SortShader;


    // Additional convenience variables
    int cachedParticleCount = -1;
    float cachedBoxWidth = -1f;
    float cachedRadius = -1f;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    
    // Compute buffers
    public Buffer<Vector4> positionBuffer, velocityBuffer;
    public Buffer<Particle> particleBuffer;
    Buffer<Vector4> debug1Buffer, debug2Buffer, debug3Buffer;
    Buffer<uint> particleIDBuffer, cellIDBuffer, keysBuffer;
    Buffer<Vector2Int> startendBuffer;
    Buffer<Cell> cellBuffer;
    
    // Compute shader kernels
    Kernel particleRearrange;
    Kernel buildStartEndIDs;
    Kernel particleUpdate;
    Kernel optimizedParticleUpdate;
    Kernel cellReset;

    // Number of thread groups
    int groupCount;

    // Simulation space and grid variables
    [SerializeField]
    Vector2 box_range;
    [SerializeField]
    Vector3 box;
    [SerializeField]
    Vector3Int grid_dims;
    [SerializeField]
    public int cellCount;
    int frameCounter = 0;
    

    // Third-party Sorter - Emmet GITHUB
    Sorter sorter;
    public bool optimized;

    const uint MAX_BUFFER_BYTES = 2147483648;
    int max_cell_count;
    Vector3Int threadGroups;

    Visualiser visualiser;


    // RUN ON FIRST FRAME

    void Start() {

        sorter = new Sorter(SortShader);
        visualiser = GetComponent<Visualiser>();

        groupCount = Mathf.CeilToInt((float)particleCount / 128);
        threadGroups = new Vector3Int(groupCount, 1, 1);
        max_cell_count = (int)(MAX_BUFFER_BYTES / 8);

        UpdateSimParams();
        SetupBuffers();
        SetupKernels();
    }


    // RUN ONCE EVERY FRAME

    void Update() {
        if (cachedParticleCount != particleCount || cachedBoxWidth != boxWidth || cachedRadius != radius) {
            UpdateSimParams();
            
            // Reset Sorter
            if (sorter != null)
                sorter.Dispose();
            sorter = new Sorter(SortShader);
            
            ResetBuffers();
            SetupKernels();
        }

        UpdateComputeShaderVariables();

        // Each particle only searches neighbouring cells
        if (optimized) {
            sorter.Sort(keysBuffer.buffer, cellIDBuffer.buffer);
            particleRearrange.InitBuffers();
            particleRearrange.Run();
            buildStartEndIDs.Run();
            optimizedParticleUpdate.Run();
        }
        // O(N^2) computational complexity
        else {
            particleUpdate.Run();
        }

        visualiser.RenderParticles(particleBuffer.buffer);
    }


    // HELPER FUNCTIONS

    // Recalculate simulation parameters
    void UpdateSimParams()
    {
        // Set particle count
        if (particleCount != cachedParticleCount)
            particleCount = Mathf.NextPowerOfTwo(particleCount) >> 1;
        // Set box vector
        box = new Vector3(boxWidth, boxWidth, boxWidth);
        // Recalculate box vector - box must be the same size as the grid boundaries
        box = new Vector3((int)(box.x/radius) * radius, (int)(box.y/radius) * radius, (int)(box.z/radius) * radius);

        // Calculate cell count
        grid_dims = new Vector3Int((int)(box.x/radius), (int)(box.y/radius), (int)(box.z/radius));
        cellCount = grid_dims.x*grid_dims.y*grid_dims.z;

        particleDensity = particleCount / (boxWidth*boxWidth*boxWidth);
        particleCellDensity = particleCount / cellCount;

        // Caching simulation parameters
        cachedParticleCount = particleCount;
        cachedBoxWidth = boxWidth;
        cachedRadius = radius;
    }

    // Create array of random particle positions and velocities
    Particle[] InitParticleArray() {
        Particle[] particleArray = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++) {
            particleArray[i].position = new Vector4(Random.Range(0f, boxWidth), Random.Range(0f, boxWidth), Random.Range(0f, boxWidth), 10f);
            particleArray[i].velocity = Random.onUnitSphere * speed;
        }
        return particleArray;
    }

    // Initialise buffers
    void SetupBuffers() {
        Particle[] initArray = InitParticleArray();
        particleBuffer = new Buffer<Particle>(particleCount, "particleBuffer", initArray);
        particleIDBuffer = new Buffer<uint>(particleCount, "particleIDs");
        cellIDBuffer = new Buffer<uint>(particleCount, "cellIDs");
        keysBuffer = new Buffer<uint>(particleCount, "keys");
        startendBuffer = new Buffer<Vector2Int>(cellCount, "startendIDs");
        cellBuffer = new Buffer<Cell>(cellCount, "cellBuffer");
        debug1Buffer = new Buffer<Vector4>(particleCount, "debugBuffer");
        debug2Buffer = new Buffer<Vector4>(particleCount, "debugBuffer2");
        debug3Buffer = new Buffer<Vector4>(particleCount, "debugBuffer3");
    }

    // Initialise Compute Kernels
    void SetupKernels() {
        // Create kernels
        particleRearrange = new Kernel(ParticleCompute, "RearrangeParticleIDs", threadGroups);
        buildStartEndIDs = new Kernel(ParticleCompute, "BuildStartEndIDs", threadGroups);
        particleUpdate = new Kernel(ParticleCompute, "ParticleUpdate", threadGroups);
        optimizedParticleUpdate = new Kernel(ParticleCompute, "OptimizedParticleUpdate", threadGroups);
        cellReset = new Kernel(ParticleCompute, "ResetCellBuffer", threadGroups);

        // Associate buffers with kernels
        particleRearrange.SetBuffers(new List<IBuffer>{particleIDBuffer, keysBuffer});
        buildStartEndIDs.SetBuffers(new List<IBuffer>{particleIDBuffer, cellIDBuffer, startendBuffer, cellBuffer});
        particleUpdate.SetBuffers(new List<IBuffer>{particleBuffer});
        optimizedParticleUpdate.SetBuffers(new List<IBuffer>{particleBuffer, particleIDBuffer, startendBuffer, cellBuffer, cellIDBuffer, debug1Buffer, debug2Buffer, debug3Buffer});
        cellReset.SetBuffers(new List<IBuffer>{cellBuffer});

        // Attach buffers to kernels
        particleRearrange.InitBuffers();
        buildStartEndIDs.InitBuffers();
        particleUpdate.InitBuffers();
        optimizedParticleUpdate.InitBuffers();
        cellReset.InitBuffers();
    }

    // Dispose buffers and create again
    void ResetBuffers() {
        Particle[] initArray = InitParticleArray();
        particleBuffer.Reset(particleCount, initArray);
        particleIDBuffer.Reset(particleCount);
        keysBuffer.Reset(particleCount);
        startendBuffer.Reset(cellCount);
        cellIDBuffer.Reset(particleCount);
        cellBuffer.Reset(cellCount);
        debug1Buffer.Reset(particleCount);
        debug2Buffer.Reset(particleCount);
        debug3Buffer.Reset(particleCount);
    }

    // Send variable values from CPU to GPU
    void UpdateComputeShaderVariables() {
        // Update compute shader variables
        ParticleCompute.SetInt("particle_count", particleCount);
        ParticleCompute.SetFloats("box", new [] {box.x, box.y, box.z});
        ParticleCompute.SetFloat("radius", radius);
        ParticleCompute.SetFloat("speed", speed);
        ParticleCompute.SetFloat("dt", timeStep);
        ParticleCompute.SetFloat("time", Time.time);
        ParticleCompute.SetFloat("noise", noise);
        ParticleCompute.SetInt("state", (int)(Time.time*1000 % 255));
        ParticleCompute.SetInts("grid_dims", new [] {grid_dims.x, grid_dims.y, grid_dims.z});
        ParticleCompute.SetInt("frame_counter", frameCounter);
    }

    // Runs on application quit
    void OnDisable() {
        particleBuffer.Dispose();
        particleIDBuffer.Dispose();
        cellIDBuffer.Dispose();
        startendBuffer.Dispose();
        cellBuffer.Dispose();
        keysBuffer.Dispose();
        debug1Buffer.Dispose();
        debug2Buffer.Dispose();
        debug3Buffer.Dispose();
        if (sorter != null)
            sorter.Dispose();
    }
}