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