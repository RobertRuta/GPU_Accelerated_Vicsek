using UnityEngine;
using System.IO;
using vicsek;
public class DebugControl : MonoBehaviour {

    SimulationControl sim;
    ComputeBuffer debugBuffer, debugBuffer2, debugBuffer3;

    public bool update_toggle = false;
    public bool print_debug_buffer = false;
    public bool print_buffers = false;
    public bool save_debug_to_file = false;
    public bool get_buffers = false;

    Vector4[] debugArray, debugArray2, debugArray3;
    [SerializeField]
    Vector4 sums, means;


    // class DebugBuffer<T> {
    //     T sums, means;
    //     T[] array;

    //     public DebugBuffer(ComputeBuffer buffer) {
    //         int count = buffer.count;
    //         array = new T[count];
    //         buffer.GetData(array);
    //         if (typeof(T) == typeof(float)) {
    //             foreach (T item in array) {
    //                 sums = sums + item;
    //             }
    //         }
    //     }
    // }

    // DebugBuffer<Vector4> debug;
    // DebugBuffer<Particle> particle_debug;
    // DebugBuffer<>


    void Start()
    {
        sim = GetComponent<SimulationControl>();
    }

    void Update()
    {
        if (get_buffers) {
            GetBufferData();
            get_buffers = false;
        }

        if (print_debug_buffer) {
            PreviewDebugBufferData();
            print_debug_buffer = false;
        }

        
        if (print_buffers) {
            PrintBufferData("Debug Update");
            print_buffers = false;
        }


        if (save_debug_to_file) {
            WriteDebugBufferToFile();
            save_debug_to_file = false;
        }
    }

    
    void OnApplicationQuit()
    {
        GetBufferData();
        PreviewDebugBufferData();
        PrintBufferData("Application Quit");
        WriteDebugBufferToFile();
    }  

    void GetBufferData()
    {
        debugArray = new Vector4[sim.particleCount];
        debugArray2 = new Vector4[sim.particleCount];
        debugArray3 = new Vector4[sim.particleCount];
        sums = new Vector4(0,0,0,0);
        means = new Vector4(0,0,0,0);
        sim.debugBuffer.GetData(debugArray);
        sim.debugBuffer2.GetData(debugArray2);
        sim.debugBuffer3.GetData(debugArray3);
        for (int i = 0; i < sim.particleCount; i++)
            sums += debugArray[i];
        means = sums / (float)sim.particleCount;
    }

    void WriteDebugBufferToFile() {
        debugArray = new Vector4[sim.particleCount];
        debugArray2 = new Vector4[sim.particleCount];
        debugArray3 = new Vector4[sim.particleCount];
        sim.debugBuffer.GetData(debugArray);
        sim.debugBuffer2.GetData(debugArray2);
        sim.debugBuffer3.GetData(debugArray3);
        float[] debugArray_x = new float[sim.particleCount];
        float[] debugArray_y = new float[sim.particleCount];
        float[] debugArray_z = new float[sim.particleCount];
        float[] debugArray_w = new float[sim.particleCount];
        float[] debugArray_x_2 = new float[sim.particleCount];
        float[] debugArray_y_2 = new float[sim.particleCount];
        float[] debugArray_z_2 = new float[sim.particleCount];
        float[] debugArray_w_2 = new float[sim.particleCount];
        float[] debugArray_x_3 = new float[sim.particleCount];
        float[] debugArray_y_3 = new float[sim.particleCount];
        float[] debugArray_z_3 = new float[sim.particleCount];
        float[] debugArray_w_3 = new float[sim.particleCount];

        for (int i = 0; i < sim.particleCount; i++) {
            debugArray_x[i] = debugArray[i].x;
            debugArray_y[i] = debugArray[i].y;
            debugArray_z[i] = debugArray[i].z;
            debugArray_w[i] = debugArray[i].w;
            debugArray_x_2[i] = debugArray2[i].x;
            debugArray_y_2[i] = debugArray2[i].y;
            debugArray_z_2[i] = debugArray2[i].z;
            debugArray_w_2[i] = debugArray2[i].w;
            debugArray_x_3[i] = debugArray3[i].x;
            debugArray_y_3[i] = debugArray3[i].y;
            debugArray_z_3[i] = debugArray3[i].z;
            debugArray_w_3[i] = debugArray3[i].w;
        }

        SaveFloatsToCSV(debugArray_x, "./debugArray_x.csv");
        SaveFloatsToCSV(debugArray_y, "./debugArray_y.csv");
        SaveFloatsToCSV(debugArray_z, "./debugArray_z.csv");
        SaveFloatsToCSV(debugArray_w, "./debugArray_w.csv");

        SaveFloatsToCSV(debugArray_x_2, "./debugArray_x_2.csv");
        SaveFloatsToCSV(debugArray_y_2, "./debugArray_y_2.csv");
        SaveFloatsToCSV(debugArray_z_2, "./debugArray_z_2.csv");
        SaveFloatsToCSV(debugArray_w_2, "./debugArray_w_2.csv");
   
        SaveFloatsToCSV(debugArray_x_3, "./debugArray_x_3.csv");
        SaveFloatsToCSV(debugArray_y_3, "./debugArray_y_3.csv");
        SaveFloatsToCSV(debugArray_z_3, "./debugArray_z_3.csv");
        SaveFloatsToCSV(debugArray_w_3, "./debugArray_w_3.csv");
    }


