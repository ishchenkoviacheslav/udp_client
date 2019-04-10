using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
namespace UdpClientApp
{
    public class UDPListener
    {
        private static IConfigurationRoot configuration;
        private static int Client_listenPort = 0;
        private static int Server_listenPort = 0;
        private static string server_ip = string.Empty;
        ///ping
        private static List<DateTime> outputTime = new List<DateTime>();
        private static List<DateTime> inputTime = new List<DateTime>();
        private static byte[] ping = Encoding.ASCII.GetBytes("ping");
        private static int pauseBetweenPing = 0;
        private static int pauseBetweenSendData = 0;
        ///
        private static int countOfsmsPerSecond = 0;
        private static void StartListener()
        {
            //work slow for ping!
            Console.WriteLine("Waiting ...");

            Client_listenPort = int.Parse(configuration["client_listenPort"]);
            Server_listenPort = int.Parse(configuration["server_listenPort"]);
            server_ip = configuration.GetSection("serverip").Value;
            pauseBetweenPing = int.Parse(configuration["pausebetweenping"]);
            pauseBetweenSendData = int.Parse(configuration["pauseBetweenSendData"]);
            if (Client_listenPort == 0 || Server_listenPort == 0 || string.IsNullOrEmpty(server_ip) || pauseBetweenPing < 10 || pauseBetweenSendData < 10)
                throw new Exception("configuration data is wrong");
            Console.WriteLine("*********Client*******");
            UdpClient listener = new UdpClient(Client_listenPort);
            //UdpClient sender = new UdpClient();
            DateTime startOfCount = DateTime.UtcNow;
            TimeSpan OneSecond = new TimeSpan(0, 0, 1);
            IPEndPoint groupEP = null; // new IPEndPoint(IPAddress.Any, listenPort);
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        UdpReceiveResult result = await listener.ReceiveAsync();
                        groupEP = result.RemoteEndPoint;
                        byte[] bytes = result.Buffer;
                        //ping must be fastest of calculate  data - no one command must slow it(for example cw)
                        if (bytes.SequenceEqual(ping))
                        {
                            inputTime.Add(DateTime.Now);
                        }
                        else
                        {
                            countOfsmsPerSecond++;
                            if (DateTime.UtcNow.Subtract(startOfCount) > OneSecond)
                            {
                                Console.WriteLine($"Count of sms per 1 second: {countOfsmsPerSecond}");
                                countOfsmsPerSecond = 0;
                                startOfCount = DateTime.UtcNow;
                            }
                            //if ping than don't need wait when cw is finished
                            //Console.WriteLine($"Received from {groupEP} :");
                            //Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                        }
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    listener.Close();
                }
            });
            //ping proccess
            Task.Run(() =>
            {
                while (true)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        outputTime.Add(DateTime.Now);
                        listener.SendAsync(ping, ping.Length, server_ip, Server_listenPort);
                    }
                    Thread.Sleep(pauseBetweenPing);
                    List<TimeSpan> timeSpan = new List<TimeSpan>();
                    //if 5 == 5 ...
                    if (outputTime.Count == inputTime.Count)
                    {
                        for (int i = 0; i < outputTime.Count; i++)
                        {
                            timeSpan.Add(inputTime[i].Subtract(outputTime[i]));
                        }
                        //Console.Clear();
                        Console.BackgroundColor = ConsoleColor.Blue;

                        double doubleAverageTicks = timeSpan.Average(ts => ts.Ticks);
                        long longAverageTicks = Convert.ToInt64(doubleAverageTicks);

                        Console.WriteLine(new TimeSpan(longAverageTicks));
                        Console.BackgroundColor = ConsoleColor.Black;

                    }
                    outputTime.Clear();
                    inputTime.Clear();
                }
            });
            //
            Task.Run(() =>
            {
                while (true)
                {
                    //Thread.Sleep is not so exactly method for stop, but error is not so big(~1 ms)
                    Thread.Sleep(pauseBetweenSendData);
                    string message = "test";
                    if (message == "stop")
                    {
                        break;
                    }
                    byte[] myString = Encoding.ASCII.GetBytes(message);
                    
                    listener.SendAsync(myString, myString.Length, server_ip, Server_listenPort);
                }
            });
            Console.ReadLine();
        }

        public static void Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();
            StartListener();


            //IPAddress serverIP = IPAddress.Parse("127.0.0.1");     // Server IP
            //int port = 27005;                                           // Server port
            //IPEndPoint ipEndPoint = new IPEndPoint(serverIP, port);
            //string serverResponse = string.Empty;       // The variable which we will use to store the server response

            //using (UdpClient client = new UdpClient())
            //{
            //    byte[] data = Encoding.UTF8.GetBytes("I am client");      // Convert our message to a byte array
            //    client.Send(data, data.Length, ipEndPoint);      // Send the date to the server

            //    serverResponse = Encoding.UTF8.GetString(client.Receive(ref ipEndPoint));    // Retrieve the response from server as byte array and convert it to string
            //}
            //Console.WriteLine(serverResponse);

            //work good
            //Task.Run(async () =>
            //{
            //    Console.WriteLine("Client");
            //    IPAddress serverIP = IPAddress.Parse("127.0.0.1");     // Server IP
            //    int port = 27005;                                           // Server port
            //    IPEndPoint ipEndPoint = new IPEndPoint(serverIP, port);
            //    string serverResponse = string.Empty;       // The variable which we will use to store the server response

            //    using (UdpClient client = new UdpClient())
            //    {
            //        byte[] data = Encoding.UTF8.GetBytes("I am client");      // Convert our message to a byte array
            //        await client.SendAsync(data, data.Length, ipEndPoint);      // Send the date to the server
            //        UdpReceiveResult result = await client.ReceiveAsync();
            //        serverResponse = Encoding.UTF8.GetString(result.Buffer);    // Retrieve the response from server as byte array and convert it to string
            //    }
            //    Console.WriteLine(serverResponse);
            //});
            //Console.ReadLine();
        }
    }

}