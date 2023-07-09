#include <iostream>
#include <stdio.h>
#include <cmath>
using namespace std;

static inline uint32_t rotl(const uint32_t x, int k);
uint32_t xorshift128plus(void);


// Output texture resolution
int const res_x = 4096;
int const res_y = 4096;
// Initialising noise texture array
float noise[res_x][res_y][3];
// Initialising random seeding array
static uint32_t s[4];


int main(void)
{
    // Create file in pointer in memory
    FILE *fp = NULL;

    // Initialise rng seeder with some big primes
    s[0] = 27538393;
    s[1] = 88067389;
    s[2] = 67901627;
    s[3] = 36402433;
    

    // Open file that will be written to
    fp = fopen("generated_noise_texture.csv", "w");
    
    // Write random 8 bit unsigned integers to file
    for (int i = 0; i < res_x; i++)
    {
        for (int j = 0; j < res_y; j++)
        {
            noise[i][j][0] = (float)xorshift128plus() / (float)4294967296;
            noise[i][j][1] = (float)xorshift128plus() / (float)4294967296;
            noise[i][j][2] = (float)xorshift128plus() / (float)4294967296;
            uint8_t red = round(noise[i][j][0] * 255);
            uint8_t green = round(noise[i][j][1] * 255);
            uint8_t blue = round(noise[i][j][2] * 255.0);
            if (j < res_y-1)
                fprintf(fp, "%u,%u,%u,", red, green, blue);   
            else
                fprintf(fp, "%u,%u,%u\n", red, green, blue);   
        }
    }  
    
    return 0;
}



///////////// Random Generators Below ///////////////



/*  Written in 2018 by David Blackman and Sebastiano Vigna (vigna@acm.org)

To the extent possible under law, the author has dedicated all copyright
and related and neighboring rights to this software to the public domain
worldwide. This software is distributed without any warranty.

See <http://creativecommons.org/publicdomain/zero/1.0/>.

This is xoshiro128+ 1.0, our best and fastest 32-bit generator for 32-bit
   floating-point numbers. We suggest to use its upper bits for
   floating-point generation, as it is slightly faster than xoshiro128**.
   It passes all tests we are aware of except for
   linearity tests, as the lowest four bits have low linear complexity, so
   if low linear complexity is not considered an issue (as it is usually
   the case) it can be used to generate 32-bit outputs, too.

   We suggest to use a sign test to extract a random Boolean value, and
   right shifts to extract subsets of bits.

   The state must be seeded so that it is not everywhere zero. */

static inline uint32_t rotl(const uint32_t x, int k) {
	return (x << k) | (x >> (32 - k));
}


uint32_t xorshift128plus()
{
    const uint32_t result = s[0] + s[3];
    const uint32_t t = s[1] << 9;

	s[2] ^= s[0];
	s[3] ^= s[1];
	s[1] ^= s[2];
	s[0] ^= s[3];

	s[2] ^= t;

	s[3] = rotl(s[3], 11);
    return result;
}