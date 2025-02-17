using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace WorkSpace.Library
{
    public class PathProvider : IPathProvider
    {
        private IHostingEnvironment _HostingEnvironment;

        public PathProvider(IHostingEnvironment HostingEnvironment)
        {
            _HostingEnvironment = HostingEnvironment;
        }

        public String MapPath(String path)
        {
            String filePath = Path.Combine(_HostingEnvironment.ContentRootPath, path);

            return filePath;
        }
    }
}
