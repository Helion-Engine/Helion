using System;
using Helion.Util.Configuration;
using NLog;

namespace Helion.Util
{
    /// <summary>
    /// Tracks when GC events happen and logs them if the user wants.
    /// </summary>
    public class GCTracker
    {
        private const int Generations = 3;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Config m_config;
        private readonly int[] m_lastGenAmount = new int[Generations];
        private readonly int[] m_collectionGen = new int[Generations];

        /// <summary>
        /// Creates a new GC tracker that uses the config for checking if
        /// printing should occur.
        /// </summary>
        /// <param name="config">The config to use.</param>
        public GCTracker(Config config)
        {
            m_config = config;
        }

        /// <summary>
        /// Performs an update, whereby any GC events since the last update are
        /// printed out.
        /// </summary>
        public void Update()
        {
            if (!m_config.Engine.Developer.GCStats)
                return;
            
            for (int gen = 0; gen < Generations; gen++)
            {
                m_collectionGen[gen] = m_lastGenAmount[gen];
                m_lastGenAmount[gen] = GC.CollectionCount(gen);

                if (m_lastGenAmount[gen] > m_collectionGen[gen] && gen != 0)
                    Log.Info("GC ran for generation {0} at {1}", gen, DateTime.Now.ToString("HH:mm:ss.fff"));
            }
        }
    }
}