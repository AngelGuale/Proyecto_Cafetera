
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Gadgeteer.Networking;
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
//using Microsoft.SPOT.Hardware;

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
        private GHI.Glide.Display.Window window22;
        
        private const string DOMAIN = "Gadgeteer";
        private const string CLIENTID = "CoffeeMaker"; //this is the device id for the broker to use
        private const string DEVICEID = "device1";

        private const string COFFEEMAKERSTATUS = "status/CoffeMaker";
        private const string COFFEECONTROL = "cmd/Coffee";
        private const string DEVICESTATUS = "status";

        //servidor mqtt
        public const string BROKER = "m10.cloudmqtt.com";//54.87.67.171
        public const int PORT = 11001;
        public const string USERNAME = "test1";
        public const string PASSWORD = "test1";

        public MqttClient cliente;
        HttpRequest request;

        public string estado_actual="apagado_all";
        public byte response;
        public Button btn_inicio;
        public TextBlock titulo;
        public TextBlock text_temp;
        public ProgressBar temp_barra;
       
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
          
            
            //iniciar la pantalla
            iniciarWindow = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.inicioWindow));
             window22 = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.window2));
             GlideTouch.Initialize();
         
            btn_inicio = (Button)iniciarWindow.GetChildByName("button_iniciar");
            btn_inicio.TapEvent += btn_inicio_TapEvent;
            //iniciar el timer
           // timer = new GT.Timer(5000);
           // timer.Tick += timer_Tick;

            titulo = (TextBlock)window22.GetChildByName("titulo");
            text_temp = (TextBlock)window22.GetChildByName("temp");
            text_temp.Text = "Temperatura";
            titulo.Text = "Cafetera Apagada.\n Conectando..";
            temp_barra = (ProgressBar)window22.GetChildByName("barra1");


            //setear la pantalla
            Glide.MainWindow = iniciarWindow;

            //configurar la ethernet
            ethernetJ11D.NetworkDown += ethernetJ11D_NetworkDown;
            ethernetJ11D.NetworkUp += ethernetJ11D_NetworkUp;

            ethernetJ11D.NetworkInterface.Open();
            ethernetJ11D.NetworkInterface.EnableDhcp();
            ethernetJ11D.UseThisNetworkInterface();


        }

        private void btn_inicio_TapEvent(object sender)
        {

            Glide.MainWindow = window22;
            apagado_all_action();
          
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

                    //original
                    //cliente = new MqttClient(IPAddress.Parse("54.87.67.171"), 18485, false, null, null, MqttSslProtocols.None);
                    //falso
                    cliente = new MqttClient(IPAddress.Parse("52.31.15.152"), 18860, false, null, null, MqttSslProtocols.None);
                    
                    
                    cliente.MqttMsgPublishReceived += cliente_MqttMsgPublishReceived;
                    cliente.MqttMsgSubscribed += cliente_MqttMsgSubscribed;
                    cliente.MqttMsgPublished += cliente_MqttMsgPublished;

                    //byte response = cliente.Connect(CLIENTID, USERNAME, PASSWORD, true, 2, true,mqttDeviceStatus, "offline", _cleanSession, 60);
                    //original
                    //response = cliente.Connect("Prototipado", "Prototipado", "S9i6r3pmQWME");
                    //falso
                    response = cliente.Connect("ezbxcocw", "ezbxcocw", "JZH8SKw2UuAb");
                    
                    Debug.Print("Connect " + response);

                    //encendido();

                    string[] topic = { "/cafetera", "/onoff", "/prox", "/temp/ir" };
                    byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE };

                    cliente.Subscribe(topic, qosLevels);
                    //cliente.Publish("/cafetera", Encoding.UTF8.GetBytes("Ready"), 2, true);
                   
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

            string topico=e.Topic;
            var message = new string(chars);
            Debug.Print("el mensaje es:" + message+"***");
            mapa_estados(message, topico);
        
            //TextBlock te = (TextBlock)window2.GetChildByName("temp");
            //te.Text = message;
            //ProgressBar pb = (ProgressBar)window2.GetChildByName("barra1");
            //pb.Value = (int)System.Math.Ceiling(Convert.ToDouble(response.Text));
           // Glide.MainWindow = window22;


        }

        public void mapa_estados(string message, string topico)
        {
            Debug.Print("Topico: "+topico);
            switch (estado_actual) { 
                case "apagado_all":
                    switch (message)
                    {
                        case "Conectada":          // respuesta del canal cafetera
                            estado_actual = "encendido_only";
                            Debug.Print(estado_actual);
                            //enviar published
                            encendido_action();
                            break;
                        default:
                            Debug.Print("cafetera canal msg diff");
                            //determinartemp(message);
                           // apagado_all_action();
                            break;
                    }
                    break;

                case "encendido_only":

                     switch (message)
                    {
                        case "1":       // respuesta del canal prox
                            estado_actual = "con_taza";
                            Debug.Print(estado_actual);
                            con_taza_action();
                            break;
                        case "0":          // respuesta del canal OnOff - On
                            Debug.Print("sin taza");
                           // estado_actual = "working";
                            sin_taza_action();
                            break;
                        default:            //si no es las demas, es temperatura
                            Debug.Print("un error en el canal prox");
                            break;
                    }
                    break;

                case "con_taza":
                    if (topico == "/onoff")
                    {
                        switch (message)
                        {
                            case "On":       // respuesta del estado
                                estado_actual = "working";
                                Debug.Print(estado_actual);
                                working_action();
                                break;
                            case "Off":          // respuesta del estado
                                Debug.Print("not_working");
                                // estado_actual = "working";
                                
                                Debug.Print(estado_actual);
                               
                                break;
                            default:            //si no es las demas, es temperatura
                                Debug.Print("un error en el canal prox");
                                break;
                        }
                    }
                    break;
                case "sin_taza":
                    if (topico == "/prox")
                    {
                        switch (message)
                        {
                            case "1":       // respuesta del canal prox
                                estado_actual = "con_taza";
                                Debug.Print(estado_actual);
                                con_taza_action();
                                break;
                            case "0":          // respuesta del canal OnOff - On
                                Debug.Print("sin taza");
                                // estado_actual = "working";
                                break;
                            default:            //si no es las demas, es temperatura
                                Debug.Print("un error en el canal prox");
                                break;
                        }
                    }
                    break;
                case "working":

                    vigila_temperatura_action(message, topico);
                    break;

                default:
                    return;


            
            
            }
            
               
            
            
           
        }

        private void working_action()
        {
            titulo.Text = "Cafetera trabajando..";
            Glide.MainWindow = window22;
         
        }


        private void vigila_temperatura_action(string temp, string topico)
        {

            if (estado_actual == "working" && topico=="/temp/ir") {

                
                Debug.Print("La temperatura: "+temp);
                double temp_float = Double.Parse(temp);

                temp_barra.Value =(int)System.Math.Ceiling( temp_float);
                titulo.Text = "Cafetera trabajando..";
                Glide.MainWindow = window22;
         
                if (temp_float < 60.0)
                {
                    Debug.Print("cafetera funcionando");

                }
                else {

                    Debug.Print("**********Cafe Listo**********");
                    titulo.Text = "**Cafe Listo**";
                    Glide.MainWindow = window22;
         
                }
            }
        }

        
        private void con_taza_action()
        {
            titulo.Text = "Taza colocada. Iniciando..";
            Glide.MainWindow = window22;
         
            cliente.Publish("/cafetera", Encoding.UTF8.GetBytes("On"), 2, true);
            
        }

        private void sin_taza_action()
        {
            titulo.Text = "Sin Taza.";
            Glide.MainWindow = window22;

            cliente.Publish("/prox", Encoding.UTF8.GetBytes("Ready"), 2, true);
           
        }

        private void apagado_all_action()
        {
            titulo.Text = "Cafetera Apagada. Conectando..";

            Glide.MainWindow = window22;
         
            Debug.Print("apagado action, pregunta por encendido");
            cliente.Publish("/cafetera", Encoding.UTF8.GetBytes("Ready"), 2, true);
         
        }

        private void encendido_action()
        {
            Debug.Print("Encendido action, pregunta por prox");
            titulo.Text = "Cafetera Encendida. Preguntando por proximidad";
            Glide.MainWindow = window22;
         
            cliente.Publish("/prox", Encoding.UTF8.GetBytes("Ready"), 2, true);
             

            }

        private void on_working()
        {
            if(estado_actual=="con_taza")
            {
                cliente.Publish("/cafetera", Encoding.UTF8.GetBytes("On"), 2, true);
                Debug.Print("is working...");
            }

        }

        

        
      

    }
}
