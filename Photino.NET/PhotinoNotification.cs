using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photino.NET
{
    public partial class PhotinoNotification
    {
        private IntPtr _instance;

        private CppNotificationActionDelegate _actionDelegate;
        private CppNotificationActivatedDelegate _activatedDelegate;
        private CppNotificationDismissedDelegate _dismissedDelegate;

        public event Action<string> OnAction;
        public event Action OnActivated;
        public event Action<PhotinoNotificationDismissalReason> OnDismissed;

        private int _actionCount = 0;
        private string[] _actionNames = new string[4];
        private Action<PhotinoNotification>[] _actionCallbacks = new Action<PhotinoNotification>[4];

        internal PhotinoNotification(IntPtr window)
        {
            _instance = PhotinoNotification_ctor(window);

            _actionDelegate = InvokeOnAction;
            _activatedDelegate = InvokeOnActivated;
            _dismissedDelegate = InvokeOnDismissed;

            PhotinoNotification_SetActionCallback(_instance, _actionDelegate);
            PhotinoNotification_SetActivatedCallback(_instance, _activatedDelegate);
            PhotinoNotification_SetDismissedCallback(_instance, _dismissedDelegate);
        }

        public PhotinoNotification AddText(string text)
        {
            PhotinoNotification_AddText(_instance, text);

            return this;
        }

        public PhotinoNotification AddAction(string action, Action<PhotinoNotification> callback = null)
        {
            if (_actionCount >= _actionNames.Length)
                throw new ArgumentOutOfRangeException(nameof(action), $"Only {_actionNames.Length} actions can be added");

            PhotinoNotification_AddAction(_instance, action);

            _actionNames[_actionCount] = action;

            if (callback != null)
                _actionCallbacks[_actionCount] = callback;

            _actionCount++;

            return this;
        }

        public PhotinoNotification SetImagePath(string imagePath)
        {
            PhotinoNotification_SetImagePath(_instance, imagePath);

            return this;
        }

        public PhotinoNotification SetType(PhotinoNotificationType type)
        {
            PhotinoNotification_SetType(_instance, type);

            return this;
        }

        public void Show()
        {
            PhotinoNotification_Show(_instance);
        }

        public event EventHandler<string> NotificationAction;

        /// <summary>
        /// Registers user-defined handler methods to receive callbacks from the notification when an action is clicked.
        /// </summary>
        /// <returns>
        /// Returns the <see cref="PhotinoNotification" /> instance.
        /// </returns>
        /// <param name="handler"><see cref="EventHandler"/></param>
        public PhotinoNotification RegisterActionHandler(EventHandler<string> handler)
        {
            NotificationAction += handler;

            return this;
        }

        /// <summary>
        /// Invokes the registered user-defined handler methods when a notification's action is clicked.
        /// </summary>
        internal void InvokeOnAction(int actionIndex)
        {
            NotificationAction?.Invoke(this, _actionNames[actionIndex]);

            var actionCallback = _actionCallbacks[actionIndex];

            actionCallback?.Invoke(this);
        }

        public event EventHandler NotificationActivated;

        /// <summary>
        /// Registers user-defined handler methods to receive callbacks from the notification when it is activated.
        /// </summary>
        /// <returns>
        /// Returns the <see cref="PhotinoNotification" /> instance.
        /// </returns>
        /// <param name="handler"><see cref="EventHandler"/></param>
        public PhotinoNotification RegisterActivatedHandler(EventHandler handler)
        {
            NotificationActivated += handler;

            return this;
        }

        /// <summary>
        /// Invokes the registered user-defined handler methods when the notification is activated.
        /// </summary>
        internal void InvokeOnActivated()
        {
            NotificationActivated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<PhotinoNotificationDismissalReason> NotificationDismissed;

        /// <summary>
        /// Registers user-defined handler methods to receive callbacks from the notification when it is dismissed.
        /// </summary>
        /// <returns>
        /// Returns the <see cref="PhotinoNotification" /> instance.
        /// </returns>
        /// <param name="handler"><see cref="EventHandler"/></param>
        public PhotinoNotification RegisterDismissedHandler(EventHandler<PhotinoNotificationDismissalReason> handler)
        {
            NotificationDismissed += handler;

            return this;
        }

        /// <summary>
        /// Invokes the registered user-defined handler methods when the notification is dismissed.
        /// </summary>
        internal void InvokeOnDismissed(PhotinoNotificationDismissalReason reason)
        {
            NotificationDismissed?.Invoke(this, reason);
        }
    }
}

