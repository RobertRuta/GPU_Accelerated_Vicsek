using UnityEngine;
namespace vicsek {
    // Simulation struct
    public struct Particle
    {
        public Vector4 position;
        public Vector4 velocity;
    }

    public struct Cell
    {
        public int is_full;
    } 
}