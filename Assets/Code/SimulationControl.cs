using UnityEngine;
using BufferSorter;
using System.Runtime.InteropServices;
using vicsek;

public class SimulationControl : MonoBehaviour {

    // User defined variables at startup
    public int particleCount = 100000;
    public float radius = 5;
    public float speed = 5;
    public float noise = 1.0f;
    public float particleSize = 0.05f;
    public float perturbation_frequency = 0.2f;
    float elapsed = 0;
    public bool debug_toggle = false;
    [SerializeField]
    public Vector2 radius_range;
    public float particleDensity, particleCellDensity;

    // Variables that can be moved elsewhere
    public ComputeShader ParticleCompute;
    public ComputeShader sortShader;
    public Texture2D NoiseTexture;
    public Material particleMaterial;
    public Mesh particleMesh;
    public int subMeshIndex = 0;


    // Additional convenience variables
    int cachedParticleCount = -1;
    float cachedBoxWidth = -1f;
    float cachedRadius = -1f;
    int cachedSubMeshIndex = -1;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    
    // Compute shader kernel IDs
    int particleRearrangeKernel;
    int startendIDKernel;
    int particleUpdateKernel;
    int optimizedParticleUpdateKernel;
    int cellResetKernel;

    // Dispatch group counts
    int group_count;
    
    
    // Simulation struct


    // Simulation space and grid variables
    public float box_width = 100f;
    [SerializeField]
    Vector2 box_range;
    [SerializeField]
    Vector3 box;
    [SerializeField]
    Vector3Int grid_dims;
    [SerializeField]
    public int cellCount;
    int frameCounter = 0;
    

    // Third-party Sorter
    Sorter sorter;
    
    
    // Compute buffers
    public ComputeBuffer particleBuffer;
    public ComputeBuffer particleIDBuffer;
    public ComputeBuffer cellIDBuffer;
    public ComputeBuffer debugBuffer, debugBuffer2, debugBuffer3;
    public ComputeBuffer keyBuffer;
    public ComputeBuffer startendIDBuffer;
    int startend_group_count;
    ComputeBuffer cellBuffer;
    public bool optimized;

    const uint MAX_BUFFER_BYTES = 2147483648;
    int max_cell_count;

    DebugControl debugger;
    void Awake()
    {
        debugger = GetComponent<DebugControl>();
    }

    void Start() {
        max_cell_count = (int)(MAX_BUFFER_BYTES / 8);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        particleUpdateKernel = ParticleCompute.FindKernel("ParticleUpdate");
        optimizedParticleUpdateKernel = ParticleCompute.FindKernel("OptimizedParticleUpdate");
        cellResetKernel = ParticleCompute.FindKernel("ResetCellBuffer");
        RecalcBoxRange();
        RecalcRadiusRange();
        InitiateSim();


        debugBuffer = new ComputeBuffer(particleCount, 4*4);
        debugBuffer2 = new ComputeBuffer(particleCount, 4*4);
        debugBuffer3 = new ComputeBuffer(particleCount, 4*4);
        Vector4 [] debugArray = new Vector4[particleCount];
        for (int i = 0; i < 100; i++)
        {
            debugArray[i] = Vector4.zero;
        }
        debugBuffer.SetData(debugArray);
        debugBuffer2.SetData(debugArray);
        debugBuffer3.SetData(debugArray);
    }



