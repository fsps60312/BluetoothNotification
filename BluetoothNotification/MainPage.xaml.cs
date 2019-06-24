using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using System.Diagnostics;
using Newtonsoft.Json;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BluetoothNotification
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Button button_notification = new Button {Content="notification" };
        TextBox txb = new TextBox { AcceptsReturn = true };
        void InitializeViews()
        {
            this.Content = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                },
                Children =
                {
                    button_notification.Set(0,0),
                    txb.Set(1,0)
                }
            };
        }
        async void Log(string text)
        {
            var action = new Action(() =>
              {
                  txb.Text += $"{text}\n";
              });
            if (Dispatcher.HasThreadAccess) action();
            else await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => action());
        }
        void RegisterEvents()
        {
            int counter = 0;
            button_notification.Click += delegate
            {
                var message = "hi";
                var xml = $"<toast><visual><binding template='ToastGeneric'><text>{message}</text><text>content</text></binding></visual></toast>";
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(xml);
                ToastNotification toast = new ToastNotification(xmlDocument);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
                button_notification.Content = $"button {++counter}";
            };
        }
        async void ShowNotification(string app_name,string title,string content)
        {
            var action = new Action(() =>
              {
                  var xml = $"<toast><visual><binding template='ToastGeneric'><text>{app_name}</text><text>{title}</text><text>{content}</text></binding></visual></toast>";
                  XmlDocument xmlDocument = new XmlDocument();
                  xmlDocument.LoadXml(xml);
                  ToastNotification toast = new ToastNotification(xmlDocument);
                  ToastNotificationManager.CreateToastNotifier().Show(toast);
              });
            if (Dispatcher.HasThreadAccess) action();
            else await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => action());
        }
        RfcommServiceProvider provider;
        const uint SERVICE_VERSION_ATTRIBUTE_ID = 0x0300;
        const byte SERVICE_VERSION_ATTRIBUTE_TYPE = 0x0A;   // UINT32
        const uint SERVICE_VERSION = 200;
        async void InitializeBluetooth()
        {
            try
            {
                var service_id=RfcommServiceId.FromUuid(Guid.Parse("ff1477c2-7265-4d55-bb08-c3c32c6d635c"));
                provider = await RfcommServiceProvider.CreateAsync(service_id);
                StreamSocketListener listener = new StreamSocketListener();
                listener.ConnectionReceived += OnConnectionReceived;
                await listener.BindServiceNameAsync(
                    provider.ServiceId.AsString(),
                    SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

                // InitializeServiceSdpAttributes
                var writer = new Windows.Storage.Streams.DataWriter();
                writer.WriteByte(SERVICE_VERSION_ATTRIBUTE_TYPE);
                writer.WriteUInt32(SERVICE_VERSION);
                var data = writer.DetachBuffer();
                provider.SdpRawAttributes.Add(SERVICE_VERSION_ATTRIBUTE_ID, data);

                provider.StartAdvertising(listener, true);
            }
            catch (Exception error)
            {
                Log(error.ToString());
            }
        }
        class NotificationData
        {
            public string app_name, title, content;
        }
        async void OnConnectionReceived(StreamSocketListener listener,StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                Log("Accepted.");
                provider.StopAdvertising();
                await listener.CancelIOAsync();
                StreamSocket socket = args.Socket;
                Log("Start connection.");
                var input_stream = socket.InputStream;
                while (true)
                {
                    Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer(1024);
                    var n =await input_stream.ReadAsync(buffer, buffer.Capacity, Windows.Storage.Streams.InputStreamOptions.Partial);
                    byte[] bytes = buffer.ToArray();
                    String text = System.Text.Encoding.UTF8.GetString(bytes, 0, (int)n.Length);
                    Log($"Received: {text}");
                    var notificationData= JsonConvert.DeserializeObject<NotificationData>(text);
                    ShowNotification(notificationData.app_name, notificationData.title, notificationData.content);
                }
            }
            catch(Exception error)
            {
                Log(error.ToString());
            }
        }
        public MainPage()
        {
            this.InitializeComponent();
            InitializeViews();
            RegisterEvents();
            InitializeBluetooth();
            Log("Initialized.");
        }
    }
}
