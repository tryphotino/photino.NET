using System.Collections.Generic;
using System.IO;

namespace PhotinoNET
{
    public class PhotinoFluidOptions
    {
        public PhotinoFluid Parent { get; set; }
        public IDictionary<string, ResolveWebResourceDelegate> SchemeHandlers { get; }
            = new Dictionary<string, ResolveWebResourceDelegate>();
    }

    // public delegate Stream ResolveWebResourceDelegate(string url, out string contentType);
}
