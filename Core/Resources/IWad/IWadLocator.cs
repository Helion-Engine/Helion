using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.Resources.IWad
{
    public class IWadLocator
    {
        private readonly List<string> m_directories;

        public IWadLocator(IEnumerable<string> directories)
        {
            m_directories = directories.ToList();
        }

        public List<(string,IWadInfo)> Locate()
        {
            List<(string, IWadInfo)> iwads = new();
            foreach (var dir in m_directories)
            {
                string[] files = Directory.GetFiles(dir, "*.wad");
                foreach (var file in files)
                {
                    IWadInfo? iwadInfo = IWadInfo.GetIWadInfo(file);
                    if (iwadInfo != null)
                        iwads.Add((file, iwadInfo));
                }
            }

            iwads.Sort((i1, i2) => i1.Item2.IWadType.CompareTo(i2.Item2.IWadType));
            return iwads;
        }
    }
}
