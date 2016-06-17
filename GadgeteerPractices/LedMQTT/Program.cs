using System.Threading;
using System.Collections;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.SPOT.Hardware;
using System.Text;
using Gadgeteer.Networking;
//Estas referencias son necesarias para usar GLIDE
using GHI.Glide;
using GHI.Glide.Display;
using GHI.Glide.UI;
using System.Globalization;
using Gadgeteer.Modules.GHIElectronics;

namespace LedMQTT
{
    public partial class Program
    {
        private GHI.Glide.Display.Window iniciarWindow;
        private GHI.Glide.Display.Window window2;
        
       
   

        private const string DOMAIN = "Gadgeteer";
        private const string CLIENTID = "CoffeeMaker"; //this is the device id for the broker to use
        private const string DEVICEID = "device1";

        private const string COFFEEMAKERSTATUS = "status/CoffeMaker";
        private const string COFFEECONTROL = "cmd/Coffee";
        private const string DEVICESTATUS = "status";

        //servidor mqtt
        public const string BROKER = "m10.cloudmqtt.com";
        public const int PORT = 11001;
        public const string USERNAME = "test1";
        public const string PASSWORD = "test1";

        public MqttClient cliente;
        HttpRequest request;

        public string estado_actual="sin_energia";
        public byte response;

       
        //requested QoS level, the client receives PUBLISH messages at less than or equal to this level

        private static bool _cleanSession = true;

        private readonly char[] _delimiters = { '/' }; //used to parse topic strings

        //only want control messages for this device (subscription)
        private const string mqttDeviceCommand = DOMAIN + "/" + DEVICEID + "/" + COFFEECONTROL + "/";

        // used to send out status messages for the lights (publish)
        private const string mqttCoffeStatus = DOMAIN + "/" + DEVICEID + "/" + COFFEEMAKERSTATUS + "/";

        // used to send out status messages for the device (publish)
        private const string mqttDeviceStatus = DOMAIN + "/" + DEVICEID + "/" + DEVICESTATUS;

        private readonly string[] DeviceSubscriptions = { mqttDeviceCommand };

        private readonly byte[] QOSServiceLevels = { 2 };

        Gadgeteer.Timer timer = new Gadgeteer.Timer(4000);

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
          

            Debug.Print("Program Started");
          
            //configurar la ethernet
            ethernetJ11D.NetworkDown += ethernetJ11D_NetworkDown;
            ethernetJ11D.NetworkUp += ethernetJ11D_NetworkUp;

            ethernetJ11D.NetworkInterface.Open();
            ethernetJ11D.NetworkInterface.EnableDhcp();
            ethernetJ11D.UseThisNetworkInterface();

