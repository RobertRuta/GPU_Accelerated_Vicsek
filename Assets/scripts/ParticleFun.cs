using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleFun : MonoBehaviour
{

    private Vector2 cursorPos;

    // struct
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
    }

    const int SIZE_PARTICLE = 7 * sizeof(float);
    const int SIZE_DEBUG = 3 * sizeof(float);

    public int particleCount = 1000000;
    public int randCount = 512*512;
    public int debugCount = 1000000;
    public float radius = 1f;
    public Material material;
    public ComputeShader shader;
    [Range(1, 10)]
    public int pointSize = 2;

    public Texture2D noiseTexture;
    ComputeBuffer particleBuffer;
    ComputeBuffer debugBuffer;
    ComputeBuffer randomBuffer;

    int groupSizeX; 
    
    
    // Use this for initialization
    void Start()
    {
        Init("CSVicsek");
        //InitRandom();
        //Init("CSVicsek");
    }

    void Init(string kernelName)
    {
        // initialize the particles
        Particle[] particleArray = new Particle[particleCount];
        // Vector3[] debugArray = new Vector3[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            Vector3 xyz = new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f), Random.Range(-1f,1f));
            xyz.Normalize();
            xyz *= Random.value;
            xyz *= 0.5f;
            xyz.z += 3;

            particleArray[i].position = new Vector3(Random.Range(0f,1f), Random.Range(0f,1f), Random.Range(0f,1f));
            particleArray[i].velocity = Random.onUnitSphere;

            // Initial life value
            particleArray[i].life = Random.value * 5.0f + 1.0f;
        }

        // create compute buffer
        particleBuffer = new ComputeBuffer(particleCount, SIZE_PARTICLE);
        // debugBuffer = new ComputeBuffer(debugCount, 3*sizeof(float));


        particleBuffer.SetData(particleArray);
        // debugBuffer.SetData(debugArray);

        // find the id of the kernel
        int kernelID = shader.FindKernel(kernelName);

        uint threadsX;
        shader.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadsX);

        // bind the compute buffer to the shader and the compute shader
        shader.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        
        shader.SetTexture(kernelID, "noiseTexture", noiseTexture);
        material.SetBuffer("particleBuffer", particleBuffer);
        // material.SetBuffer("debugBuffer", debugBuffer);

        shader.SetInt("particleCount", particleCount);
        material.SetInt("_PointSize", pointSize);

    }

    void InitRandom()
    {
        // // Initialise random array
        // Vector3[,] randomArray = new Vector3[512,512];
        // for (int i = 0; i < 512; i++)
        // {
        //     for (int j = 0; j < 512; j++)
        //     {
        //         randomArray[i,j] = Vector3.zero;
        //     }
        // }
        // Initialise random array
        Vector3[] randomArray = new Vector3[randCount];
        for (int i = 0; i < randCount; i++)
            randomArray[i] = Vector3.zero;

        randomBuffer = new ComputeBuffer(randCount, 3*sizeof(float));

        int kernelID = shader.FindKernel("CSRandom");
        shader.SetBuffer(kernelID, "randBuffer", randomBuffer);
        shader.SetTexture(kernelID, "noiseTexture", noiseTexture);

        shader.Dispatch(kernelID, 512/128, 512/128, 1);
    }

    void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleCount);
    }

    void OnDestroy()
    {
        if (particleBuffer != null)
            particleBuffer.Release();
    }

    // Update is called once per frame
    void Update()
    {

        float[] mousePosition2D = { cursorPos.x, cursorPos.y };

        // Send datas to the compute shader
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetFloat("time", Time.time);
        shader.SetFloat("radius", radius);
        shader.SetFloats("mousePosition", mousePosition2D);

        // Update the Particles
        int kernelID = shader.FindKernel("CSVicsek");
        shader.Dispatch(kernelID, groupSizeX, 1, 1);

        // Vector3[] tempArray = new Vector3[randCount];
        // randomBuffer.GetData(tempArray);
        // for (int i = 0; i < 100; i++)
        // {
        //     print("id: " + i + "    " + tempArray[i]);
        // }
    }

    void OnGUI()
    {
        Vector3 p = new Vector3();
        Camera c = Camera.main;
        Event e = Event.current;
        Vector2 mousePos = new Vector2();

        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        mousePos.x = e.mousePosition.x;
        mousePos.y = c.pixelHeight - e.mousePosition.y;

        p = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane + 14));// z = 3.

        cursorPos.x = p.x;
        cursorPos.y = p.y;
        
    }
}
