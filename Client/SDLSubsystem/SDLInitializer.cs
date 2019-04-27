using Helion.Util;
using NLog;
using SDL2;
using System;

namespace Helion.Client.SDLSubsystem
{
    public class SDLInitializer : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private bool disposed = false;

        public SDLInitializer()
        {
            if (SDL.SDL_WasInit(SDL.SDL_INIT_VIDEO) != 0)
                throw new HelionException("Trying to initialize SDL twice");

            // This needs to come before SDL_Init.
            // https://github.com/flibitijibibo/SDL2-CS/issues/106
            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");

            if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) != 0)
                throw new HelionException("Failure to initialize SDL");

            log.Info("Initialized SDL v{0}.{1}.{2}", SDL.SDL_MAJOR_VERSION, SDL.SDL_MINOR_VERSION, SDL.SDL_PATCHLEVEL);
        }
        
        ~SDLInitializer()
        {
            Dispose(false);
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
                SDL.SDL_Quit();

            disposed = true;
        }
    }
}
