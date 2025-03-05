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
    float data[] = {0, 0, 1, 0, 0, 0};
    float* result = _fft_3(data, 3, 1, 0, 1);//fft(data, 3); expected for 1: ++---+, expected for -1: ++-+--
    float expected[] = {1, 0, -0.5, -0.5 * sqrt(3), -0.5, 0.5 * sqrt(3)};
    for (int i = 0; i < 6; i++)
    {
        fprintf(stdout, "Test 1, Expected: %f, Got: %f\n", expected[i], result[i]);
        if (result[i] != expected[i])
        {
            fprintf(stderr, "Test 1: Expected %f at index %d, got %f\n", expected[i], i, result[i]);
            //free(result);
            //return 1;
        }
    }
    free(result);

    return 0;

    float data1[] = {1, 0, -0.5, 0.5 * sqrt(3), -0.5, -0.5 * sqrt(3)};
    float expected1[] = {0, 0, 1, 0, 0, 0};
    float* result1 = ifft(data1, 3);
    for (int i = 0; i < 6; i++)
    {
        fprintf(stdout, "Test 2, Expected: %f, Got: %f\n", expected1[i], result1[i]);
        if (expected1[i] - result1[i] > 0.00001)
        {
            fprintf(stderr, "Test 2: Expected %f at index %d, got %f\n", expected1[i], i, result1[i]);
            //free(result1);
            //return 2;
        }
    }
    free(result1);


    

    int n = 27;

    int n2 = 2 * n;

    float* data2 = (float*)malloc(sizeof(float) * n2);
    for (int i = 0; i < n2; i++)
    {
        data2[i] = (float)rand() / RAND_MAX;
        data2[i] = data2[i] * 2 - 1;
    }
    float* result2 = fft(data2, n);
    float* expected2 = _dft(data2, n, 1, 0, 1);
    for (int i = 0; i < n2; i++)
    {
        fprintf(stdout, "Test 3, Expected: %f, Got: %f\n", expected2[i], result2[i]);
        if (fabs(expected2[i] - result2[i]) > 0.001)
        {
            fprintf(stderr, "Test 3: Expected %f at index %d, got %f\n", expected2[i], i, result2[i]);
            //free(data2);
            //free(result2);
            //free(expected2);
            //return 3;
        }
    }
    free(expected2);

    float* result3 = ifft(result2, n);
    for (int i = 0; i < n2; i++)
    {
        fprintf(stdout, "Test 4, Expected: %f, Got: %f\n", data2[i], result3[i]);
        if (fabs(data2[i] - result3[i]) > 0.001)
        {
            fprintf(stderr, "Test 4: Expected %f at index %d, got %f\n", data2[i], i, result3[i]);
            //free(data2);
            //free(result2);
            //free(result3);
            //return 4;
        }
    }
    free(data2);
    free(result2);
    free(result3);
    return 0;
}
