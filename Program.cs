using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EWSTestConsole
{
    class Program
    {
        private static AutoResetEvent Signal;
        static void Main(string[] args)
        {
            // Establish connection to Exchange with the account
            ExchangeService service = new ExchangeService(ExchangeVersion.Exchange2013);
            service.Credentials = new WebCredentials(ConfigurationManager.AppSettings["eml:AccountAddress"], 
                ConfigurationManager.AppSettings["eml:AccountPassword"]);
            service.Url = new Uri("https://outlook.office365.com/EWS/Exchange.asmx");
            service.AutodiscoverUrl(ConfigurationManager.AppSettings["eml:AccountAddress"], 
                RedirectionUrlValidationCallback);

            // Setup subscription to listen for new messages
            var subscription = service.SubscribeToStreamingNotifications(
                new FolderId[] { WellKnownFolderName.Inbox }, EventType.NewMail);
            StreamingSubscriptionConnection connection = new StreamingSubscriptionConnection(service, 1);
            connection.AddSubscription(subscription);

            // Setup delegate event handlers for the subscription connection 
            connection.OnNotificationEvent += Connection_OnNotificationEvent;
            connection.OnSubscriptionError += Connection_OnSubscriptionError;
            connection.OnDisconnect += Connection_OnDisconnect;
            connection.Open();
            Console.WriteLine($"Connection established at {DateTime.Now.ToLongTimeString()}.");

            // Wait for the application to exit
            Signal = new AutoResetEvent(false);
            Signal.WaitOne();
        }

        private static void Connection_OnDisconnect(object sender, SubscriptionErrorEventArgs args)
        {
            // Cast the sender as a StreamingSubscriptionConnection object            
            StreamingSubscriptionConnection connection = (StreamingSubscriptionConnection)sender;

            // Reopen the connection
            if (!connection.IsOpen)
            {
                connection.Open();
                Console.WriteLine($"Connection renewed at {DateTime.Now.ToLongTimeString()}.");
            }
        }

        private static void Connection_OnSubscriptionError(object sender, SubscriptionErrorEventArgs args)
        {
            throw args.Exception;

            // TODO: put additional logic for handling exceptions
        }

        private static async void Connection_OnNotificationEvent(object sender, NotificationEventArgs args)
        {
            StreamingSubscription subscription = args.Subscription;

            // Loop through all item-related events.  
            foreach (NotificationEvent notification in args.Events)
            {
                EmailMessage email = EmailMessage.Bind(subscription.Service, ((ItemEvent)notification).ItemId);
                email.Load(new PropertySet(ItemSchema.MimeContent));
                var mimeContent = email.MimeContent;

                // Generate a random file id for storage and data mapping
                var id = Guid.NewGuid().ToString();
                await AzureStorageUtil.UploadFile($"{id}.eml", mimeContent.Content);
                Console.WriteLine($"Message processed at {DateTime.Now.ToLongTimeString()}.");
            }
        }

        static bool RedirectionUrlValidationCallback(String redirectionUrl)
        {
            // Perform validation.
            return (redirectionUrl == "https://autodiscover-s.outlook.com/autodiscover/autodiscover.xml");
        }
    }
}
