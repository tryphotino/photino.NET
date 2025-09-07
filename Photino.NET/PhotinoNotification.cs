using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photino.NET
{
    public class PhotinoNotification
    {
        public string FirstLine { get; set; }
        public string SecondLine { get; set; }
        public string ThirdLine { get; set; }
        public string AttributionText { get; set; }
        public string IconPath { get; set; }
        public PhotinoNotificationType Type { get; set; }
        public PhotinoNotificationButton[] Buttons { get; set; } = new PhotinoNotificationButton[5];
    }
}
