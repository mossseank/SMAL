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

## Format Support

Support matrix for audio encoding formats:

|Format|Decode|Encode|
|:----:|:----:|:----:|
|**Wave<sup>1</sup>**|*Planned*|*Planned*|
|**Vorbis**|*Planned*|*Planned*|
|**FLAC**|*Planned*|*Planned*|
|**Opus**|*Planned*|*Planned*|
|**[RLAD](https://github.com/mossseank/SMAL/wiki/RLAD)**|*Planned*|*Planned*|

Support matrix for container file formats:

|Format|Encoding|Decode|Encode|
|:----:|:------:|:----:|:----:|
|**RIFF**|Wave<sup>1</sup>|*Planned*|*Planned*|
|**Ogg**|Vorbis, FLAC, Opus|*Planned*|*Planned*|
|**RLAD**|RLAD|*Planned*|*Planned*|

<sup>1</sup><small>*Wave is a file format that supports many encodings - only the most common ones are implemented.*</small>

This is the core set of planned formats. Pull requests are encouraged to add support for more formats, or to help implement or augment existing formats. Only formats that are unencumbered by restrictive licenses and patents will be accepted.
