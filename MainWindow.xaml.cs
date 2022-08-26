using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text.Json;
using MQTTnet;
using System.Threading;
using MQTTnet.Protocol;

namespace MQTTTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        
        IMqttClient client;
        MqttClientOptions options;
        private string topic;
        //MqttClient mqttClient;
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (client != null && client.IsConnected)
            {
                await client.DisconnectAsync();
                client.Dispose();
            }
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            topic = $"{txtClientId.Text}/{txtTopic.Text}";
            if (client != null && client.IsConnected)
            {
                client.DisconnectAsync();
                client.Dispose();
            }
            Connect();
        }
        async Task Connect()
        {
            //var server = "test.mosquitto.org";
            //server = "broker.hivemq.com";
            var server = txtBrokerAdress.Text;
            var port = Convert.ToInt16(txtBrokerPort.Text);
            var mqttFactory = new MqttFactory();
            client = mqttFactory.CreateMqttClient();
            options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(server, port)
                .WithCleanSession()
                .Build();
            client.ConnectedAsync += Client_ConnectedAsync;
            client.DisconnectedAsync += Client_DisconnectedAsync;
            client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;
            
            await client.ConnectAsync(options, CancellationToken.None);
            var msg = "connect,server=" + server + ",port=" + port.ToString();
            WriteLog(msg);
        }

        private Task Client_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            var msg = $"received: {Encoding.UTF8.GetString(arg.ApplicationMessage.Payload)} from Topic {arg.ApplicationMessage.Topic}";
            var message = Encoding.UTF8.GetString(arg.ApplicationMessage.Payload);
            Dispatcher.Invoke(delegate
            {              // we need this construction because the receiving code in the library and the UI with textbox run on different threads
                listBox1.Items.Add($"Message: ( {message} ) from Topic ( {arg.ApplicationMessage.Topic} ) at {DateTime.Now}");

                listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
            });
            if (message.Substring(0, 1) == "{" && message.Substring(message.Length - 1) == "}")
            {
                try
                {
                    Dictionary<string, JsonElement> user = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
                    if (user.ContainsKey("Status") && user["Status"].GetString() == "Connected" && user["UseMAC"].GetString() == "True")
                    {


                        Dispatcher.Invoke(delegate
                        {              // we need this construction because the receiving code in the library and the UI with textbox run on different threads
                            Subscribe($"{txtClientId.Text}/{user["MAC"].GetString()}/{txtTopic.Text}");
                            listBox1.Items.Add($"Topic Subscribed : {txtClientId.Text}/{user["MAC"].GetString()}/{txtTopic.Text} at {DateTime.Now}");
                            listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
                        });
                    }
                }
                catch (Exception ex)
                { Console.WriteLine(ex.Message); }
            }

            Debug.WriteLine(msg);
            return Task.CompletedTask;
        }

        private Task Client_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            var msg = "Disconnected from broker!";
            WriteLog(msg);
            Task.Delay(25);
            client.ConnectAsync(options);
            //dispatcherTimer.Start();
            return Task.CompletedTask;
        }

        private Task Client_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            var msg = "connected to the broker!";
            WriteLog(msg);
            //var userId = txtUser.Text;
            Dispatcher.Invoke(delegate
            {              // we need this construction because the receiving code in the library and the UI with textbox run on different threads
                if (client.IsConnected)
                {
                    MQTTnet.Client.MqttClientOptions options = client.Options; 
                    listBox1.Items.Add("Connection Port : " + ((MQTTnet.Client.MqttClientTcpOptions)options.ChannelOptions).Port + " Protocol Version : " + options.ProtocolVersion);
                    listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
                }
            });
                Subscribe(topic);
            return Task.CompletedTask;
        }

        void Subscribe(string stopic)
        {
            var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic(stopic)
                    .Build();
            client.SubscribeAsync(topicFilter);
            var subscribeMsg = "Subscribed topic=" + stopic;
            WriteLog(subscribeMsg);
        }

        void WriteLog(string msg)
        {
            Debug.WriteLine(msg);
        }

        private void btnPublish_Click(object sender, RoutedEventArgs e)
        {
            var msg = "{\"Publish\": \"Test\"}";
            Publish(msg);

        }

        async Task Publish(string msg)
        {
            
            //var userId = txtUser.Text;
            var topic = $"BT_DMM/Values";
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(msg)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            if (client.IsConnected)
            {
                await client.PublishAsync(message);
            }
            var doneMsg = "message published,topic=" + topic + ",msg=" + msg;
            WriteLog(doneMsg);
        }
    }
}