    void Update() {
        // Update starting position buffer
        if (cachedParticleCount != particleCount || cachedSubMeshIndex != subMeshIndex || cachedBoxWidth != box_width || cachedRadius != radius)
            InitiateSim();

        // Pad input
        if (Input.GetKey(KeyCode.RightArrow))
            particleCount += 1000;
        if (Input.GetKey(KeyCode.LeftArrow))
            particleCount -= 1000;

        
        // Update compute shader variables
        ParticleCompute.SetFloat("speed", speed);
        ParticleCompute.SetFloat("dt", 1f/60f);
        ParticleCompute.SetFloat("time", Time.time);
        ParticleCompute.SetFloat("noise", noise);
        ParticleCompute.SetFloat("particleSize", particleSize);
        ParticleCompute.SetInt("state", (int)(Time.time*1000 % 255));
        ParticleCompute.SetInt("frameCounter", frameCounter);


        if (optimized)
        {
            // Sort keys such that cellIDBuffer is ascending
            sorter.Sort(keyBuffer, cellIDBuffer);

            // Rearrange particleIDsBuffer based on keyBuffer
            ParticleCompute.SetBuffer(particleRearrangeKernel, "particleIDs", particleIDBuffer);
            ParticleCompute.Dispatch(particleRearrangeKernel, group_count, 1, 1);
            
            // Build start end indices
            ParticleCompute.Dispatch(startendIDKernel, group_count, 1, 1);

            // Update particle positions
            ParticleCompute.SetBuffer(optimizedParticleUpdateKernel, "debugBuffer", debugBuffer);
            ParticleCompute.SetBuffer(optimizedParticleUpdateKernel, "debugBuffer2", debugBuffer2);
            ParticleCompute.SetBuffer(optimizedParticleUpdateKernel, "debugBuffer3", debugBuffer3);
            ParticleCompute.Dispatch(optimizedParticleUpdateKernel, group_count, 1, 1);

            // Reset cell buffer
            ParticleCompute.Dispatch(cellResetKernel, group_count, 1, 1);
        }

        else
        {
            ParticleCompute.Dispatch(particleUpdateKernel, group_count, 1, 1);
        }


        Graphics.DrawMeshInstancedIndirect(particleMesh, subMeshIndex, particleMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
        frameCounter++;
    }

    // Helper functions


    void RecalcRadiusRange(){
        float MAX = 20f;
        float MIN = 0.1f;
        radius_range.x = Mathf.Clamp(box_range.y / Mathf.Pow((float)max_cell_count, 1f/3f), MIN, MAX);
        radius_range.y = Mathf.Clamp(box_range.y, MIN, MAX);   
    }

    void RecalcBoxRange(){
        float MAX = 100f;
        float MIN = 1f;
        box_range.x = MIN;
        box_range.y = Mathf.Clamp((Mathf.Pow((float)max_cell_count, 1f/3f) * radius), MIN, MAX);
    }


    void InitiateSim()
    {
        group_count = Mathf.CeilToInt((float)particleCount / 128);


        InitiateSimParams();
        InitiateBuffers();
        InitiateSorter();
        InitiateRearrange(keyBuffer, particleIDBuffer);
        InitiateStartEndIDs(startendIDBuffer, particleIDBuffer, cellIDBuffer);
        InitiateParticleUpdate(particleBuffer);
        InitiateOptimizedParticleUpdate(particleBuffer, cellIDBuffer, startendIDBuffer, particleIDBuffer);
        InitiateCellReset(cellBuffer);
        InitiateArgs();


        particleMaterial.SetBuffer("particleBuffer", particleBuffer);
    }

    void InitiateBuffers()
    {
        if (particleBuffer != null)
            particleBuffer.Release();
        if (cellIDBuffer != null)
            cellIDBuffer.Release();
        if (keyBuffer != null)
            keyBuffer.Release();
        if (particleIDBuffer != null)
            particleIDBuffer.Release();
        if (startendIDBuffer != null)
            startendIDBuffer.Release();
        if (cellBuffer != null)
            cellBuffer.Release();
        cellBuffer = new ComputeBuffer(cellCount, Marshal.SizeOf(typeof(Cell)));
        startendIDBuffer = new ComputeBuffer(cellCount, 2*Marshal.SizeOf(typeof(uint)));
        particleIDBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)));
        keyBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)));
        particleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
        cellIDBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)));

        // Initialise buffers of particleCount elements
        uint[] initArray = new uint[particleCount];
        Particle[] particleArray = new Particle[particleCount];
        for (uint i = 0; i<particleCount; i++)
        {
            // Key buffer initialisation array
            initArray[i] = i;

            // Particle buffer initialisation array
            particleArray[i].position = new Vector4(Random.Range(0f, box.x), Random.Range(0f, box.y), Random.Range(0f, box.z), particleSize);
            Vector3 vel = Random.onUnitSphere;
            particleArray[i].velocity = new Vector4(vel.x, vel.y, vel.z, particleSize);
        }

        // Initalise startendID buffer
        Vector2Int[] startendIDArray = new Vector2Int[cellCount];
        Cell[] cellArray = new Cell[cellCount];
        for (uint i = 0; i<(uint)cellCount; i++)
        {
            startendIDArray[i] = new Vector2Int((int)i+1, (int)i);
            cellArray[i].is_full = 0;
        }

        // Initalise data in buffers
        keyBuffer.SetData(initArray);
        particleIDBuffer.SetData(initArray);
        cellIDBuffer.SetData(initArray);
        startendIDBuffer.SetData(startendIDArray);
        cellBuffer.SetData(cellArray);
        particleBuffer.SetData(particleArray);

    }

    // Initiate Emmet's sorter
    void InitiateSorter()
    {
        if (sorter != null)
            sorter.Dispose();
        sorter = new Sorter(sortShader);
    }
    
    void InitiateSimParams()
    {
        // Set particle count
        if (particleCount != cachedParticleCount)
            particleCount = Mathf.NextPowerOfTwo(particleCount) >> 1;
        ParticleCompute.SetInt("particle_count", particleCount);

        // Clamp radius and box_width
        if (box_width != cachedBoxWidth)
            RecalcRadiusRange();
        if (radius != cachedRadius)
            RecalcBoxRange();
        radius = Mathf.Clamp(radius, radius_range.x, radius_range.y);
        box_width = Mathf.Clamp(box_width, box_range.x, box_range.y);

        // Set box vector
        box = new Vector3(box_width, box_width, box_width);
        // Recalculate box vector - box must be the same size as the grid boundaries
        box = new Vector3((int)(box.x/radius) * radius, (int)(box.y/radius) * radius, (int)(box.z/radius) * radius);
        // Set box vector in compute shader
        ParticleCompute.SetFloats("box", new [] {box.x, box.y, box.z});

        // Calculate grid dimensions
        grid_dims = new Vector3Int((int)(box.x/radius), (int)(box.y/radius), (int)(box.z/radius));
        // Set grid dimensions in compute shader
        ParticleCompute.SetInts("grid_dims", new [] {grid_dims.x, grid_dims.y, grid_dims.z});
        // Calculate cell count
        cellCount = grid_dims.x*grid_dims.y*grid_dims.z;
        
        // Some informational metrics
        particleDensity = particleCount / (box_width*box_width*box_width);
        particleCellDensity = particleCount / cellCount;

        // Caching simulation parameters
        cachedParticleCount = particleCount;
        cachedBoxWidth = box_width;
        cachedRadius = radius;
        cachedSubMeshIndex = subMeshIndex;

        // Seed the PRNG
        int seed0 = (int)Random.Range(0, 0xffffffff);
        int seed1 = (int)Random.Range(0, 0xffffffff);
        int seed2 = (int)Random.Range(0, 0xffffffff);
        int seed3 = (int)Random.Range(0, 0xffffffff);
        ParticleCompute.SetInts("seeds", new [] {seed0, seed1, seed2, seed3});
    }


    void InitiateRearrange(ComputeBuffer particleIDBuffer, ComputeBuffer keysBuffer)
    {
        particleRearrangeKernel = ParticleCompute.FindKernel("RearrangeParticleIDs");
        ParticleCompute.SetBuffer(particleRearrangeKernel, "particleIDs", particleIDBuffer);
        ParticleCompute.SetBuffer(particleRearrangeKernel, "keys", keyBuffer);
    }


    void InitiateStartEndIDs(ComputeBuffer startendIDBuffer, ComputeBuffer particleIDBuffer, ComputeBuffer cellIDBuffer)
    {

        startendIDKernel = ParticleCompute.FindKernel("BuildStartEndIDs");
        ParticleCompute.SetBuffer(startendIDKernel, "startendIDs", startendIDBuffer);
        ParticleCompute.SetBuffer(startendIDKernel, "particleIDs", particleIDBuffer);
        ParticleCompute.SetBuffer(startendIDKernel, "cellIDs", cellIDBuffer);
        ParticleCompute.SetBuffer(startendIDKernel, "cellBuffer", cellBuffer);
    }


    void InitiateParticleUpdate(ComputeBuffer particleBuffer)
    {
        ParticleCompute.SetFloat("radius", radius);
        ParticleCompute.SetBuffer(particleUpdateKernel, "particleBuffer", particleBuffer);
    }


    void InitiateOptimizedParticleUpdate(ComputeBuffer particleBuffer, ComputeBuffer cellIDBuffer, ComputeBuffer startendIDBuffer, ComputeBuffer particleIDBuffer)
    {
        ParticleCompute.SetFloat("radius", radius);
        ParticleCompute.SetBuffer(optimizedParticleUpdateKernel, "particleBuffer", particleBuffer);
        ParticleCompute.SetBuffer(optimizedParticleUpdateKernel, "startendIDs", startendIDBuffer);
        ParticleCompute.SetBuffer(optimizedParticleUpdateKernel, "particleIDs", particleIDBuffer);
        ParticleCompute.SetBuffer(optimizedParticleUpdateKernel, "cellIDs", cellIDBuffer);
        ParticleCompute.SetBuffer(optimizedParticleUpdateKernel, "cellBuffer", cellBuffer);
        ParticleCompute.SetTexture(optimizedParticleUpdateKernel, "NoiseTexture", NoiseTexture);
    }


    void InitiateCellReset(ComputeBuffer cellBuffer)
    {
        ParticleCompute.SetBuffer(cellResetKernel, "cellBuffer", cellBuffer);
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

    void OnDisable() {
        if (particleBuffer != null)
            particleBuffer.Release();
        particleBuffer = null;

        if (keyBuffer != null)
            keyBuffer.Release();
        keyBuffer = null;

        if (cellIDBuffer != null)
            cellIDBuffer.Release();
        cellIDBuffer = null;

        if (particleIDBuffer != null)
            particleIDBuffer.Release();
        particleIDBuffer = null;
        
        if (sorter != null)
            sorter.Dispose();

        if (startendIDBuffer != null)
            startendIDBuffer.Release();
        startendIDBuffer = null;

        if (cellBuffer != null)
            cellBuffer.Release();
        cellBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;

        if (debugBuffer != null)
            debugBuffer.Release();
        debugBuffer = null;
        
        if (debugBuffer2 != null)
            debugBuffer2.Release();
        debugBuffer2 = null;
        
        if (debugBuffer3 != null)
            debugBuffer3.Release();
        debugBuffer3 = null;
    }
}