    void PreviewDebugBufferData() 
    {
        GetBufferData();
        HeadAndFootPrint<Vector4>(debugArray, 10, 10, "debug_buffer");
        HeadAndFootPrint<Vector4>(debugArray2, 10, 10, "debug_buffer2");
        HeadAndFootPrint<Vector4>(debugArray3, 10, 10, "debug_buffer3");
    }


    void SaveFloatsToCSV(float[] floatArray, string fileName)
    {
        using (StreamWriter file = new StreamWriter(fileName)) {
            foreach (float f in floatArray)
                file.WriteLine(f);
        }
    }


    public void PrintBufferData(string after)
    {
        Particle[] particles = new Particle[sim.particleCount];
        uint[] values = new uint[sim.particleCount];
        uint[] particle_ids = new uint[sim.particleCount];
        uint[] keys = new uint[sim.particleCount];
        Vector2Int[] startend = new Vector2Int[sim.cellCount];
        debugArray = new Vector4[sim.particleCount];
        sim.particleBuffer.GetData(particles);
        sim.particleIDBuffer.GetData(particle_ids);
        sim.keyBuffer.GetData(keys);
        sim.cellIDBuffer.GetData(values);
        sim.startendIDBuffer.GetData(startend);
        sim.debugBuffer.GetData(debugArray);


        for (int i = 0; i < 10; i++)
        {
            print("After " + after + " | ParticleID["+ i + "]: " + particle_ids[i] + " | particle["+ particle_ids[i] + "]: " + particles[particle_ids[i]].position + ", " + particles[particle_ids[i]].velocity + " | keys[" + i + "]: " + keys[i] + " | grid[" + i + "]: " + values[i] + " | grid[keys[" + i + "]]: " + values[keys[i]]  + " | grid[particle_id[" + i + "]]: " + values[particle_ids[i]] + " | start_end["+ i + "]: " + startend[i] + " | debug["+ i + "]: " + debugArray[i]);
        }

        for (int k = 0; k < 10; k++)
        {
            int i = sim.particleCount - 10 + k;
            int j = sim.cellCount - 10 + k;
            print("After " + after + " | ParticleID["+ i + "]: " + particle_ids[i] + " | particle["+ particle_ids[i] + "]: " + particles[particle_ids[i]].position + ", " + particles[particle_ids[i]].velocity + " | keys[" + i + "]: " + keys[i] + " | grid[" + i + "]: " + values[i] + " | grid[keys[" + i + "]]: " + values[keys[i]]  + " | grid[particle_id[" + i + "]]: " + values[particle_ids[i]] + " | start_end["+ j + "]: " + startend[j] + " | debug["+ i + "]: " + debugArray[i]);
        }
    }

    void HeadAndFootPrint<T>(T[] data, int head=10, int foot=10, string data_name="data")
    {
        int count = data.Length;
        for (int i = 0; i < head; i++)
            print( data_name + "["+ i + "]: " + data[i]);

        for (int k = 0; k < foot; k++)
        {
            int i = count - foot + k;
            print( data_name + "["+ i + "]: " + data[i]);
        }
    }
}