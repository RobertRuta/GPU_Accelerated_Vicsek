// using UnityEngine;
// public class GUIControl : MonoBehaviour {
//     SimulationControl sim;
//     void Awake()
//     {
//         sim = GetComponent<SimulationControl>();
//     }

//     public int particleCount {get => sim.particleCount; set => sim.particleCount=value;}
//     public float radius {get => sim.radius; set => sim.radius=value;}
//     public float noise {get => sim.noise; set => sim.noise=value;}
//     public float particleSize {get => sim.particleSize; set => sim.particleSize=value;}
//     public float particleDensity {get => sim.particleDensity;}
//     public float particleCellDensity {get => sim.particleCellDensity;}


//     void OnGUI() {
//         GUI.Label(new Rect(265, 15, 200, 30), "Particle Count: " + particleCount.ToString());
//         particleCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)particleCount, 1.0f, Mathf.Pow(2,21)+1);
        
//         // GUI.Label(new Rect(265, 45, 200, 30), "Box width: " + box_width.ToString() + "m");
//         // box_width = GUI.HorizontalSlider(new Rect(25, 50, 200, 30), box_width, box_range.x, box_range.y);

//         GUI.Label(new Rect(265, 45, 200, 30), "Neighbour radius: " + radius.ToString() + "m");
//         radius = GUI.HorizontalSlider(new Rect(25, 50, 200, 30), radius, sim.radius_range.x, sim.radius_range.y);
        
//         GUI.Label(new Rect(265, 75, 200, 30), "Noise: " + noise.ToString() + "");
//         noise = GUI.HorizontalSlider(new Rect(25, 80, 200, 30), noise, 0.0f, 1f);
        
//         GUI.Label(new Rect(265, 105, 200, 30), "Particle Size: " + particleSize.ToString() + "");
//         particleSize = GUI.HorizontalSlider(new Rect(25, 110, 200, 30), particleSize, 0.05f, 5f);

//         GUI.Label(new Rect(800, 15, 200, 60), "Initial Particle Density \n" + particleDensity.ToString("F3") + " particles/m^3");
//         GUI.Label(new Rect(800, 75, 200, 60), "Initial Particle Cell Density \n" + particleCellDensity.ToString("F3") + " particles/cell");
//     }
// }