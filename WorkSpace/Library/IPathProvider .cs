using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkSpace.Library
{
    public interface IPathProvider
    {
        String MapPath(String path);
    }
}
