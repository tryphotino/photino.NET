using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photino.NET
{
    public class PhotinoNotificationBuilder
    {
        PhotinoWindow _window;
        PhotinoNotification _notification = new();

        public PhotinoNotificationBuilder() { }
        public PhotinoNotificationBuilder(PhotinoWindow window)
        {
            _window = window;
        }

        private ushort MaxLinesSupported => _notification.Type switch
        {
            PhotinoNotificationType.ToastText01 => 1,
            PhotinoNotificationType.ToastText02 => 2,
            PhotinoNotificationType.ToastText03 => 2,
            PhotinoNotificationType.ToastText04 => 3,
            PhotinoNotificationType.ToastImageAndText01 => 1,
            PhotinoNotificationType.ToastImageAndText02 => 2,
            PhotinoNotificationType.ToastImageAndText03 => 2,
            PhotinoNotificationType.ToastImageAndText04 => 3,
            _ => throw new NotImplementedException(),
        };

        public PhotinoNotificationBuilder SetFirstLine(string text)
        {
            _notification.FirstLine = text;

            return this;
        }

        public PhotinoNotificationBuilder SetSecondLine(string text)
        {
            if (MaxLinesSupported >= 2)
                _notification.SecondLine = text;

            return this;
        }

        public PhotinoNotificationBuilder SetThirdLine(string text)
        {
            if (MaxLinesSupported >= 3)
                _notification.ThirdLine = text;

            return this;
        }

        public PhotinoNotificationBuilder SetAttributionText(string text)
        {
            _notification.AttributionText = text;

            return this;
        }

        public PhotinoNotificationBuilder AddText(string text)
        {
            if (String.IsNullOrWhiteSpace(_notification.FirstLine))
                SetFirstLine(text);
            else if (String.IsNullOrWhiteSpace(_notification.SecondLine))
                SetSecondLine(text);
            else if (String.IsNullOrWhiteSpace(_notification.ThirdLine))
                SetThirdLine(text);
            else if (String.IsNullOrWhiteSpace(_notification.AttributionText))
                SetAttributionText(text);

                return this;
        }

        public PhotinoNotificationBuilder SetIcon(string path)
        {
            _notification.IconPath = path;

            return this;
        }

        public PhotinoNotificationBuilder SetWindow(PhotinoWindow window)
        {
            _window = window;

            return this;
        }

        public PhotinoNotificationBuilder AddButton(string label)
        {
            for (int i = 0; i < _notification.Buttons.Length; i++)
            {
                if (_notification.Buttons[i] is null)
                {
                    _notification.Buttons[i] = new PhotinoNotificationButton
                    {
                        Label = label
                    };
                    break;
                }
            }

            return this;
        }

        public PhotinoNotification Build()
        {
            return _notification;
        }

        public void Send()
        {
            if (_window != null)
                _window.SendNotification(_notification);
            else
                throw new ApplicationException("Notification must be bound to a window");
        }
    }
}
