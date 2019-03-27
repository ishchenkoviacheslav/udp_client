using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpClientApp
{
    public class UDPListener
    {
        private static IConfigurationRoot configuration;
        private static int Client_listenPort = 0;
        private static int Server_listenPort = 0;
        private static string server_ip = string.Empty;
        private static void StartListener()
        {
            Client_listenPort = int.Parse(configuration["client_listenPort"]);
            Server_listenPort = int.Parse(configuration["server_listenPort"]);
            server_ip = configuration["serverip"];
            if (Client_listenPort == 0 || Server_listenPort == 0 || string.IsNullOrEmpty(server_ip))
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
                        Console.WriteLine("Waiting ...");
                        //listen on 11001
                        byte[] bytes = listener.Receive(ref groupEP);

                        Console.WriteLine($"Received from {groupEP} :");
                        Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
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