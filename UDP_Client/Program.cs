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
        ///

        private static void StartListener()
        {
            //work slow for ping!
            Console.WriteLine("Waiting ...");

            Client_listenPort = int.Parse(configuration["client_listenPort"]);
            Server_listenPort = int.Parse(configuration["server_listenPort"]);
            server_ip = configuration.GetSection("serverip").Value;
            pauseBetweenPing = int.Parse(configuration["pausebetweenping"]);
            if (Client_listenPort == 0 || Server_listenPort == 0 || string.IsNullOrEmpty(server_ip) || pauseBetweenPing < 10)
                throw new Exception("configuration data is wrong");
            Console.WriteLine("*********Client*******");
            UdpClient listener = new UdpClient(Client_listenPort);
            UdpClient sender = new UdpClient();

            IPEndPoint groupEP = null; // new IPEndPoint(IPAddress.Any, listenPort);
            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        //listen on 11001
                        byte[] bytes = listener.Receive(ref groupEP);
                        if(bytes.SequenceEqual(ping))
                        {
                            inputTime.Add(DateTime.Now);
                        }
                        else
                        {
                            //if ping than don't need wait when cw is finished
                            Console.WriteLine($"Received from {groupEP} :");
                            Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
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
                        sender.Send(ping, ping.Length, server_ip, Server_listenPort);
                    }
                    Thread.Sleep(pauseBetweenPing);
                    List<TimeSpan> timeSpan = new List<TimeSpan>();
                    //if 5 == 5 ...
                    if(outputTime.Count == inputTime.Count)
                    {
                        for (int i = 0; i < outputTime.Count; i++)
                        {
                            timeSpan.Add(inputTime[i].Subtract(outputTime[i]));
                        }
                        Console.Clear();
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
            //type some message
            //Task.Run(() =>
            //{
            while (true)
            {
                Console.Write("Enter message: ");
                string message = Console.ReadLine();
                if (message == "stop")
                {
                    break;
                }
                byte[] myString = Encoding.ASCII.GetBytes(message);
                //umc 46.133.172.211
                //ukrtelecom 92.112.59.89
                sender.Send(myString, myString.Length, server_ip, Server_listenPort);
            }
            //});
            Console.ReadLine();
        }

        public static void Main()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();
            StartListener();
        }
    }

}