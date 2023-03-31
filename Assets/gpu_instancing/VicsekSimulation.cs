using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class VicsekSimulation : MonoBehaviour
{
    public ComputeShader AgentComputeShader;
    ComputeBuffer agentBuffer;
    Vector3Int thread_group_sizes = new Vector3Int(128, 1, 1);
    int agentUpdateKernel;
    public int agent_count = 10000;
    public float box_width = 10.0f;

    public Mesh instanceMesh;
    public Material instanceMaterial;
    
    struct Agent
    {
        public Vector4 position;
        public Vector4 velocity;
    };


    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    void Start()
    {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        // 
        agentUpdateKernel = AgentComputeShader.FindKernel("AgentUpdate");
        
        agentBuffer = new ComputeBuffer(agent_count, Marshal.SizeOf(typeof(Agent)));
        Agent[] agents_array = new Agent[agent_count];
        for (int i=0; i<agent_count; i++)
        {
            agents_array[i].position = new Vector4(Random.Range(0, box_width), Random.Range(0, box_width), Random.Range(0, box_width), 0.0f);
            Vector3 rand_velocity = Random.onUnitSphere;
            agents_array[i].velocity = new Vector4(rand_velocity.x, rand_velocity.y, rand_velocity.z, 0.0f);
        }

        agentBuffer.SetData(agents_array);
    }


    // void Update()
    // {
    //     AgentComputeShader.SetFloat("dt", Time.deltaTime);

    //     AgentComputeShader.SetBuffer(agentUpdateKernel, "agents", agentBuffer);
    //     AgentComputeShader.Dispatch(agentUpdateKernel, thread_group_sizes.x, thread_group_sizes.y, thread_group_sizes.z);

    //     int subMeshIndex = 0;
    //     args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
    //     args[1] = (uint)agent_count;
    //     args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
    //     args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);

    //     argsBuffer.SetData(args);

    //     Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    // }

    void Update()
    {
        AgentComputeShader.SetFloat("dt", Time.deltaTime);

        AgentComputeShader.SetBuffer(agentUpdateKernel, "agents", agentBuffer);
        AgentComputeShader.Dispatch(agentUpdateKernel, thread_group_sizes.x, thread_group_sizes.y, thread_group_sizes.z);

        int subMeshIndex = 0;
        args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
        args[1] = (uint)agent_count;
        args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
        args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);

        argsBuffer.SetData(args);

        // Pass the agent positions to the instanced shader
        instanceMaterial.SetBuffer("_AgentPositions", agentBuffer);

        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
        // Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial
    }
}