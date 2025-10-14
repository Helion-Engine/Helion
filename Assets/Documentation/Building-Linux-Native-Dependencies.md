# Building Linux Native Dependencies
We've made an effort to include appropriate versions of our native dependencies along with Helion.  However, since there are many different Linux-based operating systems, it is not possible for us to provide prebuilt binaries for all of them.

# Native Dependencies
We depend on the following native libraries:
1. [ZMusic (Helion fork)](https://github.com/Helion-Engine/ZMusic)
2. [SDL2](https://github.com/libsdl-org/SDL)
3. [GLFW](https://github.com/glfw/glfw)
4. [OpenAL-Soft](https://github.com/kcat/openal-soft)

Note that some of these have their own transitive dependencies.  For example, ZMusic requires libsndfile and libmpg123 for certain audio types such as FLAC, MP3, and so on.  However, these are installed by default on most "desktop" Linux distributions.

We provide our own `.so` files for use with "normal" .NET builds.  They are in `Client/Unmanaged/binary/linux-x64/`, except for GLFW, which is provided by the OpenTK package.  These `.so` files are used in "normal" .NET builds.  In Native AOT builds, which are fully precompiled, we statically link to all four of these libraries, to ensure that we run in a "known" configuration.  These files are in `Client/Unmanaged/lib/linux-x64`.  If you would like to build your own libraries from source, either because they do not work for you, or because you would prefer not to use binaries downloaded from the Internet, the following are the versions we're distributing:

1.  ZMusic (Helion fork): Use latest from GitHub
2.  SDL2:  Use `release-2.32.8` tag 
3.  GLFW:  Use `3.4` tag
4.  OpenAL-Soft:  Use `1.24.3` tag

If you are doing Native AOT builds, you may want to use Clang as your compiler, because the AOT build will use the Clang linker when available.

When building ZMusic, we suggest setting the `DYN_MPG123` and `DYN_SNDFILE` parameters to `OFF`.  This will cause the native loader to just fail and refuse to launch Helion if the libraries are unavailable, spewing an error to your terminal.  We believe this is preferable to silent failure when running the game.

All of these projects use CMake and may have other package dependencies, although these are generally pretty straightforward (for example, ZMusic requires libmpg123-dev and libsndfile-dev).

When building the `.a` files for use with Native AOT linking, we generally use the following CMake parameters as a baseline:
`-DCMAKE_CXX_COMPILER=clang -DCMAKE_C_COMPILER=clang -DBUILD_SHARED_LIBS=OFF -DLIBTYPE=STATIC`

# Library-specific notes
## GLFW
Note that building GLFW for AOT use may require you to add the following function:

```
GLFWAPI void* glfwGetWin32Window(GLFWwindow* handle)
{
    return NULL;
}
```

This is, unfortunately, necessitated by library code (OpenTK) we do not control.

## OpenAL-Soft
Building OpenAL-Soft might require a newer version of CMake than some older Linux distributions provide.  On our Ubuntu 22.04-based build environment, we needed to follow the workaround described here, which involved adding Kitware's APT repository:

https://askubuntu.com/questions/355565/how-do-i-install-the-latest-version-of-cmake-from-the-command-line

Note also that OpenAL-Soft bundles some utilities and example programs that we do not need.  If you encounter a build error when building the static library, you can ignore this, so long as the `libopenal.a` file is created in your target directory.
