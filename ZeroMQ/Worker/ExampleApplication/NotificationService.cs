using System;

namespace Worker.ExampleApplication
{
    public class NotificationService
    {
        private NotificationServiceAPI _api;

        public NotificationService(NotificationServiceAPI api)
        {
            _api = api;
            _api.Start(HandleNotificationReveive);
        }

        private void HandleNotificationReveive(string text)
        {
            Console.WriteLine(text);
        }
    }
}