            //iniciar la pantalla
            iniciarWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.inicioWindow));
            GlideTouch.Initialize();
            window2 = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.window2));
            GlideTouch.Initialize();

            //iniciar el timer
            timer = new GT.Timer(5000);
            timer.Tick += timer_Tick;

            //setear la pantalla
            Glide.MainWindow = window2;

        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Debug.Print("Respuesta");
        }

        private void timer_Tick(GT.Timer timer)
        {
            Debug.Print("tick");
          
        }

        private void request_ResponseReceived(HttpRequest sender, HttpResponse response)
        {
            Debug.Print("Resp");
            Debug.Print(response.Text);
        }

        private void ethernetJ11D_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print(ethernetJ11D.NetworkInterface.IPAddress);
            inicio();
        }

        private void ethernetJ11D_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("apagado");
        }
        /*
        private void Timer_Tick(Gadgeteer.Timer timer)
        {
            try
            {
                if (cliente.IsConnected)
                {
                   
                }
                else
                {
                    byte response = cliente.Connect(CLIENTID, USERNAME, PASSWORD, true, 2, true,
                            mqttDeviceStatus, "offline", _cleanSession, 60);

                    if (response == 0)
                    {
                        cliente.Publish(mqttDeviceStatus, Encoding.UTF8.GetBytes("online"), 2, true);
                        Debug.Print(mqttDeviceStatus + " online : 2 true");
                    }
                    Debug.Print("Connect " + response);
                    timer.Start();
                    cliente.Subscribe(DeviceSubscriptions, QOSServiceLevels);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }
        */
        private void inicio()
        {
            if (ethernetJ11D.IsNetworkConnected && (ethernetJ11D.NetworkInterface.IPAddress != "0.0.0.0"))
            {
                try
                {

                    cliente = new MqttClient(IPAddress.Parse("52.31.15.152"), 18860, false, null, null, MqttSslProtocols.None);
                    cliente.MqttMsgPublishReceived += cliente_MqttMsgPublishReceived;
                    cliente.MqttMsgSubscribed += cliente_MqttMsgSubscribed;
                    cliente.MqttMsgPublished += cliente_MqttMsgPublished;

                    //byte response = cliente.Connect(CLIENTID, USERNAME, PASSWORD, true, 2, true,mqttDeviceStatus, "offline", _cleanSession, 60);
                    response = cliente.Connect("cafetera1", "ezbxcocw", "JZH8SKw2UuAb");
                    Debug.Print("Connect " + response);

                    encendido();

                    string[] topic = { "Caf", "OnOff", "Prox", "temp/ir" };
                    byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };

                    cliente.Subscribe(topic, qosLevels);
                   
                }
                catch (Exception ex)
                {
                    Debug.Print("se fue a error");
                    Debug.Print(ex.Message);
                  
                }                
            }
            
        }

        private void ThreadedDisconnect()
        {
            cliente.Disconnect();
        }

        private void cliente_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            Debug.Print("Msg Unsubscribed " + e.MessageId);
        }

        private void cliente_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            Debug.Print("Msg Published " + e.MessageId);
        }        

        private void cliente_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            Debug.Print("Message Subscribed " + e.MessageId);
        }

        private void cliente_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {

            char[] chars = Encoding.UTF8.GetChars(e.Message);
            var message = new string(chars);
            mapa_estados(message);

            TextBlock te = (TextBlock)window2.GetChildByName("temp");
            te.Text = message;
            //ProgressBar pb = (ProgressBar)window2.GetChildByName("barra1");
            //pb.Value = (int)System.Math.Ceiling(Convert.ToDouble(response.Text));
            Glide.MainWindow = window2;


        }

        public void mapa_estados(string message)
        {
            switch (message)
            {
                case "Encen":       // respuesta del canal Caf - Encendido
                    estado_actual = "con_energia";
                    prox_taza();
                    break;
                case "on":          // respuesta del canal OnOff - On
                    Debug.Print("Haciendo cafe");
                    estado_actual = "working";
                    break;
                case "off":         // respuesta del canal OnOff - Off
                    Debug.Print("Apagando");
                    estado_actual = "con_energia";
                    prox_taza();
                    break;
                case "1":           // respuesta del canal Prox-1
                    estado_actual = "con_taza";
                    on_working();
                    Debug.Print("es 1");
                    break;
                case "0":           // respuesta del canal Prox-0
                    estado_actual = "sin_taza";
                    encendido();
                    Debug.Print("es 0");
                    break;
                default:            //si no es las demas, es temperatura
                    Debug.Print("es temp");
                    determinartemp(message);
                    break;
            }
        }

        private void on_working()
        {
            if(estado_actual=="con_taza")
            {
                cliente.Publish("App/Caf", Encoding.UTF8.GetBytes("On"), 2, true);
                Debug.Print("is working...");
            }

        }

        private void encendido()
        {
            if (response == 0)
            {
                Debug.Print("encendido");
                cliente.Publish("App/Caf", Encoding.UTF8.GetBytes("Ready"), 2, true);
            }
        }

        private void prox_taza()
        {
            if (estado_actual == "con_energia")
            {
                Debug.Print("ask taza?");
                cliente.Publish("App/Prox", Encoding.UTF8.GetBytes("Ready"), 2, true);
            }
        }

        private void determinartemp(string message)
        {
            if (estado_actual == "working")
            {
                Debug.Print("temperatura: " + message);
                double temp = Convert.ToDouble(message);

                if (temp > 60)
                {
                    Debug.Print("cafe  listo");
                    //presentar en pantalla CAFE LISTO
                }
                else
                {
                    Debug.Print("cafe no listo");
                }
            }
        }

      

    }
}
