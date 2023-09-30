using UnityEngine;
using System;
using System.IO;
using vicsek;
public class DebugControl : MonoBehaviour {

    SimulationControl sim;
    DebugArray debugArray1, debugArray2, debugArray3;

    public bool updateToggle = true;
    public bool updateDebug = false;
    public int head = 10, foot = 10;
    public bool displayDebug = false;
    public bool displayAll = false;
    public bool save = false;
    public bool saveOnQuit = false;
    public bool saveToggle = false;
    public bool overwrite = false;



    void Start() {
        sim = GetComponent<SimulationControl>();
    }


    void Update() {
        if (updateDebug == true) {
            UpdateDebugArrays();
            updateDebug = false;
        }

        if (displayDebug == true) {
            if (updateToggle)
                UpdateDebugArrays();
            debugArray1.Print(head, foot, "debug1");
            debugArray2.Print(head, foot, "debug2");
            debugArray3.Print(head, foot, "debug3");
            displayDebug = false;
        }

        if (displayAll == true) {
            PrintBufferData("");
            displayAll = false;
        }

        if (save || saveToggle) {
            if (updateToggle)
                UpdateDebugArrays();
            debugArray1.WriteArrayToFile("./data_analysis/data/debugArray1.csv", overwrite);
            debugArray2.WriteArrayToFile("./data_analysis/data/debugArray2.csv", overwrite);
            debugArray3.WriteArrayToFile("./data_analysis/data/debugArray3.csv", overwrite);
            save = false;
        }
    }

    
    void OnApplicationQuit()
    {
        if (saveOnQuit) {
            UpdateDebugArrays();
            debugArray1.WriteArrayToFile("./data_analysis/data/debugArray1.csv");
            debugArray2.WriteArrayToFile("./data_analysis/data/debugArray2.csv");
            debugArray3.WriteArrayToFile("./data_analysis/data/debugArray3.csv");
        }
    }

    public class DebugArray
    {
        public Vector4[] debugArray {get; private set;}
        public int count {get; private set;}
        public Vector4 mean {get; private set;}
        public Vector4 sum {get; private set;}


        public DebugArray(ComputeBuffer buffer) {
            count = buffer.count;
            debugArray = new Vector4[count];
            
            buffer.GetData(debugArray);
            
            sum = debugArray[0];
            for (int i = 1; i < count; i++)
                sum += debugArray[i];
            
            mean = sum / count;
        }

        public DebugArray(Vector4[] debugArray) {
            count = debugArray.Length;
            sum = debugArray[0];
            
            sum = debugArray[0];
            for (int i = 1; i < count; i++)
                sum += debugArray[i];
            
            mean = sum / count;
        }

        public void Print() {
            for (int i = 0; i < count; i++)
                print(i.ToString() + " | " + debugArray[i]);
        }

        public void Print(int head, int foot) {            
            // Print first "head" number of array elements
            for (int i = 0; i < head; i++)
                print(i.ToString() + " | " + debugArray[i]);
            
            // Print last "foot" number of array elements
            for (int k = 0; k < foot; k++) {
                int i = count - foot + k;
                print(i.ToString() + " | " + debugArray[i]);
            }
        }

        public void Print(int head, int foot, string arrayName) {            
            // Print first "head" number of array elements
            for (int i = 0; i < head; i++)
                print(arrayName + "[" + i.ToString() + "]" + debugArray[i]);
            
            // Print last "foot" number of array elements
            for (int k = 0; k < foot; k++) {
                int i = count - foot + k;
                print(arrayName + "[" + i.ToString() + "]" + debugArray[i]);
            }
        }

        // public void WriteArrayToFile(string filePath) {
        //     using (StreamWriter file = new StreamWriter(filePath)) {
        //         foreach (Vector4 row in debugArray) {
        //             string row_string = row.x.ToString() + "," + 
        //             row.y.ToString() + "," + 
        //             row.z.ToString() + "," + 
        //             row.w.ToString();

        //             file.WriteLine(row_string);
        //         }
        //     }
        // }

        public void WriteArrayToFile(string filePath, bool overwrite=false)
        {
            int fileCount = 0;
            string baseFilePath = filePath;

            while (!overwrite && File.Exists(filePath))
            {
                fileCount++;
                filePath = $"{Path.GetDirectoryName(baseFilePath)}\\{Path.GetFileNameWithoutExtension(baseFilePath)}_{fileCount}{Path.GetExtension(baseFilePath)}";
            }

            using (StreamWriter file = new StreamWriter(filePath))
            {
                foreach (Vector4 row in debugArray)
                {
                    string row_string = row.x.ToString() + "," +
                                        row.y.ToString() + "," +
                                        row.z.ToString() + "," +
                                        row.w.ToString();

                    file.WriteLine(row_string);
                }
            }
        }

