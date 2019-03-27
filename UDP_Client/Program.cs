using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpClientApp
{
    public class UDPListener
    {
        private const int Client_listenPort = 11001;
        private const int Server_listenPort = 11000;

        private static void StartListener()
        {
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
                sender.Send(myString, myString.Length, "46.133.172.211", Server_listenPort);
            }
            //});
            Console.ReadLine();
        }

        public static void Main()
        {
            StartListener();
        }
    }

}