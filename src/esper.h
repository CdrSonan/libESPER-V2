// Copyright (c) Johannes Klatt <johannes.klatt@nova-vox.org>.
// Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#define PI 3.14159265358979323846

#define ASSERT(condition, message) if (!(condition)) { fprintf(stderr, "Assertion failed: %s\n", message); exit(1); }

#define ESPER_AUDIO float //to distinguish float* representing ESPER audio data from other float*
