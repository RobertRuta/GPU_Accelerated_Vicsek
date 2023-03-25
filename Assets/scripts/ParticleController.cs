using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    public float particle_density = 10;
    [Range(0.01f, 10.0f)]
    public float radius = 1.0f;
    public float box_width;
    [Range(0.01f, 10.0f)]
    public float noise = 1.0f;
    [Range(0.0f, 200.0f)]
    public float speed = 4.0f;

    [SerializeField]
    private float particle_cell_density;
    [SerializeField]
    private int num_particles;
    [SerializeField]
    private Vector3Int grid_dims;
    [SerializeField]
    private Vector3 box;


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct Particle
    {
        public Vector4 position;
        public Vector4 velocity;
    };
    int c_particleStride = Marshal.SizeOf(typeof(Particle));
    const int c_quadStride = 12;


    public ComputeShader ParticleCalcultion;
    public Material ParticleMaterial;
    public Texture2D NoiseTexture;
    private const int c_groupSize = 128;
    private int m_updateParticleKernel;
    private int m_updateGridKernel;
    ComputeBuffer m_particlesBuffer;
    ComputeBuffer m_particleIDBuffer;
    ComputeBuffer m_gridIDBuffer;
    ComputeBuffer m_quadPoints;


    // Start is called before the first frame update
    void Start()
    {
        // Initiate simulation variables

        float cell_width = radius;
        int num_cells_width = (int)(box_width / cell_width);
        box_width = cell_width * num_cells_width;
        grid_dims = num_cells_width * Vector3Int.one;
        box = box_width * Vector3.one;
        
        ParticleCalcultion.SetFloat("radius", radius);
        ParticleCalcultion.SetFloat("speed", speed);
        ParticleCalcultion.SetFloats("box", new [] {box.x, box.y, box.z});
        ParticleCalcultion.SetInts("grid_dims", new [] {grid_dims.x, grid_dims.y, grid_dims.z});

        
        int num_cells = num_cells_width*num_cells_width*num_cells_width;
        particle_cell_density = Mathf.FloorToInt((float)num_particles / num_cells);
        float grid_width = grid_dims.x;
        num_particles = Mathf.FloorToInt(box.x*box.y*box.z*particle_density);
        ParticleCalcultion.SetInt("num_particles", num_particles);
        


        // Initialise and locate compute shader kernels
        m_updateParticleKernel = ParticleCalcultion.FindKernel("UpdateParticles");
        m_updateParticleKernel = ParticleCalcultion.FindKernel("UpdateGrid");


        // Initiate compute buffers
        m_particlesBuffer = new ComputeBuffer(num_particles, c_particleStride);
        m_particleIDBuffer = new ComputeBuffer(num_particles, sizeof(uint));
        m_gridIDBuffer = new ComputeBuffer(num_particles, sizeof(uint));


        // Initiate contents of compute buffers
        Particle[] particleArray = new Particle[num_particles];
        uint[] pIDArray = new uint[num_particles];
        uint[] gridIDArray = new uint[num_particles];

        for (int i = 0; i < num_particles; i++)
        {
            // particle buffer initial values
            particleArray[i].position = new Vector4(Random.Range(0, box_width), Random.Range(0, box_width), Random.Range(0, box_width), 0);
            Vector3 vel = Random.onUnitSphere * speed;
            particleArray[i].velocity = new Vector4(vel.x, vel.y, vel.z, 0);

            // particle buffer initial values
            pIDArray[i] = (uint) i;
            gridIDArray[i] = 0;
        }
        m_particlesBuffer.SetData(particleArray);
        m_particleIDBuffer.SetData(pIDArray);
        m_gridIDBuffer.SetData(gridIDArray);

        m_quadPoints = new ComputeBuffer(6, c_quadStride);

        m_quadPoints.SetData(new[] {
            new Vector3(-0.5f, 0.5f),
            new Vector3(0.5f, 0.5f),
            new Vector3(0.5f, -0.5f),
            new Vector3(0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f)
        });
    }


    float summed_grid_update_time = 0;
    float summed_particle_update_time = 0;
    int update_counter = 0;
    // Update is called once per frame
    void Update()
    {
        update_counter++;
        ParticleCalcultion.SetFloat("deltaTime", Time.deltaTime);
        ParticleCalcultion.SetFloat("time", Time.time);
        ParticleCalcultion.SetFloat("noise", noise);

        int numberOfGroups = Mathf.CeilToInt((float)num_particles / c_groupSize);

        float start_time = 0;
        float end_time = 0;
        float time_taken = 0;


        // Grid Update code in GPU
        ParticleCalcultion.SetBuffer(m_updateGridKernel, "grid_ids", m_gridIDBuffer);
        ParticleCalcultion.SetBuffer(m_updateGridKernel, "particle_ids", m_particleIDBuffer);
        ParticleCalcultion.SetBuffer(m_updateGridKernel, "particles", m_particlesBuffer);        
        ParticleCalcultion.SetTexture(m_updateGridKernel, "NoiseTexture", NoiseTexture);
        start_time = Time.realtimeSinceStartup;
        ParticleCalcultion.Dispatch(m_updateGridKernel, numberOfGroups, 1, 1);
        
        end_time = Time.realtimeSinceStartup;
        time_taken = end_time - start_time;
        // print("Grid Update time taken: " + time_taken*1000 + "ms");
        summed_grid_update_time += time_taken;

        // // Particle Update code in GPU
        // ParticleCalcultion.SetBuffer(m_updateParticleKernel, "particles", m_particlesBuffer);
        // ParticleCalcultion.SetBuffer(m_updateParticleKernel, "grid_ids", m_gridIDBuffer);
        // ParticleCalcultion.SetBuffer(m_updateParticleKernel, "particle_ids", m_particleIDBuffer);
        // ParticleCalcultion.SetTexture(m_updateParticleKernel, "NoiseTexture", NoiseTexture);
        // start_time = Time.realtimeSinceStartup;
        // ParticleCalcultion.Dispatch(m_updateParticleKernel, numberOfGroups, 1, 1);
        // end_time = Time.realtimeSinceStartup;
        // time_taken = end_time - start_time;
        // // print("Particle Update time taken: " + time_taken*1000 + "ms");
        // summed_particle_update_time += time_taken;

        
        
    }


    void debug()
    {
        Particle[] tempArray = new Particle[num_particles];
        m_particlesBuffer.GetData(tempArray);
        

        // Debug
        for (int i = 0; i < 10; i++)
        {
            print(i + ": " + tempArray[i].position);
        }
    }


    void OnRenderObject()
    {
        ParticleMaterial.SetBuffer("particles", m_particlesBuffer);
        ParticleMaterial.SetBuffer("quadPoints", m_quadPoints);

        ParticleMaterial.SetPass(0);

        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, num_particles);
    }

    void OnDestroy()
    {
        m_particlesBuffer.Dispose();
        m_particleIDBuffer.Dispose();
        m_gridIDBuffer.Dispose();
        m_quadPoints.Dispose();
    }

    void OnApplicationQuit()
    {
        float ave_grid_time = summed_grid_update_time / update_counter;
        float ave_particle_time = summed_particle_update_time / update_counter;

        print(" ");
        print("Average grid update code: " + ave_grid_time*1000000 + "ns");
        print("Average particle update code: " + ave_particle_time*1000000 + "ns");
    }
}
