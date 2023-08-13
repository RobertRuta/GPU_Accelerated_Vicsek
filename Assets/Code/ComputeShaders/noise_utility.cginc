#include "simulation_variables.cginc"

// Random number generation

float3 RodriguezRot(float3 v, float3 axis, float angle)
{
    float3 rotated_v = v*cos(angle) + cross(axis, v)*sin(angle) + axis*dot(axis, v)*(1-cos(angle));
    return rotated_v;
}


float3 CartFromSpherical(float theta, float phi)
{
    float x = cos(phi)*sin(theta);
    float z = sin(phi)*sin(theta);
    float y = cos(theta);

    return float3(x,y,z);
}


uint rand(uint state)
{
    state ^= state << 13;
    state ^= state >> 17;
    state ^= state << 5;
    return state;
}


float2 box_muller_normal_random(float rand1, float rand2)
{
    float r = sqrt(-2*log(rand1));
    float x = cos(2*PI*rand1);
    float y = sin(2*PI*rand2);

    float z1 = r*x;
    float z2 = r*y;
    
    return float2(z1, z2);
}


static const uint k = 1103515245;

float3 hash( uint3 x )
{
    x = ((x>>8)^x.yzx)*k;
    x = ((x>>8)^x.yzx)*k;
    x = ((x>>8)^x.yzx)*k;    
    return float3(x)*(1.0/float(uint(0xffffffff)));
}


float3 RandomNormalVector(float3 vec, uint3 seed) {
    float3 random_vector = (hash(seed)-0.5) * 2;
    float3 normal_vec = cross(vec, random_vector);
    return normalize(normal_vec);
}


// MurmurHash3 implementation
uint MurmurHash3(uint seed) {
    seed ^= seed >> 16;
    seed *= 0x85ebca6b;
    seed ^= seed >> 13;
    seed *= 0xc2b2ae35;
    seed ^= seed >> 16;
    return seed;
}



float3 GetOtherVector(float3 vec) {
    float x_dot = abs(dot(vec, float3(1,0,0)));
    float y_dot = abs(dot(vec, float3(0,1,0)));
    float z_dot = abs(dot(vec, float3(0,0,1)));
    float minimum = min(min(x_dot, y_dot), z_dot);

    if (minimum == x_dot)
        return float3(1, 0, 0);
    if (minimum == y_dot)
        return float3(0, 1, 0);
    if (minimum == z_dot)
        return float3(0, 0, 1);

    return float3(1, 0, 0);
}