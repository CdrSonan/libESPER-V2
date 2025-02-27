// Copyright (c) Johannes Klatt <johannes.klatt@nova-vox.org>.
// Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

float* _fft_recurse(float* data, int n, int step, int offset, int sign);
float* _fft_2(float* data, int n, int step, int offset, int sign);
float* _fft_3(float* data, int n, int step, int offset, int sign);
float* _dft(float* data, int n, int step, int offset, int sign);
float* fft(float* data, int n);
float* ifft(float* data, int n);
float* rfft(float* data, int n);
float* irfft(float* data, int n);
