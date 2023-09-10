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
    [Range(0f, 1f)] public float noise = 1.0f;

    //      
    public Vector2 radius_range;
    public float particleDensity, particleCellDensity;

    // Variables that can be moved elsewhere
    public ComputeShader ParticleCompute, SortShader;
    
    // Move to Visualisation Script
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
    

    // Third-party Sorter
    Sorter sorter;
    public bool optimized;

    const uint MAX_BUFFER_BYTES = 2147483648;
    int max_cell_count;

    Visualiser visualiser;

    void Start() {

        sorter = new Sorter(SortShader);

        visualiser = GetComponent<Visualiser>();


        Vector3Int threadGroups = new Vector3Int(groupCount, 1, 1);

        SetupBuffers();
        SetupKernels();
    }

    void Update() {

        if (cachedParticleCount != particleCount || cachedSubMeshIndex != subMeshIndex || cachedBoxWidth != boxWidth || cachedRadius != radius) {
            // Set box vector
            box = new Vector3(boxWidth, boxWidth, boxWidth);
            // Recalculate box vector - box must be the same size as the grid boundaries
            box = new Vector3((int)(box.x/radius) * radius, (int)(box.y/radius) * radius, (int)(box.z/radius) * radius);
            // Set box vector in compute shader
            ParticleCompute.SetFloats("box", new [] {box.x, box.y, box.z});
            
            // Calculate grid dimensions
            grid_dims = new Vector3Int((int)(box.x/radius), (int)(box.y/radius), (int)(box.z/radius));
            // Calculate cell count
            cellCount = grid_dims.x*grid_dims.y*grid_dims.z;
            
            ResetBuffers();

            // Reset Kernels
            if (sorter != null)
                sorter.Dispose();
            sorter = new Sorter(SortShader);
            SetupKernels();
        }


        if (optimized) {
            sorter.Sort(keysBuffer.buffer, cellIDBuffer.buffer);
            particleRearrange.Run();
            buildStartEndIDs.Run();
            optimizedParticleUpdate.Run();
        }

        else {
            particleUpdate.Run();
        }

        visualiser.RenderParticles(particleBuffer.buffer);
    }

    void SetupBuffers() {

        Particle[] particleArray = new Particle[particleCount];
        for (int i = 0; i < particleCount; i++) {
            particleArray[i].position = new Vector4(Random.Range(0, boxWidth), Random.Range(0, boxWidth), Random.Range(0, boxWidth), 10f);
            particleArray[i].velocity = Random.onUnitSphere * speed;
        }

        particleBuffer = new Buffer<Particle>(particleCount, "particleBuffer", particleArray);
        particleIDBuffer = new Buffer<uint>(particleCount, "particleIDs");
        cellIDBuffer = new Buffer<uint>(particleCount, "cellIDs");
        keysBuffer = new Buffer<uint>(particleCount, "keys");
        startendBuffer = new Buffer<Vector2Int>(cellCount, "startendIDs");
        cellBuffer = new Buffer<Cell>(cellCount, "cellBuffer");
        debug1Buffer = new Buffer<Vector4>(particleCount, "debugBuffer");


    }

    void SetupKernels() {
        // Sort keys such that cellIDBuffer is ascending
        particleRearrange.SetBuffers(new List<IBuffer>{particleIDBuffer, keysBuffer});
        buildStartEndIDs.SetBuffers(new List<IBuffer>{particleIDBuffer, cellIDBuffer, startendBuffer, cellBuffer});
        optimizedParticleUpdate.SetBuffers(new List<IBuffer>{particleBuffer, particleIDBuffer, startendBuffer, cellBuffer, cellIDBuffer, debug1Buffer, debug2Buffer});
        cellReset.SetBuffers(new List<IBuffer>{cellBuffer});
    }

    void ResetBuffers() {
        particleBuffer.Reset(particleCount);
        particleIDBuffer.Reset(particleCount);
        keysBuffer.Reset(particleCount);
        startendBuffer.Reset(particleCount);
        cellIDBuffer.Reset(cellCount);
        cellBuffer.Reset(cellCount);
    }

    void OnDisable() {
        particleBuffer.Dispose();
        particleIDBuffer.Dispose();
        cellIDBuffer.Dispose();
        startendBuffer.Dispose();
        cellBuffer.Dispose();
        keysBuffer.Dispose();
    }
}

//     void Update() {
//         // Update starting position buffer
//         if (cachedParticleCount != particleCount || cachedSubMeshIndex != subMeshIndex || cachedBoxWidth != boxWidth || cachedRadius != radius)
//             InitSim();

