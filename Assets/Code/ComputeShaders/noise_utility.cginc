

// Random number generation
int rng_state;
uint s[4];


uint rotl(uint x, int k)
{
	return (x << k) | (x >> (32 - k));
}

uint xorshift128plus()
{
    // Initialise rng seeder with some big primes
    s[0] = 27538393;
    s[1] = 88067389;
    s[2] = 67901627;
    s[3] = 36402433;

    rng_state = s[0] + s[3];
    uint t = s[1] << 9;

	s[2] ^= s[0];
	s[3] ^= s[1];
	s[1] ^= s[2];
	s[0] ^= s[3];

	s[2] ^= t;

	s[3] = rotl(s[3], 11);
    return rng_state;
}


uint rand_xorshift()
{
	// Xorshift algorithm from George Marsaglia's paper
	rng_state ^= (rng_state << 13);
	rng_state ^= (rng_state >> 17);
	rng_state ^= (rng_state << 5);
	return rng_state;
}


float rand_float(uint id)
{
    rng_state = id;
	float tmp = (1.0 / 4294967296.0);
	return float(rand_xorshift()) * tmp;
}


float RandomRange(uint seed, float min, float max)
{
    float random_float = rand_float(seed);
    random_float = random_float * (max - min) + min;
    return random_float;
}


float3 RodriguezRot(float3 v, float3 axis, float angle)
{
    float3 rotated_v = v * angle + cross(axis, angle) * sin(angle) + axis * dot(axis, v) * (1 - cos(angle));
    return rotated_v;
}


float3 CartFromSpherical(float theta, float phi)
{
    float x = cos(phi)*sin(theta);
    float z = sin(phi)*sin(theta);
    float y = cos(theta);

    return float3(x,y,z);
}

uint state;
// XORShift PRNG
uint rand()
{
    state ^= state << 13;
    state ^= state >> 17;
    state ^= state << 5;
    return state;
}

