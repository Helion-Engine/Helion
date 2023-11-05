using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helion.Resources.IWad;

public struct IWadPath
{
    public string Path;
    public IWadInfo Info;

    public IWadPath(string path, IWadInfo info)
    {
        Path = path;
        Info = info;
    }
}