//         // Pad input
//         if (Input.GetKey(KeyCode.RightArrow))
//             particleCount += 1000;
//         if (Input.GetKey(KeyCode.LeftArrow))
//             particleCount -= 1000;
        
//         // Update compute shader variables
//         ParticleCompute.SetFloat("speed", speed);
//         ParticleCompute.SetFloat("dt", 1f/60f);
//         ParticleCompute.SetFloat("time", Time.time);
//         ParticleCompute.SetFloat("noise", noise);
//         ParticleCompute.SetFloat("particleSize", particleSize);
//         ParticleCompute.SetInt("state", (int)(Time.time*1000 % 255));
//         ParticleCompute.SetInt("frameCounter", frameCounter);

//         if (optimized)
//         {
//             // Sort keys such that cellIDBuffer is ascending
//             sorter.Sort(keyBuffer, cellIDBuffer);

//             // Rearrange particleIDsBuffer based on keyBuffer
//             ParticleCompute.SetBuffer(particleRearrangeKernel, "particleIDs", particleIDBuffer);
//             ParticleCompute.Dispatch(particleRearrangeKernel, groupCount, 1, 1);
            
//             // Build start end indices
//             ParticleCompute.Dispatch(startendIDKernel, groupCount, 1, 1);

//             // Update particle positions
//             ParticleCompute.SetBuffer(optimizedparticleUpdate, "debugBuffer", debugBuffer);
//             ParticleCompute.SetBuffer(optimizedparticleUpdate, "debugBuffer2", debugBuffer2);
//             ParticleCompute.SetBuffer(optimizedparticleUpdate, "debugBuffer3", debugBuffer3);
//             ParticleCompute.Dispatch(optimizedparticleUpdate, groupCount, 1, 1);

//             // Reset cell buffer
//             ParticleCompute.Dispatch(cellResetKernel, groupCount, 1, 1);
//         }

//         else
//         {
//             ParticleCompute.Dispatch(particleUpdate, groupCount, 1, 1);
//         }

//         Graphics.DrawMeshInstancedIndirect(particleMesh, subMeshIndex, particleMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
//         frameCounter++;
//     }


//     // Helper functions


//     void RecalcRadiusRange(){
//         float MAX = 20f;
//         float MIN = 0.1f;
//         radius_range.x = Mathf.Clamp(box_range.y / Mathf.Pow((float)max_cell_count, 1f/3f), MIN, MAX);
//         radius_range.y = Mathf.Clamp(box_range.y, MIN, MAX);   
//     }

//     void RecalcBoxRange(){
//         float MAX = 100f;
//         float MIN = 1f;
//         box_range.x = MIN;
//         box_range.y = Mathf.Clamp((Mathf.Pow((float)max_cell_count, 1f/3f) * radius), MIN, MAX);
//     }


//     void InitSim()
//     {
//         groupCount = Mathf.CeilToInt((float)particleCount / 128);


//         InitSimParams();
//         InitBuffers();
//         InitSorter();
//         InitRearrange(keyBuffer, particleIDBuffer);
//         InitStartEndIDs(startendIDBuffer, particleIDBuffer, cellIDBuffer);
//         InitParticleUpdate(particleBuffer);
//         InitOptimizedParticleUpdate(particleBuffer, cellIDBuffer, startendIDBuffer, particleIDBuffer);
//         InitCellReset(cellBuffer);
//         InitArgs();


//         particleMaterial.SetBuffer("particleBuffer", particleBuffer);
//     }

//     void InitBuffers()
//     {
//         // Cleaning up existing buffers
//         if (particleBuffer != null)
//             particleBuffer.Release();
//         if (cellIDBuffer != null)
//             cellIDBuffer.Release();
//         if (keyBuffer != null)
//             keyBuffer.Release();
//         if (particleIDBuffer != null)
//             particleIDBuffer.Release();
//         if (startendIDBuffer != null)
//             startendIDBuffer.Release();
//         if (cellBuffer != null)
//             cellBuffer.Release();
//         if (debugBuffer != null)
//             debugBuffer.Release();
//         if (debugBuffer2 != null)
//             debugBuffer2.Release();
//         if (debugBuffer3 != null)
//             debugBuffer3.Release();

