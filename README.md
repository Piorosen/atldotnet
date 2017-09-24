# Audio Tools Library (ATL) for .NET

## Status / master branch

__Linux and OSX Mono Build__ : [![Build Status](https://travis-ci.org/Zeugma440/atldotnet.svg?branch=master)](https://travis-ci.org/Zeugma440/atldotnet) (powered by Travis CI)


__Windows .NET Build__ : [![Build status](https://ci.appveyor.com/api/projects/status/s4y0e3g6fxncdhi6/branch/master?svg=true)](https://ci.appveyor.com/project/Zeugma440/atldotnet/branch/master) (powered by AppVeyor)


__Code coverage__ : [![codecov](https://codecov.io/gh/Zeugma440/atldotnet/branch/master/graph/badge.svg)](https://codecov.io/gh/Zeugma440/atldotnet) (powered by CodeCov)

__Optimized with__ : [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) and [CodeTrack](http://www.getcodetrack.com/)


## What is ATL .NET ?

Audio Tools Library .NET is the C# port of [the original ATL](http://mac.sourceforge.net/atl/), written in Pascal by the MAC Team (Jürgen Faul, Gambit and contributors).

It is aimed at giving C# developers a native, portable and easy-to-use library to access and read data from various digital audio formats.

As a showcase, I have used ATL.NET as a cornerstone to build [Ethos Cataloger](https://trello.com/b/ZAzRjbXZ/ethos-cataloger), a digital music cataloging software written entirely in C#.


## What is NOT ATL .NET ?

Audio Tools Library .NET is not

* a standalone application : it is a library aimed at being used by developers to build software

* an audio music player : it gives access to various properties and metadata (see below for the comprehensive list), but does not process audio data into an audible signal


## Why open source ?

ATL has been open source since its creation. The ATL 2.3 source written in Pascal language is still out there on Sourceforge !

By publicly sharing the result of their work, the MAC team has helped many developers to gain tremendous time in creating audio tools.

As a fellow audiophile and developer, I'm proudly extending and improving their initial contribution to the open source community.


## Why would I want to use ATL while TagLib is out there ?

* ATL is a __fully native C# implementation__, which makes portability trivial if your app is already based on .NET or Mono frameworks

* ATL features a __flexible logging system__ which allows you to catch and record audio file reading/writing incidents into your app

* ATL supports __more audio formats than TagLib, including video game audio formats (SPC, PSF)__

* ATL supports __Playlists and Cuesheets__


## How to use it ?  Which platforms and .NET/Mono versions does ATL run on ?

The ATL library runs on .NET 3.0+ / Mono 2.0+

ATL unit tests run on .NET 4.5+

The library and its tests have been maintained on Visual Studio Express 2012 and 2015

Please refer to the [Code snippets section of the Documentation](Usage-_-Code-snippets) for quick usage


## What kind of data can ATL actually read ? From which formats ?

* Supported audio formats and tagging standards :

NB1 : Empty cells mean "not applicable for this audio format"

NB2 : All metadata is read according to Unicode/UTF-8 encoding when applicable, which means any "foreign" character (japanese, chinese, cyrillic...) __will__ be recognized and displayed properly

R= Read

W= Write


Audio format | Extensions | ID3v1.0-1.1 support | ID3v2.2-2.4 support | APEtag 1.0-2.0 support | Format-specific tagging support
---|---|---|---|---|---
Advanced Audio Coding, Apple Lossless (ALAC) | .AAC, .MP4, .M4A | R/W | R/W | R/W | R/W
Audio Interchange File Format | .AIF, .AIFF, .AIFC |  | R/W |  | R/W
Digital Theatre System | .DTS |  |  |  | 
Direct Stream Digital | .DSD, .DSF |  | R/W |  | 
Dolby Digital | .AC3 |  |  | R/W | 
Extended Module | .XM |  |  |  | R (2)
Free Lossless Audio Codec | .FLAC |  | R/W |  | R/W
Impulse Tracker | .IT |  |  |  | R/W (2)
Musical Instruments Digital Interface | .MID, .MIDI |  |  |  | R/W (1)
Monkey's Audio | .APE | R/W | R/W | R/W | 
MPEG Audio Layer | .MP1, .MP2, .MP3 | R/W | R/W | R/W | |
MusePack / MPEGplus|.MPC, .MP+|R/W|R/W|R/W| |
Noisetracker/Soundtracker/Protracker|.MOD| | | |R/W (2)|
OGG : Vorbis, Opus|.OGG, .OPUS| | | |R/W|
OptimFROG|.OFR, .OFS|R/W|R/W|R/W| |
Portable  Sound Format|.PSF, .PSF1, .PSF2, .MINIPSF, .MINIPSF1, .MINIPSF2, .SSF, .MINISSF, .DSF, .MINIDSF, .GSF, .MINIGSF, .QSF, .MINISQF| | | |R/W|
ScreamTracker|.S3M| | | |R/W (2)|
SPC700 (Super Nintendo Sound files)|.SPC| | | |R/W|
Toms' losslesss Audio Kompressor|.TAK| | |R/W| |
True Audio|.TTA|R/W|R/W|R/W| |
TwinVQ|.VQF| | | |R/W|
PCM (uncompressed audio)|.WAV|R/W| | | |
WavPack|.WV| | |R/W| |
Windows Media Audio/Advanced Systems Format|.WMA,.ASF| | | |R/W|


(1) : MIDI meta events appear on the track's Comment field

(2) : all sample names appear on the track's Comment field. Track title only is editable.


* Detected fields
	* Audio data (from audio data) : Bitrate, Sample rate, Duration, VBR, Codec family
	* Metadata (from tags) : Title, Artist, Composer, Conductor, Description, Comment, Genre, Track number, Disc number, Year, Album, Rating, Publisher, Copyright, Original album, Original artist, Embedded pictures

* Supported playlists formats : ASX, B4S, FPL (experimental), M3U, M3U8, PLS, SMIL (including WPL and ZPL), XSPF

* Supported Cuesheets formats : CUE


## What is the roadmap of ATL.NET ?

* Better support of ID3v2 tags with multiple Comments (each having a specific comment description, which is currently not supported)
* Compatibility with .NET Core (some emulation of System.Drawing needed here and there)
* Support for __more video game-specific music formats__ (.GYM, .VGM)
* Connectors to __other library file formats__ (iTunes)


## Does ATL.NET include code authored by other people ?

ATL.NET is based on :

* Audio Tools Library 2.3  by Jurgen Faul, Mattias Dahlberg, Gambit, MaDah and Erik Stenborg (code ported from Pascal to C# and refactored)

* MIDI class 1.5 by Valentin Schmidt & Michael Mlivoncic (code ported from PHP to C# and refactored)

* CueSharp 0.5 by Wyatt O'Day (original C# class wrapped in CatalogDataReaders)
