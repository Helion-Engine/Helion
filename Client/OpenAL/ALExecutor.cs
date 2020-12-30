using System;

namespace Helion.Client.OpenAL
{
    /// <summary>
    /// A helper class for running OpenAL code to assist in debugging.
    /// </summary>
    public static class ALExecutor
    {
        /// <summary>
        /// Runs the code. Used in debugging OpenAL problems.
        /// </summary>
        /// <param name="debugText">The text that indicates what action is
        /// being done for debug purposes.</param>
        /// <param name="action">The code to run. Should not be async.</param>
        public static void Run(string debugText, Action action)
        {
#if DEBUG
            ALAudioSystem.CheckForErrors("[Running: {0}]", debugText);
#endif
            action();
#if DEBUG
            ALAudioSystem.CheckForErrors("[Done: {0}]", debugText);
#endif
        }
    }
}