//         // Recreating buffers
//         debugBuffer = new ComputeBuffer(particleCount, 4*4);
//         debugBuffer2 = new ComputeBuffer(particleCount, 4*4);
//         debugBuffer3 = new ComputeBuffer(particleCount, 4*4);
//         cellBuffer = new ComputeBuffer(cellCount, Marshal.SizeOf(typeof(Cell)));
//         startendIDBuffer = new ComputeBuffer(cellCount, 2*Marshal.SizeOf(typeof(uint)));
//         particleIDBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)));
//         keyBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)));
//         particleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
//         cellIDBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(uint)));

//         // Initialise buffers of particleCount elements
//         uint[] initArray = new uint[particleCount];
//         Particle[] particleArray = new Particle[particleCount];
//         Vector4 [] debugArray = new Vector4[particleCount];
//         for (uint i = 0; i<particleCount; i++)
//         {
//             // Key buffer initialisation array
//             initArray[i] = i;

//             // Particle buffer initialisation array
//             particleArray[i].position = new Vector4(Random.Range(0f, box.x), Random.Range(0f, box.y), Random.Range(0f, box.z), particleSize);
//             Vector3 vel = Random.onUnitSphere;
//             particleArray[i].velocity = new Vector4(vel.x, vel.y, vel.z, particleSize);
            
//             // Debug array
//             debugArray[i] = Vector4.zero;
//         }

//         // Initalise startendID buffer
//         Vector2Int[] startendIDArray = new Vector2Int[cellCount];
//         Cell[] cellArray = new Cell[cellCount];
//         for (uint i = 0; i<(uint)cellCount; i++)
//         {
//             startendIDArray[i] = new Vector2Int((int)i+1, (int)i);
//             cellArray[i].is_full = 0;
//         }

//         // Initalising buffer values
//         keyBuffer.SetData(initArray);
//         particleIDBuffer.SetData(initArray);
//         cellIDBuffer.SetData(initArray);
//         startendIDBuffer.SetData(startendIDArray);
//         cellBuffer.SetData(cellArray);
//         particleBuffer.SetData(particleArray);
//         debugBuffer.SetData(debugArray);
//         debugBuffer2.SetData(debugArray);
//         debugBuffer3.SetData(debugArray);

//     }

//     // Init Emmet's sorter
//     void InitSorter()
//     {
//         if (sorter != null)
//             sorter.Dispose();
//         sorter = new Sorter(SortShader);
//     }
    
//     void InitSimParams()
//     {
//         // Set particle count
//         if (particleCount != cachedParticleCount)
//             particleCount = Mathf.NextPowerOfTwo(particleCount) >> 1;
//         ParticleCompute.SetInt("particle_count", particleCount);

//         // Clamp radius and boxWidth
//         if (boxWidth != cachedBoxWidth)
//             RecalcRadiusRange();
//         if (radius != cachedRadius)
//             RecalcBoxRange();
//         radius = Mathf.Clamp(radius, radius_range.x, radius_range.y);
//         boxWidth = Mathf.Clamp(boxWidth, box_range.x, box_range.y);

//         // Set box vector
//         box = new Vector3(boxWidth, boxWidth, boxWidth);
//         // Recalculate box vector - box must be the same size as the grid boundaries
//         box = new Vector3((int)(box.x/radius) * radius, (int)(box.y/radius) * radius, (int)(box.z/radius) * radius);
//         // Set box vector in compute shader
//         ParticleCompute.SetFloats("box", new [] {box.x, box.y, box.z});

//         // Calculate grid dimensions
//         grid_dims = new Vector3Int((int)(box.x/radius), (int)(box.y/radius), (int)(box.z/radius));
//         // Set grid dimensions in compute shader
//         ParticleCompute.SetInts("grid_dims", new [] {grid_dims.x, grid_dims.y, grid_dims.z});
//         // Calculate cell count
//         cellCount = grid_dims.x*grid_dims.y*grid_dims.z;
        
//         // Some informational metrics
//         particleDensity = particleCount / (boxWidth*boxWidth*boxWidth);
//         particleCellDensity = particleCount / cellCount;

//         // Caching simulation parameters
//         cachedParticleCount = particleCount;
//         cachedBoxWidth = boxWidth;
//         cachedRadius = radius;
//         cachedSubMeshIndex = subMeshIndex;

//         // Seed the PRNG
//         int seed0 = (int)Random.Range(0, 0xffffffff);
//         int seed1 = (int)Random.Range(0, 0xffffffff);
//         int seed2 = (int)Random.Range(0, 0xffffffff);
//         int seed3 = (int)Random.Range(0, 0xffffffff);
//         ParticleCompute.SetInts("seeds", new [] {seed0, seed1, seed2, seed3});
//     }