        public void Dispose() { 
            if (debugArray != null)
                debugArray = null;
        }
    }  


    void WriteDebugBufferToFile() {
        // debugArray = new Vector4[sim.particleCount];
        // debugArray2 = new Vector4[sim.particleCount];
        // debugArray3 = new Vector4[sim.particleCount];
        // sim.debugBuffer.GetData(debugArray);
        // sim.debugBuffer2.GetData(debugArray2);
        // sim.debugBuffer3.GetData(debugArray3);
        // float[] debugArray_x = new float[sim.particleCount];
        // float[] debugArray_y = new float[sim.particleCount];
        // float[] debugArray_z = new float[sim.particleCount];
        // float[] debugArray_w = new float[sim.particleCount];
        // float[] debugArray_x_2 = new float[sim.particleCount];
        // float[] debugArray_y_2 = new float[sim.particleCount];
        // float[] debugArray_z_2 = new float[sim.particleCount];
        // float[] debugArray_w_2 = new float[sim.particleCount];
        // float[] debugArray_x_3 = new float[sim.particleCount];
        // float[] debugArray_y_3 = new float[sim.particleCount];
        // float[] debugArray_z_3 = new float[sim.particleCount];
        // float[] debugArray_w_3 = new float[sim.particleCount];

        // for (int i = 0; i < sim.particleCount; i++) {
        //     debugArray_x[i] = debugArray[i].x;
        //     debugArray_y[i] = debugArray[i].y;
        //     debugArray_z[i] = debugArray[i].z;
        //     debugArray_w[i] = debugArray[i].w;
        //     debugArray_x_2[i] = debugArray2[i].x;
        //     debugArray_y_2[i] = debugArray2[i].y;
        //     debugArray_z_2[i] = debugArray2[i].z;
        //     debugArray_w_2[i] = debugArray2[i].w;
        //     debugArray_x_3[i] = debugArray3[i].x;
        //     debugArray_y_3[i] = debugArray3[i].y;
        //     debugArray_z_3[i] = debugArray3[i].z;
        //     debugArray_w_3[i] = debugArray3[i].w;
        // }

        // SaveFloatsToCSV(debugArray_x, "./debugArray_x.csv");
        // SaveFloatsToCSV(debugArray_y, "./debugArray_y.csv");
        // SaveFloatsToCSV(debugArray_z, "./debugArray_z.csv");
        // SaveFloatsToCSV(debugArray_w, "./debugArray_w.csv");

        // SaveFloatsToCSV(debugArray_x_2, "./debugArray_x_2.csv");
        // SaveFloatsToCSV(debugArray_y_2, "./debugArray_y_2.csv");
        // SaveFloatsToCSV(debugArray_z_2, "./debugArray_z_2.csv");
        // SaveFloatsToCSV(debugArray_w_2, "./debugArray_w_2.csv");
   
        // SaveFloatsToCSV(debugArray_x_3, "./debugArray_x_3.csv");
        // SaveFloatsToCSV(debugArray_y_3, "./debugArray_y_3.csv");
        // SaveFloatsToCSV(debugArray_z_3, "./debugArray_z_3.csv");
        // SaveFloatsToCSV(debugArray_w_3, "./debugArray_w_3.csv");
    }

    void UpdateDebugArrays()
    {
        debugArray1 = new DebugArray(sim.debugBuffer1.buffer);
        debugArray2 = new DebugArray(sim.debugBuffer2.buffer);
        debugArray3 = new DebugArray(sim.debugBuffer3.buffer);      
    }


    void SaveFloatsToCSV(float[] floatArray, string filePath)
    {
        using (StreamWriter file = new StreamWriter(filePath)) {
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
        Vector4[] debugArray = new Vector4[sim.particleCount];
        sim.particleBuffer.buffer.GetData(particles);
        sim.particleIDBuffer.buffer.GetData(particle_ids);
        sim.keysBuffer.buffer.GetData(keys);
        sim.cellIDBuffer.buffer.GetData(values);
        sim.startendBuffer.buffer.GetData(startend);
        sim.debugBuffer1.buffer.GetData(debugArray);

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