/*  Written in 2019 by David Blackman and Sebastiano Vigna (vigna@acm.org)

To the extent possible under law, the author has dedicated all copyright
and related and neighboring rights to this software to the public domain
worldwide. This software is distributed without any warranty.

See <http://creativecommons.org/publicdomain/zero/1.0/>. */


/* This is xoshiro128++ 1.0, one of our 32-bit all-purpose, rock-solid
   generators. It has excellent speed, a state size (128 bits) that is
   large enough for mild parallelism, and it passes all tests we are aware
   of.

   For generating just single-precision (i.e., 32-bit) floating-point
   numbers, xoshiro128+ is even faster.

   The state must be seeded so that it is not everywhere zero. */

uint s[4];

void SeedXorPRNG(uint seed0, uint seed1, uint seed2, uint seed3) {
    s[0] = seed0;
    s[1] = seed1;
    s[2] = seed2;
    s[3] = seed3;
}


uint rotl(const uint x, int k) {
	return (x << k) | (x >> (32 - k));
}



uint Next() {
	const uint result = rotl(s[0] + s[3], 7) + s[0];

	const uint t = s[1] << 9;

	s[2] ^= s[0];
	s[3] ^= s[1];
	s[1] ^= s[2];
	s[0] ^= s[3];

	s[2] ^= t;

	s[3] = rotl(s[3], 11);

	return result;
}


/* This is the jump function for the generator. It is equivalent
   to 2^64 calls to next(); it can be used to generate 2^64
   non-overlapping subsequences for parallel computations. */

void Jump() {
    static const uint JUMP[] = { 0x8764000b, 0xf542d2d3, 0x6fa035c3, 0x77f2db5b };

    uint s0 = 0;
    uint s1 = 0;
    uint s2 = 0;
    uint s3 = 0;

    for (int i = 0; i < 4; i++) {
        for (int b = 0; b < 32; b++) {
            if ((JUMP[i] & (1u << b)) != 0) {
                s0 ^= s[0];
                s1 ^= s[1];
                s2 ^= s[2];
                s3 ^= s[3];
            }
            Next();
        }
    }

    s[0] = s0;
    s[1] = s1;
    s[2] = s2;
    s[3] = s3;
}



/* This is the long-jump function for the generator. It is equivalent to
   2^96 calls to next(); it can be used to generate 2^32 starting points,
   from each of which jump() will generate 2^32 non-overlapping
   subsequences for parallel distributed computations. */

void LongJump() {
	static const uint LONG_JUMP[] = { 0xb523952e, 0x0b6f099f, 0xccf5a0ef, 0x1c580662 };

	uint s0 = 0;
	uint s1 = 0;
	uint s2 = 0;
	uint s3 = 0;
	for(int i = 0; i < 4; i++)
		for(int b = 0; b < 32; b++) {
			if (LONG_JUMP[i] & 1 << b) {
				s0 ^= s[0];
				s1 ^= s[1];
				s2 ^= s[2];
				s3 ^= s[3];
			}
			Next();	
		}
		
	s[0] = s0;
	s[1] = s1;
	s[2] = s2;
	s[3] = s3;
}