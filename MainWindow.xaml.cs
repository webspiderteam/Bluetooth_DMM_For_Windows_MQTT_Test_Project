﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text.Json;

namespace MQTTTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        MqttClient mqttClient;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MqttClient_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Message);
            var user = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);
            Dispatcher.Invoke(delegate
            {              // we need this construction because the receiving code in the library and the UI with textbox run on different threads
                listBox1.Items.Add($"Message: ( {message} ) from Topic ( {e.Topic} ) at {DateTime.Now}");
                
                listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
            });
            if (user.ContainsKey("Status") && user["Status"].GetString() == "Connected" && user["UseMAC"].GetString() == "True") 
            {
                mqttClient.Subscribe(new string[] { "BT_DMM/" + user["MAC"].GetString() + "/Values" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                
                Dispatcher.Invoke(delegate
                {              // we need this construction because the receiving code in the library and the UI with textbox run on different threads
                    listBox1.Items.Add("Topic Subscribed : BT_DMM/" + ((JsonElement)user["MAC"]).GetString() + "/Values at " + DateTime.Now);
                    listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
                });
            }
            Debug.WriteLine("test");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (mqttClient != null && mqttClient.IsConnected)
                mqttClient.Disconnect();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (mqttClient != null && mqttClient.IsConnected)
                mqttClient.Disconnect();

            try
            {
                mqttClient = new MqttClient((string)txtBrokerAdress.Text, Convert.ToInt32(txtBrokerPort.Text), (bool)isEncrypted.IsChecked, null, null, (bool)isEncrypted.IsChecked ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                mqttClient.shouldReconnect = true;
                mqttClient.MqttMsgPublishReceived -= MqttClient_MqttMsgPublishReceived;
                mqttClient.MqttMsgPublishReceived += MqttClient_MqttMsgPublishReceived;
                mqttClient.Subscribe(new string[] { $"{txtClientId.Text}/{txtTopic.Text}" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                if ((bool)chkUseLogin.IsChecked)
                    mqttClient.Connect(txtClientId.Text, txtUserName.Text, txtPasword.Password);
                else
                    mqttClient.Connect(txtClientId.Text);
                if (mqttClient.IsConnected)
                {

                    listBox1.Items.Add("Connection Port : " + mqttClient.Settings.Port + " Protocol Version : " + mqttClient.ProtocolVersion);

                    listBox1.ScrollIntoView(listBox1.Items[listBox1.Items.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                //MQTT Connection Error
                Debug.WriteLine("MQTT Connection Error");
                MessageBox.Show(ex.ToString());
            }
        }

    }
}
