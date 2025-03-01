// Copyright (c) Johannes Klatt <johannes.klatt@nova-vox.org>.
// Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#include "fft.h"
#include "esper.h"

#include <math.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>

int main(int argc, char** argv)
{
    char* mode = argv[1];
    if (stricmp(mode, "fft") == 0)
    {
        return test_fft();
    }
    // add other tests once implemented
    fprintf(stderr, "Unknown mode: %s\n", mode);
    return 1;
}

int test_fft()
{
    float data[] = {1, 0, 0, 0, 0, 0, 0, 0};
    float* result = fft(data, 4);
    float expected[] = {1, 0, 1, 0, 1, 0, 1, 0};
    for (int i = 0; i < 8; i++)
    {
        if (result[i] != expected[i])
        {
            fprintf(stderr, "Test 1: Expected %f at index %d, got %f\n", expected[i], i, result[i]);
            free(result);
            return 1;
        }
    }
    free(result);

    float data1[] = {0, 0, 0, 0, 0, 0, 1, 0};
    float expected1[] = {1, 0, 0, -1, -1, 0, 0, 1};
    float* result1 = fft(data1, 4);
    for (int i = 0; i < 8; i++)
    {
        if (expected1[i] - result1[i] > 0.00001)
        {
            fprintf(stderr, "Test 2: Expected %f at index %d, got %f\n", expected1[i], i, result1[i]);
            free(result1);
            return 2;
        }
    }
    free(result1);

    float* data2 = (float*)malloc(sizeof(float) * 256);
    for (int i = 0; i < 256; i++)
    {
        data2[i] = (float)rand() / RAND_MAX;
        data2[i] = data2[i] * 2 - 1;
    }
    float* result2 = fft(data2, 128);
    float* expected2 = _dft(data2, 128, 1, 0, 1);
    for (int i = 0; i < 256; i++)
    {
        if (fabs(expected2[i] - result2[i]) > 0.001)
        {
            fprintf(stderr, "Test 3: Expected %f at index %d, got %f\n", expected2[i], i, result2[i]);
            free(data2);
            free(result2);
            free(expected2);
            return 3;
        }
    }
    free(expected2);

    float* result3 = ifft(result2, 128);
    for (int i = 0; i < 256; i++)
    {
        if (fabs(data2[i] - result3[i]) > 0.001)
        {
            fprintf(stderr, "Test 4: Expected %f at index %d, got %f\n", data2[i], i, result3[i]);
            free(data2);
            free(result2);
            free(result3);
            return 4;
        }
    }
    free(data2);
    free(result2);
    free(result3);
    return 0;
}
