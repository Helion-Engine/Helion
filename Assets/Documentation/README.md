# Installation

1. Extract the contents of this archive to a directory of your choice.
2. Obtain one or more of the official IWADs (Doom, Doom II, Plutonia, TNT, etc.) and (pick one):
  a. Copy them to the same directory as Helion
  b. Run Helion.exe (or just "./Helion", on Linux) with the -iwad parameter, followed by the path to the IWAD you want to use
  c. Configure your Doom launcher of choice to pass the -iwad parameter
  d. Launch the Helion executable, let it fail because it cannot find an IWAD, then edit the `files.directories = [".", "wads"]` line to include the directory that contains your IWADs.
3. Run the Helion executable (Helion.exe on Windows, ./Helion on Linux) to play.

# Common issues

## Windows

1. If you have downloaded a file named Helion-<version>-win-x64.zip, you must install a Microsoft .NET 8.x runtime.  Please see https://dotnet.microsoft.com/en-us/download/dotnet/8.0 .  If you have downloaded a file named Helion-<version>-win-x64_SelfContained.zip, then this is not required.
2. One of our dependencies, OpenTK, needs the Microsoft Visual C Runtime on Windows installs.  If Helion appears to simply _not launch_, please consult errorlog.txt.  If it mentions being unable to load MSVCRT140.dll (or similar), please install the latest redistributable package: https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2015-2017-2019-and-2022

## Linux

1.  While we have included a version of LibFluidSynth that _should_ work on common Linux distributions, if it doesn't work, you may need to use your package manager to install a different one (and possibly delete the ".so" file we've included).  You can also run with the `-nomusic` to disable music support.
2.  Similar to the Windows ZIP files, the standard Helion-<version>-linux-x64.zip file requires a .NET 8.x runtime.  See https://learn.microsoft.com/en-us/dotnet/core/install/linux .  The Helion-<version>-linux-x64_SelfContained.zip file provides its own self-contained runtime and does not require this.
3.  The audio libraries we are currently using do not provide support for MP3 or Ogg Vorbis audio playback on Linux.

# Contact Us

If you encounter issues using Helion and would like to report bugs you've encountered, you can:
1. Visit our thread on the Doomworld forums at https://www.doomworld.com/forum/topic/132153-helion-c-0940-824-goodbye-bsp-tree-rendering/
2. Report issues via GitHub at https://github.com/Helion-Engine/Helion/issues

