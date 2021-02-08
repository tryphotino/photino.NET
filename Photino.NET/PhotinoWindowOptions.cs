using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PhotinoNET
{
    public class PhotinoWindowOptions
    {
        public PhotinoWindow Parent { get; set; }
        public IDictionary<string, ResolveWebResourceDelegate> CustomSchemeHandlers { get; }
            = new Dictionary<string, ResolveWebResourceDelegate>();
        
        public EventHandler WindowCreatingHandler { get; set; }
        public EventHandler WindowCreatedHandler { get; set; }
        
        public EventHandler WindowClosingHandler { get; set; }

        public EventHandler<Size> SizeChangedHandler { get; set; }
        public EventHandler<Point> LocationChangedHandler { get; set; }
        
        public EventHandler<string> WebMessageReceivedHandler { get; set; }
    }

    public delegate Stream ResolveWebResourceDelegate(string url, out string contentType);
}
