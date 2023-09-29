using UnityEngine;
public class GUIControl : MonoBehaviour {
    SimulationControl sim;
    Visualiser vis;
    
    void Awake()
    {
        sim = GetComponent<SimulationControl>();
        vis = GetComponent<Visualiser>();
    }

    public int particleCount {get => sim.particleCount; set => sim.particleCount=value;}
    public float radius {get => sim.radius; set => sim.radius=value;}
    public float noise {get => sim.noise; set => sim.noise=value;}
    public float speed {get => sim.speed; set => sim.speed=value;}
    public float particleDensity {get => sim.particleDensity;}
    public float particleCellDensity {get => sim.particleCellDensity;}
    public float particleSize {get => vis.particleSize; set => vis.particleSize=value;}

    int labelCounter = 0;


    void OnGUI() {
        sim.particleCount = AddUIElement("Particle Count: ", sim.particleCount, "", 2, 1000000);
        sim.radius = AddUIElement("Neighbour Radius: ", sim.radius, "m", 0.5f, 10f);
        sim.speed = AddUIElement("Speed: ", sim.speed, "m/s", 0.1f, 100f);
        sim.noise = AddUIElement("Noise: ", sim.noise, "", 0.0f, 1f);
        vis.particleSize = AddUIElement("Particle Size: ", vis.particleSize, "", 0.1f, 10f);

        GUI.Label(new Rect(800, 15, 200, 60), "Initial Particle Density \n" + particleDensity.ToString("F3") + " particles/m^3");
        GUI.Label(new Rect(800, 75, 200, 60), "Initial Particle Cell Density \n" + particleCellDensity.ToString("F3") + " particles/cell");

        labelCounter = 0;
    }


    float AddUIElement(string label, float value, string unit, float min, float max) {
        int y_pos = 15 + labelCounter*30;
        GUI.Label(new Rect(265, y_pos, 200, 30), label + value.ToString() + unit);
        value = GUI.HorizontalSlider(new Rect(25, y_pos + 5, 200, 30), value, min, max);
        labelCounter++;
        return value;
    }


    int AddUIElement(string label, int value, string unit, float min, float max) {
        int y_pos = 15 + labelCounter*30;
        GUI.Label(new Rect(265, y_pos, 200, 30), label + value.ToString() + unit);
        value = (int)GUI.HorizontalSlider(new Rect(25, y_pos + 5, 200, 30), (float)value, min, max);
        labelCounter++;
        return value;
    }
}