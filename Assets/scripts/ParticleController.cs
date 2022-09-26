using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{

    public ComputeShader ParticleCalcultion;
    public Material ParticleMaterial;
    public int NumParticles = 500000;
    // public float Radius = 10.0f;
    public Vector3 box;
    public float neighbourRadius = 1.0f;
    public float noise = 1.0f;
    public float speed = 4.0f;
    public Texture2D NoiseTexture;

    private const int c_groupSize = 128;
    private int m_updateParticleKernel;



    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 color;
    };


    ComputeBuffer m_particlesBuffer;
    ComputeBuffer m_quadPoints;
    const int c_particleStride = 36;
    const int c_quadStride = 12;


    // Start is called before the first frame update
    void Start()
    {
        m_updateParticleKernel = ParticleCalcultion.FindKernel("UpdateParticles");
        m_particlesBuffer = new ComputeBuffer(NumParticles, c_particleStride);

        Particle[] particleArray = new Particle[NumParticles];

        for (int i = 0; i < NumParticles; i++)
        {
            particleArray[i].position = new Vector3(Random.Range(0, box.x), Random.Range(0, box.y), Random.Range(0, box.z));
            particleArray[i].velocity = Random.insideUnitSphere * speed;
            particleArray[i].color = Vector3.one;
        }


        ParticleCalcultion.SetInt("numParticles", NumParticles);
        m_particlesBuffer.SetData(particleArray);

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
        ParticleCalcultion.SetBuffer(m_updateParticleKernel, "particles", m_particlesBuffer);
        ParticleCalcultion.SetFloat("deltaTime", Time.deltaTime);
        ParticleCalcultion.SetFloat("time", Time.time);
        ParticleCalcultion.SetFloat("noise", noise);
        ParticleCalcultion.SetFloat("radius", neighbourRadius);
        ParticleCalcultion.SetFloat("speed", speed);
        ParticleCalcultion.SetFloats("box", new [] {box.x, box.y, box.z});
        ParticleCalcultion.SetTexture(m_updateParticleKernel, "NoiseTexture", NoiseTexture);

        int numberOfGroups = Mathf.CeilToInt((float)NumParticles / c_groupSize);
        ParticleCalcultion.Dispatch(m_updateParticleKernel, numberOfGroups, 1, 1);
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
