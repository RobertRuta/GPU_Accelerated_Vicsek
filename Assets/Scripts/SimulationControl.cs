using UnityEngine;
using BufferSorter;
using vicsek;
using GPUCompute;
using System.Collections.Generic;

public class SimulationControl : MonoBehaviour {

    // SIMULATION PARAMETERS
    public int particleCount = 100000;
    public float boxWidth = 100f;
    public float radius = 5;
    public float speed = 5;
    [Range(0f, 1f)] public float noise = 1.0f;
    // [SerializeField] float timeStep = 1f/60f;

    public Vector2 radiusRange;
    public float particleDensity, particleCellDensity;

    // Compute shader assignment
    public ComputeShader ParticleCompute, SortShader;


    // Additional convenience variables
    int cachedParticleCount = -1;
    float cachedBoxWidth = -1f;
    float cachedRadius = -1f;
    float cachedTime = 0f;
    public bool resetToggle = false;

    // Compute buffers
    public Buffer<Particle> particleBuffer, particleInBuffer;
    public Buffer<Vector4> debugBuffer1, debugBuffer2, debugBuffer3;
    public Buffer<uint> particleIDBuffer, cellIDBuffer, keysBuffer;
    public Buffer<Vector2Int> startendBuffer;
    public Buffer<Cell> cellBuffer;
    
    // Compute shader kernels
    Kernel particleUpdate;
    Kernel optimizedParticleUpdate;
    Kernel particleRearrange;
    Kernel buildStartEndIDs;
    Kernel cellReset;
    Kernel copyBuffer;

    // Number of thread groups
    int groupCount;

    // Simulation space and grid variables
    [SerializeField]
    Vector2 boxRange;
    [SerializeField]
    Vector3 box;
    [SerializeField]
    public Vector3Int grid_dims;
    [SerializeField]
    public int cellCount;
    

    // Third-party Bitonic Sorter - Emmet GITHUB
    Sorter sorter;
    public bool optimized;
    public int targetFPS = 60;

    const uint MAX_BUFFER_BYTES = 2147483648;
    int max_cell_count;
    Vector3Int threadGroups;

    Visualiser visualiser;

    UIControl UI;


    ///// ----- RUN ON FIRST FRAME ----- /////

    void Start() {
        Application.targetFrameRate = targetFPS;

        sorter = new Sorter(SortShader);
        visualiser = GetComponent<Visualiser>();
        UI = GetComponent<UIControl>();

        max_cell_count = (int)(MAX_BUFFER_BYTES / 8);

        UpdateSimulationParameters();
        SetupBuffers();
        SetupKernels();
    }


    ///// ----- RUN ONCE EVERY FRAME ----- /////

    void Update() {
        if (((cachedParticleCount != particleCount || cachedBoxWidth != boxWidth || cachedRadius != radius) && ((Time.time - cachedTime) > 1f)) || resetToggle) {
            UpdateSimulationParameters();
            
            // Reset Sorter
            if (sorter != null)
                sorter.Dispose();
            sorter = new Sorter(SortShader);
            
            ResetBuffers();
            SetupKernels();
            
            resetToggle = false;
        }

        UpdateComputeShaderVariables();

        if (!UI.isPaused) {
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
            
            copyBuffer.Run();
        }

        visualiser.RenderParticles(particleBuffer.buffer);
    }


    ///// ----- RUNS ON APPLICATION QUIT / SIM GAMEOBJECT DEACTIVATION ----- /////

    void OnDisable() {
        particleBuffer.Dispose();
        particleInBuffer.Dispose();
        particleIDBuffer.Dispose();
        cellIDBuffer.Dispose();
        startendBuffer.Dispose();
        cellBuffer.Dispose();
        keysBuffer.Dispose();
        debugBuffer1.Dispose();
        debugBuffer2.Dispose();
        debugBuffer3.Dispose();
        if (sorter != null)
            sorter.Dispose();
    }



    ///// ----- HELPER FUNCTIONS ----- /////

    // Set target frame rate
    public void SetTargetFPS() {
        Application.targetFrameRate = targetFPS;
    }

