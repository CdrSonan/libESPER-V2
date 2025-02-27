// Copyright (c) Johannes Klatt <johannes.klatt@nova-vox.org>.
// Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#include "fft.h"
#include "esper.h"

#include <math.h>
#include <stdlib.h>

float* _fft_recurse(float* data, int n, int step, int offset, int sign) {
    if (n == 1)
    {
        float* result = malloc(sizeof(float) * 2);
        result[0] = data[offset];
        result[1] = 0;
        return result;
    }
    if (n % 2 == 0)
    {
        return _fft_2(data, n, step, offset, sign);
    }
    if (n % 3 == 0)
    {
        return _fft_3(data, n, step, offset, sign);
    }
    else
    {
        return _dft(data, n, step, offset, sign);
    }
}

float* _fft_2(float* data, int n, int step, int offset, int sign)
{
    float* even = _fft_recurse(data, n / 2, step * 2, offset, sign);
    float* odd = _fft_recurse(data, n / 2, step * 2, offset + step, sign);
    float* result = malloc(sizeof(float) * 2 * n);
    for (int i = 0; i < n / 2; i++)
    {
        float angle = 2 * PI * i / n * sign;
        float real = cos(angle);
        float imag = sin(angle);
        result[2 * i] = even[2 * i] + real * odd[2 * i] - imag * odd[2 * i + 1];
        result[2 * i + 1] = even[2 * i + 1] + real * odd[2 * i + 1] + imag * odd[2 * i];
        result[2 * (i + n / 2)] = even[2 * i] - real * odd[2 * i] + imag * odd[2 * i + 1];
        result[2 * (i + n / 2) + 1] = even[2 * i + 1] - real * odd[2 * i + 1] - imag * odd[2 * i];
    }
    free(even);
    free(odd);
    return result;
}

float* _fft_3(float* data, int n, int step, int offset, int sign)
{
    float* even = _fft_recurse(data, n / 3, step * 3, offset, sign);
    float* odd1 = _fft_recurse(data, n / 3, step * 3, offset + step, sign);
    float* odd2 = _fft_recurse(data, n / 3, step * 3, offset + 2 * step, sign);
    float* result = malloc(sizeof(float) * 2 * n);
    for (int i = 0; i < n / 3; i++)
    {
        float angle = 2 * PI * i / n * sign;
        float real = cos(angle);
        float imag = sin(angle);
        result[2 * i] = even[2 * i] + real * odd1[2 * i] - imag * odd1[2 * i + 1] + real * odd2[2 * i] - imag * odd2[2 * i + 1];
        result[2 * i + 1] = even[2 * i + 1] + real * odd1[2 * i + 1] + imag * odd1[2 * i] + real * odd2[2 * i + 1] + imag * odd2[2 * i];
        result[2 * (i + n / 3)] = even[2 * i] - 0.5 * (real * odd1[2 * i] + imag * odd1[2 * i + 1] + real * odd2[2 * i] + imag * odd2[2 * i + 1]);
        result[2 * (i + n / 3) + 1] = even[2 * i + 1] - 0.5 * (-real * odd1[2 * i + 1] + imag * odd1[2 * i] - real * odd2[2 * i + 1] + imag * odd2[2 * i]);
        result[2 * (i + 2 * n / 3)] = even[2 * i] - 0.5 * (real * odd1[2 * i] - imag * odd1[2 * i + 1] + real * odd2[2 * i] - imag * odd2[2 * i + 1]);
        result[2 * (i + 2 * n / 3) + 1] = even[2 * i + 1] - 0.5 * (-real * odd1[2 * i + 1] - imag * odd1[2 * i] - real * odd2[2 * i + 1] - imag * odd2[2 * i]);
    }
    free(even);
    free(odd1);
    free(odd2);
    return result;
}

float* _dft(float* data, int n, int step, int offset, int sign)
{
    float* result = malloc(sizeof(float) * 2 * n);
    for (int i = 0; i < n; i++)
    {
        float real = 0;
        float imag = 0;
        for (int j = 0; j < n; j++)
        {
            float angle = 2 * PI * i * j / n * sign;
            real += data[offset + j * step] * cos(angle) - data[offset + j * step + 1] * sin(angle);
            imag += data[offset + j * step] * sin(angle) + data[offset + j * step + 1] * cos(angle);
        }
        result[2 * i] = real;
        result[2 * i + 1] = imag;
    }
    return result;
}

float* fft(float* data, int n)
{
    return _fft_recurse(data, n, 1, 0, 1);
}

float* ifft(float* data, int n)
{
    float* result = _fft_recurse(data, n, 1, 0, -1);
    for (int i = 0; i < n; i++)
    {
        result[2 * i] /= n;
        result[2 * i + 1] /= n;
    }
    return result;
}

float* rfft(float* data, int n)
{
    return _fft_recurse(data, n, 1, 0, -1);
}

float* irfft(float* data, int n)
{
    float* result = _fft_recurse(data, n, 1, 0, 1);
    for (int i = 0; i < n; i++)
    {
        result[2 * i] /= n;
        result[2 * i + 1] /= n;
    }
    return result;
}
