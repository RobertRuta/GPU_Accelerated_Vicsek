using UnityEngine;
using System.Collections;

public class TestSimulation_2 : MonoBehaviour {
    public int instanceCount = 100000;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private int testkernel;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer particleBuffer;
    // private ComputeBuffer updatedPosition
    private RenderTexture positionTexture;
    private ComputeBuffer argsBuffer;
    public ComputeShader testshader;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };


    struct Particle
    {
        Vector4 position;
        Vector4 velocity;
    };


    void Start() {
        testkernel = testshader.FindKernel("CSMain");
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        InitBuffers();
        // instanceMaterial.SetBuffer("positionBuffer", positionBuffer);

        Particle[] particle_array = new Particle[instanceCount];

        positionTexture = new RenderTexture(instanceCount, 1, 0, RenderTextureFormat.ARGBFloat);
        positionTexture.enableRandomWrite = true;
        positionTexture.Create();
    }

    void Update() {
        // Update starting position buffer
        if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
            InitBuffers();

        // Pad input
        if (Input.GetAxisRaw("Horizontal") != 0.0f)
            instanceCount = (int)Mathf.Clamp(instanceCount + Input.GetAxis("Horizontal") * 40000, 1.0f, 5000000.0f);

        
        // testshader.SetFloat("dt", Time.deltaTime);
        // testshader.SetBuffer(testkernel, "positions", positionBuffer);
        // testshader.SetBuffer(testkernel, "updatedPositions", updatedPositionBuffer);
        // testshader.Dispatch(testkernel, Mathf.CeilToInt((float)instanceCount/128), 1, 1);

        // testshader.SetFloat("dt", Time.deltaTime);
        // testshader.SetTexture(testkernel, "positions", positionTexture);
        testshader.Dispatch(testkernel, Mathf.CeilToInt((float)instanceCount/128), 1, 1);
        
        
        
        // Render
        instanceMaterial.SetFloat("instanceCount", (float)instanceCount);
        // instanceMaterial.SetTexture("positionTexture", positionTexture);
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }

    void OnGUI() {
        GUI.Label(new Rect(265, 25, 200, 30), "Instance Count: " + instanceCount.ToString());
        instanceCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)instanceCount, 1.0f, 5000000.0f);
    }

    void InitBuffers() {
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

        // Positions
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = new ComputeBuffer(instanceCount, 16);
        Vector4[] positions = new Vector4[instanceCount];
        for (int i = 0; i < instanceCount; i++) {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(20.0f, 100.0f);
            float height = Random.Range(-2.0f, 2.0f);
            float size = Random.Range(0.05f, 0.25f);
            positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
        }
        positionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
        testshader.SetBuffer(testkernel, "positions", positionBuffer);

        // Indirect args
        if (instanceMesh != null) {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }


    void OnDisable() {
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }
}