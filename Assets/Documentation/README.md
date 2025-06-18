# Installation

1. Extract the contents of this archive to a directory of your choice.
2. Ensure that you've placed your IWADs (files that contain Doom game data) somewhere we can find them.
    1. Obtain one or more of the IWADs we support.  Doom, Doom II, Plutonia, and TNT are available in the [Doom + Doom II](https://store.steampowered.com/app/2280/DOOM__DOOM_II/) package on Steam for a reasonably low price.  We also support Doom1.WAD [from the shareware release](https://github.com/Doom-Utils/shareware-collection), and [Freedoom](https://freedoom.github.io/download.html).
    2. If you've purchased the games on Steam, you should just be able to launch Helion, and we'll automatically find the data files.  If that doesn't work, or your files are in some other location, you can (PICK ONE):
    3. Copy the IWADs to the same directory as Helion
    4. Run Helion.exe (or just "./Helion", on Linux) with the -iwad parameter, followed by the path to the IWAD you want to use
    5. Configure your Doom launcher of choice to pass the -iwad parameter
    6. Set values for the `DOOMWADDIR` or `DOOMWADPATH` environment variables
    7. Launch the Helion executable using one of the other methods described above, then edit the `files.directories = [".", "wads"]` line in `config.ini` to include the directory that contains your IWADs.
4. Run the Helion executable (Helion.exe on Windows, ./Helion on Linux) to play.

# User Data Folder / Portable Mode

By default, user data is stored in `<user folder>\Saved Games\Helion` on Windows and (most commonly) `~/.config/helion` on Linux.

Helion will run in portable mode (user data is stored in the application folder) if it finds a `config.ini` file in its folder, or if `-portable` is passed as a launch parameter.

# Launch Parameters

These parameters can be added to a shortcut or in your preferred Doom launcher:

Option                        | Description
----------------------------- | -----------
-iwad [iwad]                  | Loads an IWAD
-file [pwad1] [pwad2] ...     | Loads PWAD(s)
-deh [file]                   | Loads a DEH file
-portable                     | Runs in portable mode
-config [file]                | Uses a specific config file
-savedir [folder]             | Saves to a specific folder (default is the user data folder)
-loadgame [savefile]          | Loads a save game
-skill [level]                | Sets the skill level, 1-5 (5 = Nightmare)
-warp [map]                   | Loads directly into a specified map
+map [map]                    | Same as above
-record [demofile]            | Records a demo file
-playdemo [demofile]          | Plays a demo file
-pistolstart                  | Weapons are not kept between levels
+cheats [cheat1] [cheat2] ... | Enables specific cheats (e.g. IDKFA)
+sv_fastmonsters              | Monsters are faster, like in Nightmare
-nomonsters                   | No monsters will be spawned
-levelstat                    | Writes gameplay stats to file
-nomusic                      | Disables music
+glversion [version]          | Forces an OpenGL version (e.g. +glversion 33 forces OpenGL 3.3)
-loglevel [level]             | Sets which log levels are logged, can be used for debugging
-log [file]                   | Saves log output to file, can be used for debugging
-logprofiler [file]           | Saves performance data to file
+setpos [x,y,z]               | Sets the player's start position
+setangle [deg]               | Sets the player's start angle
+setpitch [deg]               | Sets the player's start pitch

# Console Commands

Helion's console can be opened with the tilde key (~) and features tab-completion. Here is a non-exhaustive list of
useful commands:

Command                         | Description
--------------------------------|------------
[option] [value]                | Changes a config option (e.g. hud.showfps 1)
[option]                        | Prints the value of a config option
toggle [option] [value1] [...]  | Cycles a config option through a list of values (e.g. toggle audio.musicvolume 1 0.5 0)
chasecam                        | Toggles a 3rd person camera and pauses the game
iddqd, idkfa, etc.              | Applies the corresponding Doom cheat
fly                             | Toggles fly mode
god                             | Toggles god mode
noclip                          | Toggles noclip mode
findexits                       | Highlights exits on the automap
findkeys                        | Highlights keys on the automap
findkeylines                    | Highlights key lines on the automap
marksecrets                     | Toggles secret highlighting on the automap
markspecials                    | Toggles ingame and automap highlighting of specials (shown when crossing a line special or pressing use on a sector triggered by one)
map [mapname]                   | Changes the map (health/inventory are reset)
exitlevel                       | Exits to the next level (health/inventory are preserved)
exitlevelsecret                 | Exits to the next level, using the secret exit if one exists
bind [key] [command]            | Sets a keybind, replacing any existing for the key
bindadd [key] [command]         | Adds an additional keybind for a key
unbind [key]                    | Removes keybinds for a key
listmaps                        | Lists available maps
printmap                        | Prints the current map name
printgame                       | Prints the current game/WAD name

# Common issues

## Windows

1. If you have downloaded a file named `Helion-<version>-win-x64.zip`, you must install a Microsoft .NET 9.x runtime.  Please see https://dotnet.microsoft.com/en-us/download/dotnet/9.0 .  If you have downloaded a file named `Helion-<version>-win-x64_SelfContained.zip`, then this is not required.
2. One of our dependencies, OpenTK, needs the Microsoft Visual C Runtime.  If Helion appears to simply _not launch_, please consult errorlog.txt in the user data folder. If it mentions being unable to load MSVCRT140.dll (or similar), please install the latest redistributable package: https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2015-2017-2019-and-2022

## Linux

1.  We have provided binaries for libzmusic and libfluidsynth, which are used for music playback.  If these are not compatible with the specific version of Linux you are using, you may need to obtain or build your own.  If you choose to do so, you can either install these dependencies and delete the ones provided, or overwrite the provided .so files with your own; either should work.  You can also run with `-nomusic` to disable music support.
2.  Similar to the Windows ZIP files, the standard `Helion-<version>-linux-x64.zip` file requires a .NET 9.x runtime.  See https://learn.microsoft.com/en-us/dotnet/core/install/linux .  The `Helion-<version>-linux-x64_SelfContained.zip` file provides its own self-contained runtime and does not require this.
3.  Helion requires OpenGL (GLFW) and OpenAL runtime components.  You must install these if they are not present, otherwise Helion will fail to start.  On a barebones Ubuntu install, OpenAL may need to be installed (`sudo apt-get install libopenal1`)  Additionally, the music library (ZMusic) requires libsndfile and libmpg123.  These are usually already installed by major Linux distributions (including Ubuntu) but may need to be installed manually on less common configurations.  The included music libraries were built on Ubuntu 22.04.
4.  While we _do_ test with Linux environments, we are limited in how many different distributions we can test.  You are most likely to have success with distributions based on the latest Ubuntu LTS branch (24.04 at the time of this writing); other versions may work if you build your own native libraries.

# Contact Us

If you encounter issues using Helion and would like to report bugs you've encountered, you can:
1. Visit our thread on the Doomworld forums at https://www.doomworld.com/forum/topic/132153-helion-c-0940-824-goodbye-bsp-tree-rendering/
2. Report issues via GitHub at https://github.com/Helion-Engine/Helion/issues

