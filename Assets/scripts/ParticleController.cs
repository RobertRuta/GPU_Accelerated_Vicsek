using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{

    public ComputeShader ParticleCalcultion;
    public Material ParticleMaterial;
    [Range(0, 30000)]
    public int NumParticles = 30000;
    // public float Radius = 10.0f;
    public Vector3 box;
    [SerializeField]
    private Vector3 grid;
    [SerializeField]
    private Vector3Int gridDims;
    [Range(0.01f, 10.0f)]
    public float radius = 1.0f;
    [Range(0.01f, 10.0f)]
    public float noise = 1.0f;
    [Range(0.0f, 200.0f)]
    public float speed = 4.0f;
    public Texture2D NoiseTexture;

    private const int c_groupSize = 128;
    private int m_updateParticleKernel;
    private int m_updateGridKernel;



    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 color;
    };


    ComputeBuffer m_particlesBuffer;
    ComputeBuffer m_particleIDBuffer;
    ComputeBuffer m_gridIDBuffer;
    ComputeBuffer m_quadPoints;
    const int c_particleStride = 36;
    const int c_quadStride = 12;


    // Start is called before the first frame update
    void Start()
    {
        m_updateParticleKernel = ParticleCalcultion.FindKernel("UpdateParticles");
        m_updateParticleKernel = ParticleCalcultion.FindKernel("UpdateGrid");
        m_particlesBuffer = new ComputeBuffer(NumParticles, c_particleStride);
        m_particleIDBuffer = new ComputeBuffer(NumParticles, sizeof(uint));
        m_gridIDBuffer = new ComputeBuffer(NumParticles, sizeof(uint));

        Particle[] particleArray = new Particle[NumParticles];
        uint[] pIDArray = new uint[NumParticles];
        uint[] gridIDArray = new uint[NumParticles];

        for (int i = 0; i < NumParticles; i++)
        {
            particleArray[i].position = new Vector3(Random.Range(0, box.x), Random.Range(0, box.y), Random.Range(0, box.z));
            particleArray[i].velocity = Random.insideUnitSphere * speed;
            particleArray[i].color = Vector3.one;

            pIDArray[i] = (uint) i;
            gridIDArray[i] = 0;
        }


        ParticleCalcultion.SetInt("numParticles", NumParticles);
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

    // Update is called once per frame
    void Update()
    {
        ComputeGrid();

        ParticleCalcultion.SetFloat("deltaTime", Time.deltaTime);
        ParticleCalcultion.SetFloat("time", Time.time);
        ParticleCalcultion.SetFloat("noise", noise);
        ParticleCalcultion.SetFloat("radius", radius);
        ParticleCalcultion.SetFloat("speed", speed);
        ParticleCalcultion.SetFloats("box", new [] {box.x, box.y, box.z});
        ParticleCalcultion.SetInts("grid_dims", new [] {gridDims.x, gridDims.y, gridDims.z});

        int numberOfGroups = Mathf.CeilToInt((float)NumParticles / c_groupSize);
        ParticleCalcultion.SetBuffer(m_updateParticleKernel, "particles", m_particlesBuffer);
        ParticleCalcultion.SetBuffer(m_updateParticleKernel, "grid_ids", m_gridIDBuffer);
        ParticleCalcultion.SetBuffer(m_updateParticleKernel, "particle_ids", m_particleIDBuffer);
        ParticleCalcultion.SetTexture(m_updateParticleKernel, "NoiseTexture", NoiseTexture);
        ParticleCalcultion.Dispatch(m_updateParticleKernel, numberOfGroups, 1, 1);
        
        ParticleCalcultion.SetBuffer(m_updateGridKernel, "grid_ids", m_gridIDBuffer);
        ParticleCalcultion.SetBuffer(m_updateGridKernel, "particle_ids", m_particleIDBuffer);
        ParticleCalcultion.SetBuffer(m_updateGridKernel, "particles", m_particlesBuffer);        
        ParticleCalcultion.SetTexture(m_updateGridKernel, "NoiseTexture", NoiseTexture);
        ParticleCalcultion.Dispatch(m_updateGridKernel, numberOfGroups, 1, 1);

        uint[] temp_g_array = new uint[NumParticles];
        uint[] temp_p_array = new uint[NumParticles];
        m_gridIDBuffer.GetData(temp_g_array);
        m_particleIDBuffer.GetData(temp_p_array);
        for (int i = 0; i < 100; i++)
        {
            print(" " + temp_p_array[i] + " | " + temp_g_array[temp_p_array[i]]);
        }
    }

    void ComputeGrid()
    {
        float cellDim = radius;
        // Recalculate box dimensions
        grid.x = Mathf.Ceil(box.x / cellDim) * cellDim;
        grid.y = Mathf.Ceil(box.y / cellDim) * cellDim;
        grid.z = Mathf.Ceil(box.z / cellDim) * cellDim;
        // Set grid dimensions
        gridDims = new Vector3Int((int) (box.x / cellDim) , (int) (box.y / cellDim), (int) (box.z / cellDim));
    }

    void debug()
    {
        Particle[] tempArray = new Particle[NumParticles];
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

        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, NumParticles);
    }

    void OnDestroy()
    {
        m_particlesBuffer.Dispose();
        m_quadPoints.Dispose();
    }


}
