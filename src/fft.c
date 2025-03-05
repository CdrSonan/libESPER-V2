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
        result[0] = data[2 * offset];
        result[1] = data[2 * offset + 1];
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
    float* result = malloc(sizeof(float) * 2 * n);
    float* sub1 = _fft_recurse(data, n / 3, step * 3, offset, sign);
    float* sub2 = _fft_recurse(data, n / 3, step * 3, offset + step, sign);
    float* sub3 = _fft_recurse(data, n / 3, step * 3, offset + 2 * step, sign);

    for (int k = 0; k < n / 3; k++)
    {
        float angle1 = 2 * PI * k / n * sign;
        float angle2 = 4 * PI * k / n * sign;

        float real1 = cos(angle1);
        float imag1 = sin(angle1);
        float real2 = cos(angle2);
        float imag2 = sin(angle2);

        float sub2_real = real1 * sub2[2 * k] - imag1 * sub2[2 * k + 1];
        float sub2_imag = real1 * sub2[2 * k + 1] + imag1 * sub2[2 * k];
        float sub3_real = real2 * sub3[2 * k] - imag2 * sub3[2 * k + 1];
        float sub3_imag = real2 * sub3[2 * k + 1] + imag2 * sub3[2 * k];

        result[2 * k] = sub1[2 * k] + sub2_real + sub3_real;
        result[2 * k + 1] = sub1[2 * k + 1] + sub2_imag + sub3_imag;
        result[2 * (k + n / 3)] = sub1[2 * k] + sub2_real * (-0.5) - sub2_imag * sqrt(3) / 2 + sub3_real * (-0.5) + sub3_imag * sqrt(3) / 2;
        result[2 * (k + n / 3) + 1] = sub1[2 * k + 1] + sub2_real * sqrt(3) / 2 + sub2_imag * (-0.5) + sub3_real * (-sqrt(3) / 2) + sub3_imag * (-0.5);
        result[2 * (k + 2 * n / 3)] = sub1[2 * k] + sub2_real * (-0.5) + sub2_imag * sqrt(3) / 2 + sub3_real * (-0.5) - sub3_imag * sqrt(3) / 2;
        result[2 * (k + 2 * n / 3) + 1] = sub1[2 * k + 1] + sub2_real * (-sqrt(3) / 2) + sub2_imag * (-0.5) + sub3_real * sqrt(3) / 2 + sub3_imag * (-0.5);
    }

    free(sub1);
    free(sub2);
    free(sub3);
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
            float angle = -2 * PI * i * j / n * sign;
            real += data[offset + 2 * j * step] * cos(angle) - data[offset + 2 * j * step + 1] * sin(angle);
            imag += data[offset + 2 * j * step] * sin(angle) + data[offset + 2 * j * step + 1] * cos(angle);
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
