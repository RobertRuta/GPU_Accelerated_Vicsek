using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class VicsekSimulation : MonoBehaviour
{
    ComputeShader AgentComputeShader;
    ComputeBuffer agentBuffer;
    
    struct Agent
    {
        public Vector4 position;
        public Vector4 velocity;
    };

    
    void Start()
    {
        //
        int agentUpdateKernel = AgentComputeShader.FindKernel("AgentUpdate");
        
        ComputeBuffer agentBuffer = new ComputeBuffer(agent_count, Marshal.SizeOf(typeof(Agent)));
        Agent[] agents_array = new Agent[agent_count];
        for (int i=0; i<agent_count; i++)
        {
            agents_array[i].position = new Vector4(Random.Range(0, box_width), Random.Range(0, box_width), Random.Range(0, box_width), 0.0f);
            Vector3 rand_velocity = Random.onUnitSphere();
            agents_array[i].velocity = new Vector4(rand_velocity.x, rand_velocity.y, rand_velocity.z, 0.0f);
        }

        agentBuffer.SetData("agents", agents_array);
    }


    void Update()
    {
        int thread_group_sizes = new Vector3Int(128, 0, 0);
        AgentComputeShader.SetBuffer(agentUpdateKernel, "agents", agentBuffer);
        AgentComputeShader.Dispatch(agentUpdateKernel, thread_group_sizes.x, thread_group_sizes.y, thread_group_sizes.z);


    }
}