# Control Board Barometer Tester

Run this program from the command line.

## Usage

```bash
test-baromter [-s <samples>] [-p <PressureChannelNum>] [-t <TempChanneNum>]
```

When running the program without parameters, the following defaults will be used:

* -s 100
* -p 135
* -t 136

## Building

Ensure that dotnetcore is installed on PC. Open a terminal and navigate to the source directory. Run the following command:

```bash
dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true
```

This will generate a single executable. The file is distributable