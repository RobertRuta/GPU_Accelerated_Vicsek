using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIControl : MonoBehaviour
{
    SimulationControl sim;
    Visualiser vis;
    SimulationCamera cam;
    public bool isPaused = false;
    public GameObject pauseMenu, optionMenu, hideParamButton, paramMenu;
    public Slider colourIntensitySlider;
    public Slider mouseXSlider, mouseYSlider, rotDampingSlider;
    public TMP_Dropdown meshDropdown;
    public TMP_Text radiusValue, noiseValue, particleSizeValue, particleCountValue, speedValue;
    public Slider radiusSlider, noiseSlider, particleSizeSlider, speedSlider;


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
        if (pauseMenu.activeSelf | optionMenu.activeSelf)
            cam.camControlOn = false;
        else
            cam.camControlOn = true;

        if (Input.GetKeyDown("escape")) {
            
            if (pauseMenu.activeSelf) {
                pauseMenu.SetActive(false);
                paramMenu.SetActive(true);
                isPaused = false;
            }

            else if (optionMenu.activeSelf) {
                optionMenu.SetActive(false);
                // paramMenu.SetActive(false);
                pauseMenu.SetActive(true);
            }

            else {
                paramMenu.SetActive(false);
                pauseMenu.SetActive(true);
                isPaused = true;
            }
        }
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

    public void DropdownUpdate() { 
        string selectedMesh = meshDropdown.options[meshDropdown.value].text;

        if (selectedMesh != vis.particleMesh.name) {
            vis.LoadMesh(selectedMesh);
        }
        print(meshDropdown.options[meshDropdown.value].text);
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
}
