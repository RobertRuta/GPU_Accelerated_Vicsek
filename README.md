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

