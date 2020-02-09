# SMAL

[![Build Status](https://travis-ci.org/mossseank/SMAL.svg?branch=master)](https://travis-ci.org/mossseank/SMAL)

Simple Managed Audio Library - no frills library for decoding and encoding audio in pure managed .NET.

The API is well documented, and examples/tutorials can be found on the [project wiki](https://github.com/mossseank/SMAL/wiki).

The goals of this project can be summarized as:

1. Produce a single, managed .NET library without any native dependencies.
2. Provide a simple and clear API for encoding and decoding audio streams in bulk and streaming mode.
3. Provide support for loading common audio file container formats (such as `RIFF` and `Ogg`).
4. Aim for high performance code using modern .NET and C# features (such as `Span<T>` and intrinsics).

In order to keep the implementation simple and straitforward, some limitations are introduced:

1. Audio streams will not support arbitrary seeking, only forward reading.
2. For the Wave format, only common encodings (e.g. PCM and IEEE) are supported.
3. All audio samples are decoded into signed 16-bit integer or normalized IEEE 32-bit float interleaved LPCM.
4. Minimal checks are done for file validity - it is assumed that files are well structured and standard compliant.
5. Metadata (artist, title, ect...) is not loaded, and will not be written by encoders. This is planned for a future version.

## Format Support

Support matrix for audio encoding formats:

|Format|Encoding|Decode|Encode|
|:----:|:------:|:----:|:----:|
|**Wave/RAW**|LPCM (16-bit)|v0.1|*Planned*|
||IeeeFloat (32-bit)|v0.1|*Planned*|
||A-Law|*Planned*|*Planned*|
||Mu-Law|*Planned*|*Planned*|
|**Vorbis**|Vorbis|*Planned*|*Planned*|
|**FLAC**|FLAC|*Planned*|*Planned*|
|**Opus**|Opus|*Planned*|*Planned*|
|**[RLAD](https://github.com/mossseank/SMAL/wiki/RLAD)**|Lossless|*Planned*|*Planned*|
||Lossy|*Planned*|*Planned*|

Support matrix for container file formats:

|Format|Encoding|Decode|Encode|
|:----:|:------:|:----:|:----:|
|**RIFF**|Wave/RAW|*Planned*|*Planned*|
|**Ogg**|Vorbis, FLAC, Opus|*Planned*|*Planned*|
|**RLAD**|RLAD|*Planned*|*Planned*|

This is the core set of planned formats. Pull requests are encouraged to add support for more formats, or to help implement or augment existing formats. Only formats that are unencumbered by restrictive licenses and patents will be accepted.