    // Recalculate simulation parameters
    void UpdateSimulationParameters()
    {
        // Set particle count
        if (particleCount != cachedParticleCount)
            particleCount = Mathf.NextPowerOfTwo(particleCount) >> 1;
        
        // Update number of thread groups based on new particleCount
        groupCount = Mathf.CeilToInt((float)particleCount / 128);
        threadGroups = new Vector3Int(groupCount, 1, 1);

        // Set box vector
        box = new Vector3(boxWidth, boxWidth, boxWidth);
        // Recalculate box vector - box must be the same size as the grid boundaries
        box = new Vector3((int)(box.x/radius) * radius, (int)(box.y/radius) * radius, (int)(box.z/radius) * radius);

        RecalcBoxRange();
        RecalcRadiusRange();

        // Calculate cell count
        grid_dims = new Vector3Int((int)(box.x/radius), (int)(box.y/radius), (int)(box.z/radius));
        cellCount = grid_dims.x*grid_dims.y*grid_dims.z;

        particleDensity = particleCount / (boxWidth*boxWidth*boxWidth);
        particleCellDensity = particleCount / cellCount;

        // Caching simulation parameters
        cachedParticleCount = particleCount;
        cachedBoxWidth = boxWidth;
        cachedRadius = radius;
        cachedTime = Time.time;
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
        particleBuffer = new Buffer<Particle>(particleCount, "particleBuffer", InitParticleArray());
        particleInBuffer = new Buffer<Particle>(particleCount, "particleInBuffer", InitParticleArray());
        particleIDBuffer = new Buffer<uint>(particleCount, "particleIDs");
        cellIDBuffer = new Buffer<uint>(particleCount, "cellIDs");
        keysBuffer = new Buffer<uint>(particleCount, "keys");
        startendBuffer = new Buffer<Vector2Int>(cellCount, "startendIDs");
        cellBuffer = new Buffer<Cell>(cellCount, "cellBuffer");
        debugBuffer1 = new Buffer<Vector4>(particleCount, "debugBuffer1");
        debugBuffer2 = new Buffer<Vector4>(particleCount, "debugBuffer2");
        debugBuffer3 = new Buffer<Vector4>(particleCount, "debugBuffer3");
    }


    // Initialise Compute Kernels
    void SetupKernels() {
        // Create kernels
        particleRearrange = new Kernel(ParticleCompute, "RearrangeParticleIDs", threadGroups);
        buildStartEndIDs = new Kernel(ParticleCompute, "BuildStartEndIDs", threadGroups);
        particleUpdate = new Kernel(ParticleCompute, "ParticleUpdate", threadGroups);
        optimizedParticleUpdate = new Kernel(ParticleCompute, "OptimizedParticleUpdate", threadGroups);
        cellReset = new Kernel(ParticleCompute, "ResetCellBuffer", threadGroups);
        copyBuffer = new Kernel(ParticleCompute, "CopyParticleBuffer", threadGroups);

        // Associate buffers with kernels
        particleRearrange.SetBuffers(new List<IBuffer>{particleIDBuffer, keysBuffer});
        buildStartEndIDs.SetBuffers(new List<IBuffer>{particleIDBuffer, cellIDBuffer, startendBuffer, cellBuffer});
        particleUpdate.SetBuffers(new List<IBuffer>{particleBuffer, particleInBuffer});
        optimizedParticleUpdate.SetBuffers(new List<IBuffer>{particleBuffer, particleInBuffer, particleIDBuffer, startendBuffer, cellBuffer, cellIDBuffer, debugBuffer1, debugBuffer2});
        cellReset.SetBuffers(new List<IBuffer>{cellBuffer});
        copyBuffer.SetBuffers(new List<IBuffer>{particleBuffer, particleInBuffer});

        // Attach buffers to kernels
        particleRearrange.InitBuffers();
        buildStartEndIDs.InitBuffers();
        particleUpdate.InitBuffers();
        optimizedParticleUpdate.InitBuffers();
        cellReset.InitBuffers();
        copyBuffer.InitBuffers();
    }


    // Dispose buffers and create again
    void ResetBuffers() {
        Particle[] initArray = InitParticleArray();
        particleBuffer.Reset(particleCount, initArray);
        particleInBuffer.Reset(particleCount, initArray);
        particleIDBuffer.Reset(particleCount);
        keysBuffer.Reset(particleCount);
        startendBuffer.Reset(cellCount);
        cellIDBuffer.Reset(particleCount);
        cellBuffer.Reset(cellCount);
        debugBuffer1.Reset(particleCount);
        debugBuffer2.Reset(particleCount);
        debugBuffer3.Reset(particleCount);
    }


    // Send variable values from CPU to GPU
    void UpdateComputeShaderVariables() {
        // Update compute shader variables
        ParticleCompute.SetInt("particle_count", particleCount);
        ParticleCompute.SetFloats("box", new [] {box.x, box.y, box.z});
        ParticleCompute.SetFloat("radius", radius);
        ParticleCompute.SetFloat("speed", speed);
        ParticleCompute.SetFloat("dt", Time.deltaTime);
        ParticleCompute.SetFloat("time", Time.time);
        ParticleCompute.SetFloat("noise", noise);
        ParticleCompute.SetInt("state", (int)(Time.time*1000 % 255));
        ParticleCompute.SetInts("grid_dims", new [] {grid_dims.x, grid_dims.y, grid_dims.z});
    }


    void RecalcRadiusRange(){
        float MAX = 20f;
        float MIN = 0.5f;
        radiusRange.x = Mathf.Clamp(boxRange.y / Mathf.Pow((float)max_cell_count, 1f/3f), MIN, MAX);
        radiusRange.y = Mathf.Clamp(boxRange.y, MIN, MAX);   
    }


    void RecalcBoxRange(){
        float MAX = 100f;
        float MIN = 1f;
        boxRange.x = MIN;
        boxRange.y = Mathf.Clamp((Mathf.Pow((float)max_cell_count, 1f/3f) * radius), MIN, MAX);
    }
}