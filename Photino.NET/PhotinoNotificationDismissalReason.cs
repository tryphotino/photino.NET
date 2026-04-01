using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photino.NET
{
    /// <summary>
    /// The reason given when a notification is dismissed
    /// </summary>
    public enum PhotinoNotificationDismissalReason
    {
        UserCanceled,
        ApplicationHidden,
        TimedOut,
    }
}
