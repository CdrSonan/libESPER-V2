# libESPER-V2

Second version of the ESPER library for speech parametrization, modification and recovery.

## Usage

A typical workflow using ESPER consists of three steps:

- transform speech and singing waveforms to the ESPER format
- apply powerful modifications and effects enabled by the ESPER format
- transform the ESPER-formatted audio back to a waveform

Additionally, several other data paths are available, which enable libESPER-V2 to be used in a variety of more complex
applications:

```mermaid
graph LR;
    W1[Waveform]-->F([Forward]);
    F-->E1[ESPERAudio]
    W1[Waveform]-->Fa([Approximate Forward]);
    Fa-->E1[ESPERAudio]
    E2[ESPERAudio]-->B([Backward]);
    B-->W2[Waveform];

    E1<-->Cmp[Compressed ESPERAudio];
    E1<-->Bin[Binary String];
    Cmp<-->Bin;
    
    E1-->Fx([Effects]);
    Fx-->E2;

    E1-->Ps([Pitch shift]);
    Ps-->E2;

    E1-->St([Stretch]);
    St-->E2;

    E1-->Cmb([Combine]);
    E1-->Cmb;
    Cmb-->E2;

    E1-->Cut([Cut]);
    Cut-->E2;
    Cut-->E2;
```

## Features

TODO.

### Pitch Shifting

### Sample Stretching

### Effects

### Compression

### Others