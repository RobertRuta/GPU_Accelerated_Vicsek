using UnityEngine;
namespace vicsek {
    // Simulation struct
    struct Particle
    {
        public Vector4 position;
        public Vector4 velocity;
    }

    struct Cell
    {
        public int is_full;
    } 
}