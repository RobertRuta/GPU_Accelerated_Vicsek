using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using vicsek;
using System;

public class UIControl : MonoBehaviour
{
    SimulationControl sim;
    Visualiser vis;
    SimulationCamera cam;
    public int infoFrequency = 60;
    public bool isPaused = false;
    public GameObject pauseMenu, optionMenu, hideParamButton, paramMenu, infoMenu;
    public Slider colourIntensitySlider;
    public Slider mouseXSlider, mouseYSlider, rotDampingSlider;
    public TMP_Dropdown meshDropdown, fpsDropdown;
    public TMP_Text radiusValue, noiseValue, particleSizeValue, particleCountValue, speedValue;
    public Slider radiusSlider, noiseSlider, particleSizeSlider, speedSlider;

    public TMP_Text fpsValue, cellCountValue, 
                    xBoxValue, yBoxValue, zBoxValue, xGridValue, yGridValue, zGridValue, orderValue, uniformityValue;
    private float fps, deltaTime;
    private int frameCounter = 0;


    // Start is called before the first frame update
    void Start()
    {
        sim = GetComponent<SimulationControl>();
        vis = GetComponent<Visualiser>();
        cam = GameObject.Find("Main Camera").GetComponent<SimulationCamera>();
        pauseMenu.SetActive(false);
        optionMenu.SetActive(false);

        particleCountValue.text = sim.particleCount.ToString() + " particles";

        radiusValue.text = sim.radius.ToString() + " m";
        radiusSlider.value = sim.radius;

        noiseValue.text = sim.noise.ToString();
        noiseSlider.value = sim.noise;

        particleSizeValue.text = vis.particleSize.ToString();
        particleSizeSlider.value = vis.particleSize;

        speedValue.text = sim.speed.ToString() + "m/s";
        speedSlider.value = sim.speed;
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1.0f / deltaTime;

        if (pauseMenu.activeSelf | optionMenu.activeSelf)
            cam.camControlOn = false;
        else
            cam.camControlOn = true;

        if (Input.GetKeyDown("escape")) {
            
            if (pauseMenu.activeSelf) {
                pauseMenu.SetActive(false);
                paramMenu.SetActive(true);
                infoMenu.SetActive(true);
                isPaused = false;
            }

            else if (optionMenu.activeSelf) {
                optionMenu.SetActive(false);
                // paramMenu.SetActive(false);
                pauseMenu.SetActive(true);
            }

            else {
                paramMenu.SetActive(false);
                infoMenu.SetActive(false);
                pauseMenu.SetActive(true);
                isPaused = true;
            }
        }
        
        if ((frameCounter % infoFrequency) == 0)
            InfoUIHandler();
        frameCounter += 1;
    }

    public void ResumeSimulation() {
        isPaused = false;
        pauseMenu.SetActive(false);
    }

    public void QuitGame() {
        Application.Quit();
        print("Quitting...");
    }

    public void OptionMenu() {
        optionMenu.SetActive(true);
        pauseMenu.SetActive(false);
    }

    public void BackToPauseMenu() {
        optionMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }

    public void SliderUpdate() {
        vis.colorIntensity = colourIntensitySlider.value;
        cam.xSpeed = mouseXSlider.value;
        cam.ySpeed = mouseYSlider.value;
        cam.inertialDamping = rotDampingSlider.value;
    }

    public void ToggleOptimization() {
        sim.optimized = !sim.optimized;
    }

    public void ToggleHeading() {
        vis.enableHeading = !vis.enableHeading;
    }

    public void DropdownUpdate() { 
        string selectedMesh = meshDropdown.options[meshDropdown.value].text;

        if (selectedMesh != vis.particleMesh.name) {
            vis.LoadMesh(selectedMesh);
        }
    }

    public void FPSDropdownUpdate() { 
        string selectedFPS = fpsDropdown.options[fpsDropdown.value].text;

        if (selectedFPS == "inf") {
            sim.targetFPS = int.MaxValue;
            sim.SetTargetFPS();
        }
        else if (selectedFPS != sim.targetFPS.ToString()) {
            sim.targetFPS = int.Parse(selectedFPS);
            sim.SetTargetFPS();
        }
    }

    public void ParamUIHandler() {
        // sim.particleCount = radiusSlider.value;
        particleCountValue.text = sim.particleCount.ToString() + " particles";

        sim.radius = radiusSlider.value;
        radiusValue.text = sim.radius.ToString() + " m";

        sim.noise = noiseSlider.value;
        noiseValue.text = sim.noise.ToString();

        vis.particleSize = particleSizeSlider.value;
        particleSizeValue.text = vis.particleSize.ToString();

        sim.speed = speedSlider.value;
        speedValue.text = sim.speed.ToString() + "m/s";
    }


    public void InfoUIHandler() {
        // sim.particleCount = radiusSlider.value;
        fpsValue.text = fps.ToString("F0");
        
        xBoxValue.text = sim.box.x.ToString("F1");
        yBoxValue.text = sim.box.y.ToString("F1");
        zBoxValue.text = sim.box.z.ToString("F1");

        cellCountValue.text = sim.cellCount.ToString();
        xGridValue.text = sim.grid_dims.x.ToString();
        yGridValue.text = sim.grid_dims.y.ToString();
        zGridValue.text = sim.grid_dims.z.ToString();

        Particle[] particles = sim.particleBuffer.ReturnData();
        sim.ComputeOrderAndUniformityParameters(particles);
        orderValue.text = (sim.orderParameter * 100).ToString("F1");
        uniformityValue.text = sim.uniformityParameter.ToString("F3");
    }

    public void IncrementUpParticleCount() {
        sim.particleCount = sim.particleCount * 2 + 1;
    }

    public void IncrementDownParticleCount() {
        print("firing");
        print(sim.particleCount);
        sim.particleCount = sim.particleCount / 2 + 1;
        print(sim.particleCount);
    }

    public void ResetSim() {
        sim.resetToggle = true;
    }

    public void PauseSim() {
        isPaused = !isPaused;
    }
}
