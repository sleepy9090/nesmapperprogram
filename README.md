# nesmapperprogram
Program to read NES ROMs based on iNES or NES 2.0 Header and Split ROMs for EEPROM Burning

NES Mapper Reader / Rom Fixer / Rom Splitter
Programmed by: Shawn M. Crawford [sleepy]
Last Update: February 19th, 2017
Latest Version: 2.0
Coded in C#, Requires .NET 3.5
 ----

This utility will:

    read NES ROM info based on the iNES Header (mapper, mirroring, etc)
    read NES ROM info based on the NES 2.0 Header
    clean a rom header by blanking out bytes 7 - 15
    remove the iNES header (to prep for burning to eprom)
    output the relevant BIN as a non-headered ROM
    output the relevant CHR/PRG bin files to burn with eprom burner for dev carts
    output the relevant CHR/PRG bin files to burn with eprom burner for Vs. Unisystem

It’s best to use the auto split option unless you know the PRG and CHR are incorrect sizes, since this info is based on the iNES header.

Example: Dig Dug (J).nes - see attached screen nesprog2.png
Results after Prep:
Remove 16 Bytes NES Header: Dig Dug (J).nes.bin (24576 bytes)
CHR dump: Dig Dug (J).nes.chr.bin (8192 bytes)
PRG dump: Dig Dug (J).nes.prg.bin (16384 bytes)

Requires .Net 3.5 Framework

 ----
2.0 February 19th, 2017
* Optimized code
* Added support for NES 2.0 Headers
* Added 8kb split option for Vs. Unisystem ROMs
* Open sourced, added GPL license

 ----
1.1 January 29th, 2017
* GUI cleaned up and prettified
* Now auto analyzes ROM
* Fix bug in CHR extraction
* Revised/Optimized several algorithms in code
* Fix many bugs in gui and button functionality when changing values manually
* Fix autosplit ROMs to recalculate if switching between auto and manual
* Updated Mapper analysis with more mappers.
* Updated error handling
* Updated messages

 ----
1.0  December 22nd, 2008
* initial release

 ----
Thanks:
* Ben Foster - testing Vs. Unisystem features
* http://nesdev.com/ - NESDEV Wiki and NESDEV BBS for docs and information
* http://romhacking.net/ - hosting


