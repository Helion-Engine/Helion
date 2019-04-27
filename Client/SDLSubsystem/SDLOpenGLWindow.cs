using Helion.Util;
using SDL2;
using System;

namespace Helion.Client.SDLSubsystem
{
    public class SDLOpenGLWindow : IDisposable
    {
        private const SDL.SDL_WindowFlags WindowFlags = SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                                                        SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN |
                                                        SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL;

        private bool disposed = false;
        private IntPtr windowPtr = IntPtr.Zero;
        private IntPtr glContextPtr = IntPtr.Zero;

        public SDLOpenGLWindow()
        {
            // Apparently GL attributes must be set before creating the window.
            // https://stackoverflow.com/questions/42013957/error-creating-opengl-context-with-sdl-badmatch
            SetGLAttributes();
            CreateWindow();
            CreateGLContext();
            SetSDLGLAttributes();
        }

        ~SDLOpenGLWindow()
        {
            Dispose(false);
        }

        private void SetGLAttributes()
        {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ALPHA_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

            // TODO: Note that this breaks on some drivers (ex: Nouveau). The solution
            //       is to create a dummy window first to find out supported values.
            // TODO: Also needs to be handled with a config.
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_MULTISAMPLEBUFFERS, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_MULTISAMPLESAMPLES, 8);
        }

        private void CreateWindow()
        {
            windowPtr = SDL.SDL_CreateWindow(Constants.APPLICATION_NAME,
                SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 1024, 768,
                WindowFlags);

            if (windowPtr == IntPtr.Zero)
                throw new HelionException($"Unable to create SDL OpenGL window: {SDL.SDL_GetError()}");
        }

        private void CreateGLContext()
        {
            // TODO: Implement actual versions like 4.4, 4.5, 4.6.
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 4);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 6);

            glContextPtr = SDL.SDL_GL_CreateContext(windowPtr);
            if (glContextPtr == IntPtr.Zero)
                throw new HelionException($"Unable to create SDL OpenGL context: {SDL.SDL_GetError()}");
        }

        private void SetSDLGLAttributes()
        {
            // TODO: Make this based off of the config.
            SDL.SDL_GL_SetSwapInterval((int)SDLVSync.VSYNC_OFF);
        }

        public void SwapBuffers()
        {
            SDL.SDL_GL_SwapWindow(windowPtr);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (glContextPtr != IntPtr.Zero)
                    SDL.SDL_GL_DeleteContext(glContextPtr);
                if (windowPtr != IntPtr.Zero)
                    SDL.SDL_DestroyWindow(windowPtr);
            }

            disposed = true;
        }
    }
}
