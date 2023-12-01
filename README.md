# GPU_Accelerated_Vicsek

This repository is the home of a 3D Vicsek simulation made in Unity with the help of compute shaders. The Vicsek algorithm is a very simple set of rules that serve to generate a collective bird-like flocking motion.

The algorithm is as follows:

1. Each agent moves at a constant speed (𝑠).
2. Agents within a distance (𝑟) of another agent are that agent's neighbours (every agent is its own neighbour).
3. Each agent sets its direction based on the averaged direction of its neighbours.
4. Each agent's direction of motion is perturbed at intensity (𝜂).

## Simulation Parameters:
 - Neighbour radius (𝑟): a float that is bounded based on 𝐿.
 - Particle speed (𝑠): a float
 - Box Width (𝐿): a float that ranges between 0 and 100.
 - Noise (𝜂): a float that ranges between 0 and 1.


## Implementation
This project features two implementations of the above algorithm, the first is a brute-force apporach that limits the simulation to a few tens of thousands of agents, and the second employs a spatial partitioning algorithm that optimizes the neighbour searching process and unlocks the possibility to smoothly run hundres of thousands of agents in 3D space.


## Instructions

All code is located in the Assets/Scripts/ directory.

This project can be opened within the Unity editor.
1. First clone the repository onto your local machine in a chosen directory.
2. Download the Unity Hub for free at: https://unity.com/download, and install.
3. After setup, you will be able to open this project in Unity by selecting "Open" > "Add project from disk" and then navigating to the directory in which you cloned this repository.
![image](https://github.com/RobertRuta/GPU_Accelerated_Vicsek/assets/77187208/73671f07-1478-4a05-b126-fe6317893aa6)
4. After adding this project to Unity hub, you may be prompted to download a Unity editor 2022.3.13f1. This Unity editor is required to open this project, because it was developed with this version of Unity.
5. After following the instructions on Unity hub, you should have the correct Unity editor installed, and should be able to open the program. 
