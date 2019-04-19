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
using UDP_Client;

namespace UdpClientApp
{
    public class UDPListener
    {
        private static IConfigurationRoot configuration;
        private static int client_listenPort = 0;
        private static int server_listenPort = 0;
        private static string server_ip = string.Empty;
        ///ping
        private static List<DateTime> outputTime = new List<DateTime>();
        private static List<DateTime> inputTime = new List<DateTime>();
        private static byte[] ping = Encoding.ASCII.GetBytes("ping");
        private static int pause_between_ping = 0;
        private static int pause_between_send_data = 0;
        ///
        private static int countOfsmsPerSecond = 0;
        private const int SIO_UDP_CONNRESET = -1744830452;

        private static object Locker = new object();
        /// <summary>
        /// use only in lock context(Locker object)
        /// </summary>
        /// 
        private static TimeSpan pingTime = new TimeSpan(0,0,0,0,0);

        public TimeSpan PingTime
        {
            get
            {
                TimeSpan _pingTime;
                lock (Locker)
                {
                    _pingTime = new TimeSpan(pingTime.Hours, pingTime.Minutes, pingTime.Seconds,pingTime.Milliseconds);
                }
                return _pingTime;
            }
        }

        private static void StartListener()
        {
            Console.WriteLine("Waiting ...");

            client_listenPort = int.Parse(configuration[nameof(client_listenPort)]);
            server_listenPort = int.Parse(configuration[nameof(server_listenPort)]);
            server_ip = configuration.GetSection(nameof(server_ip)).Value;
            pause_between_ping = int.Parse(configuration[nameof(pause_between_ping)]);
            pause_between_send_data = int.Parse(configuration[nameof(pause_between_send_data)]);
            if (client_listenPort == 0 || server_listenPort == 0 || string.IsNullOrEmpty(server_ip) || pause_between_ping < 10 || pause_between_send_data < 10)
            {
                throw new Exception("configuration data is wrong");
            }
            Console.WriteLine("*********Client*******");
            UdpClient listener = new UdpClient(client_listenPort);
            //listener.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);

            DateTime startOfCount = DateTime.UtcNow;
            TimeSpan OneSecond = new TimeSpan(0, 0, 1);
            IPEndPoint groupEP = null; // new IPEndPoint(IPAddress.Any, listenPort);
            //receiver
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
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        //5 send to server(without pause), and fix send-time
                        for (int i = 0; i < 5; i++)
                        {
                            outputTime.Add(DateTime.UtcNow);
                            await listener.SendAsync(ping, ping.Length, server_ip, server_listenPort);
                        }
                        Thread.Sleep(pause_between_ping);
                        List<TimeSpan> timeSpan = new List<TimeSpan>();
                        //if 5 == 5 ...count of send and receive ping requests
                        //if not equals than ignore
                        if (outputTime.Count == inputTime.Count)
                        {
                            //calculate ping time
                            for (int i = 0; i < outputTime.Count; i++)
                            {
                                timeSpan.Add(inputTime[i].Subtract(outputTime[i]));
                            }
                            Console.BackgroundColor = ConsoleColor.Blue;
                            //calculate ping time
                            double doubleAverageTicks = timeSpan.Average(ts => ts.Ticks);
                            long longAverageTicks = Convert.ToInt64(doubleAverageTicks);
                            lock (Locker)
                            {
                                pingTime = new TimeSpan(longAverageTicks);
                            }
                            //to do: fix this time in variable
                            Console.WriteLine(pingTime);
                            Console.BackgroundColor = ConsoleColor.Black;

                        }
                        outputTime.Clear();
                        inputTime.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
            //send
            Task.Run(() =>
            {
                try
                {
                    ClientData cd = new ClientData() { X = 1.5F, Y = 2.3F, Z = 3.1F };
                    while (true)
                    {
                        //Thread.Sleep is not so exactly method for stop, but error is not so big(~1 ms)
                        Thread.Sleep(pause_between_send_data);
                        //199 bytes!!!Why?!
                        byte[] bytes = cd.Serializer();
                        listener.Send(bytes, bytes.Length, server_ip, server_listenPort);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
            Console.ReadLine();
            listener.Dispose();
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