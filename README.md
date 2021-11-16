##	Checksum Verification Tool

![Alt text](/screenshot.png?raw=true "screenshot")
![Alt text](/logs.png?raw=true "logs")

This program allows you to calculate the checksums of all files in a directory and subdirectories, and then check them and find modified or deleted files. In the source code, you can change the way the checksum is calculated by different algorithms and libraries (CRC32, SHA1, xxHash).

### Usage
```
ChecksumVerification.exe --help
```

### Packages
```
- CommandLineParser (2.8.0)
- Crc32.NET (1.2.0)
- K4os.Hash.xxHash (1.0.6)
- Standart.Hash.xxHash (3.1.0)
- HashDepot (2.0.3)
```

*This software is distributed "as is", for demonstration purpose, with no warranty expressed or implied, and no guarantee for accuracy or applicability to any other purpose.*
