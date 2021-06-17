# Assembly Tools
Utils made for working with C# .NET assemblies.

## History
I originally made this because I had modded a game by modifying DLL files and wanted to share it, but I obviously couldn't just share the dll files. This was made to be an installer so I can just distribute this and a patch file and it works, however it has grown beyond original scope.

## Goals
 - Easy to use utilities for modifying and saving assemblies
 - Allow to be packaged as just an installer
 - Make updating mod easier after original DLL changes.
 
## Current Status
Nowhere near any of the previously mentioned goals. I think (mostly) final saving is done but a lot of work has to be done on loading and testing.
Currently it supports saving types, fields, and methods but I believe properties and events are separate, so I will have to support those.
End goal is a command-line utility or library, but currently it is a early testing phase so testing methods are just called from `main()`.
There are also many minor inconsistencies in where methods are and what parameters and overloads they have which will also have to be fixed. 
