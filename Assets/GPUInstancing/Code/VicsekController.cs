using UnityEngine;
using BufferSorter;
using System.Runtime.InteropServices;

public class VicsekController : MonoBehaviour {

    // User defined variables at startup
    public int particleCount = 100000;
    public float radius = 5;
    public float speed = 5;
    public float noise = 1.0f;
    public Texture2D NoiseTexture;
    public float particleSize = 0.05f;
    public bool debug_toggle = false;
    public Mesh particleMesh;
    public Material particleMaterial;
    public int subMeshIndex = 0;
    public ComputeShader ParticleCompute;
    public ComputeShader sortShader;


    // Additional convenience variables
    int cachedParticleCount = -1;
    float cachedBoxWidth = -1f;
    float cachedRadius = -1f;
    int cachedSubMeshIndex = -1;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    
    // Compute shader kernel IDs
    int particleUpdateKernel;
    int optimizedParticleUpdateKernel;
    int gridUpdateKernel;

    // Dispatch group counts
    int group_count;
    
    
    // Simulation struct
    struct Particle
    {
        public Vector4 position;
        public Vector4 velocity;
    }

    struct Cell
    {
        public int is_full;
    }


    // Simulation space and grid variables
    public float box_width = 100f;
    [SerializeField]
    Vector3 box;
    [SerializeField]
    Vector3Int grid_dims;
    [SerializeField]
    int cellCount;
    

    // Third-party Sorter
    Sorter sorter;
    
    
    // Compute buffers
    private ComputeBuffer particleBuffer;
    private ComputeBuffer particleIDBuffer;
    private ComputeBuffer cellIDBuffer;
    ComputeBuffer keyBuffer;
    ComputeBuffer startendIDBuffer;
    int particleRearrangeKernel;
    int startendIDKernel;
    int startend_group_count;
    ComputeBuffer cellBuffer;

    const uint MAX_BUFFER_BYTES = 2147483648;



    void Start() {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        particleUpdateKernel = ParticleCompute.FindKernel("ParticleUpdate");
        optimizedParticleUpdateKernel = ParticleCompute.FindKernel("OptimizedParticleUpdate");
        InitiateSim();
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
        ParticleCompute.SetFloat("dt", Time.deltaTime);
        ParticleCompute.SetFloat("time", Time.time);
        ParticleCompute.SetFloat("noise", noise);
        ParticleCompute.SetFloat("particleSize", particleSize);

        // Sort keys such that cellIDBuffer is ascending
        sorter.Sort(keyBuffer, cellIDBuffer);

        // Rearrange particleIDsBuffer based on keyBuffer
        ParticleCompute.SetBuffer(particleRearrangeKernel, "particleIDs", particleIDBuffer);
        ParticleCompute.Dispatch(particleRearrangeKernel, group_count, 1, 1);
        
        // Build start end indices
        ParticleCompute.Dispatch(startendIDKernel, group_count, 1, 1);

        
        // Update Particle Positions
        // ParticleCompute.Dispatch(particleUpdateKernel, group_count, 1, 1);
        ParticleCompute.Dispatch(optimizedParticleUpdateKernel, group_count, 1, 1);

        // Render
        Graphics.DrawMeshInstancedIndirect(particleMesh, subMeshIndex, particleMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }

    void OnGUI() {
        GUI.Label(new Rect(265, 15, 200, 30), "Particle Count: " + particleCount.ToString());
        particleCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)particleCount, 1.0f, 2000000.0f);
        
        GUI.Label(new Rect(265, 45, 200, 30), "Box width: " + box_width.ToString() + "m");
        box_width = GUI.HorizontalSlider(new Rect(25, 50, 200, 30), box_width, 1f, 100f);

        GUI.Label(new Rect(265, 75, 200, 30), "Neighbour radius: " + radius.ToString() + "m");
        radius = GUI.HorizontalSlider(new Rect(25, 80, 200, 30), radius, 0.2f, 10f);
        
        GUI.Label(new Rect(265, 105, 200, 30), "Noise: " + noise.ToString() + "");
        noise = GUI.HorizontalSlider(new Rect(25, 110, 200, 30), noise, 0.0f, 1f);
        
        GUI.Label(new Rect(265, 135, 200, 30), "Particle Size: " + particleSize.ToString() + "");
        particleSize = GUI.HorizontalSlider(new Rect(25, 140, 200, 30), particleSize, 0.05f, 5f);
    }

    // Helper functions
    void Debug(string after)
    {
        if (debug_toggle){
            Particle[] particles = new Particle[particleCount];
            uint[] values = new uint[particleCount];
            uint[] particle_ids = new uint[particleCount];
            uint[] keys = new uint[particleCount];
            Vector2Int[] startend = new Vector2Int[cellCount];
            particleBuffer.GetData(particles);
            particleIDBuffer.GetData(particle_ids);
            keyBuffer.GetData(keys);
            cellIDBuffer.GetData(values);
            startendIDBuffer.GetData(startend);
            for (int i = 0; i < 10; i++)
            {
                print("After " + after + " | ParticleID["+ i + "]: " + particle_ids[i] + " | particle["+ particle_ids[i] + "]: " + particles[particle_ids[i]].position + ", " + particles[particle_ids[i]].velocity + " | keys[" + i + "]: " + keys[i] + " | grid[" + i + "]: " + values[i] + " | grid[keys[" + i + "]]: " + values[keys[i]]  + " | grid[particle_id[" + i + "]]: " + values[particle_ids[i]] + " | start_end["+ i + "]: " + startend[i]);
            }

            for (int k = 0; k < 10; k++)
            {
                int i = particleCount - 10 + k;
                int j = cellCount - 10 + k;
                print("After " + after + " | ParticleID["+ i + "]: " + particle_ids[i] + " | particle["+ particle_ids[i] + "]: " + particles[particle_ids[i]].position + ", " + particles[particle_ids[i]].velocity + " | keys[" + i + "]: " + keys[i] + " | grid[" + i + "]: " + values[i] + " | grid[keys[" + i + "]]: " + values[keys[i]]  + " | grid[particle_id[" + i + "]]: " + values[particle_ids[i]] + " | start_end["+ j + "]: " + startend[j]);
            } 

        }
    }


    void InitiateSim()
    {
        group_count = Mathf.CeilToInt((float)particleCount / 128);


        InitiateSimParams();
        InitiateBuffers();
        InitiateSorter();
        InitiateRearrange(keyBuffer, particleIDBuffer);
        InitiateStartEndIDs(startendIDBuffer, particleIDBuffer, cellIDBuffer);
        // InitiateParticleUpdate(particleBuffer, cellIDBuffer);
        InitiateOptimizedParticleUpdate(particleBuffer, cellIDBuffer, startendIDBuffer, particleIDBuffer);
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

        cachedParticleCount = particleCount;
        cachedBoxWidth = box_width;
        cachedRadius = radius;
        cachedSubMeshIndex = subMeshIndex;
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


    void InitiateParticleUpdate(ComputeBuffer particleBuffer, ComputeBuffer cellIDBuffer)
    {
        ParticleCompute.SetFloat("radius", radius);
        ParticleCompute.SetBuffer(particleUpdateKernel, "particleBuffer", particleBuffer);
        ParticleCompute.SetBuffer(particleUpdateKernel, "cellIDs", cellIDBuffer);
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
    }
}