//     void InitRearrange(ComputeBuffer particleIDBuffer, ComputeBuffer keysBuffer)
//     {
//         particleRearrangeKernel = ParticleCompute.FindKernel("RearrangeParticleIDs");
//         ParticleCompute.SetBuffer(particleRearrangeKernel, "particleIDs", particleIDBuffer);
//         ParticleCompute.SetBuffer(particleRearrangeKernel, "keys", keyBuffer);
//     }


//     void InitStartEndIDs(ComputeBuffer startendIDBuffer, ComputeBuffer particleIDBuffer, ComputeBuffer cellIDBuffer)
//     {
//         startendIDKernel = ParticleCompute.FindKernel("BuildStartEndIDs");
//         ParticleCompute.SetBuffer(startendIDKernel, "startendIDs", startendIDBuffer);
//         ParticleCompute.SetBuffer(startendIDKernel, "particleIDs", particleIDBuffer);
//         ParticleCompute.SetBuffer(startendIDKernel, "cellIDs", cellIDBuffer);
//         ParticleCompute.SetBuffer(startendIDKernel, "cellBuffer", cellBuffer);
//     }


//     void InitParticleUpdate(ComputeBuffer particleBuffer)
//     {
//         ParticleCompute.SetFloat("radius", radius);
//         ParticleCompute.SetBuffer(particleUpdate, "particleBuffer", particleBuffer);
//     }


//     void InitOptimizedParticleUpdate(ComputeBuffer particleBuffer, ComputeBuffer cellIDBuffer, ComputeBuffer startendIDBuffer, ComputeBuffer particleIDBuffer)
//     {
//         ParticleCompute.SetFloat("radius", radius);
//         ParticleCompute.SetBuffer(optimizedparticleUpdate, "particleBuffer", particleBuffer);
//         ParticleCompute.SetBuffer(optimizedparticleUpdate, "startendIDs", startendIDBuffer);
//         ParticleCompute.SetBuffer(optimizedparticleUpdate, "particleIDs", particleIDBuffer);
//         ParticleCompute.SetBuffer(optimizedparticleUpdate, "cellIDs", cellIDBuffer);
//         ParticleCompute.SetBuffer(optimizedparticleUpdate, "cellBuffer", cellBuffer);
//         ParticleCompute.SetTexture(optimizedparticleUpdate, "NoiseTexture", NoiseTexture);
//     }


//     void InitCellReset(ComputeBuffer cellBuffer)
//     {
//         ParticleCompute.SetBuffer(cellResetKernel, "cellBuffer", cellBuffer);
//     }

//     void InitArgs()
//     {
//         // Indirect args
//         if (particleMesh != null) {
//             subMeshIndex = Mathf.Clamp(subMeshIndex, 0, particleMesh.subMeshCount - 1);
//             args[0] = (uint)particleMesh.GetIndexCount(subMeshIndex);
//             args[1] = (uint)particleCount;
//             args[2] = (uint)particleMesh.GetIndexStart(subMeshIndex);
//             args[3] = (uint)particleMesh.GetBaseVertex(subMeshIndex);
//         }
//         else
//         {
//             args[0] = args[1] = args[2] = args[3] = 0;
//         }
//         argsBuffer.SetData(args);
//     }

//     // Is called on simulation quit - when Sim gameobject is inactive
//     void OnDisable() {
//         if (particleBuffer != null)
//             particleBuffer.Release();
//         particleBuffer = null;

//         if (keyBuffer != null)
//             keyBuffer.Release();
//         keyBuffer = null;

//         if (cellIDBuffer != null)
//             cellIDBuffer.Release();
//         cellIDBuffer = null;

//         if (particleIDBuffer != null)
//             particleIDBuffer.Release();
//         particleIDBuffer = null;
        
//         if (sorter != null)
//             sorter.Dispose();

//         if (startendIDBuffer != null)
//             startendIDBuffer.Release();
//         startendIDBuffer = null;

//         if (cellBuffer != null)
//             cellBuffer.Release();
//         cellBuffer = null;

//         if (argsBuffer != null)
//             argsBuffer.Release();
//         argsBuffer = null;

//         if (debugBuffer != null)
//             debugBuffer.Release();
//         debugBuffer = null;
        
//         if (debugBuffer2 != null)
//             debugBuffer2.Release();
//         debugBuffer2 = null;
        
//         if (debugBuffer3 != null)
//             debugBuffer3.Release();
//         debugBuffer3 = null;
//     }
// }