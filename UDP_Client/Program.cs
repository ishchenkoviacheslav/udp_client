using Shared;
using System;
using System.Threading;
using System.Threading.Tasks;
using UDPClient.DLL;

namespace UdpClientApp
{
    public class UDPListener
    {
        static ClientData clientData = new ClientData() {ID = 5, X = 1, Y = 2, Z = 3 };

        public static void Main()
        {
            Task.Run(() =>
            {
                Random r = new Random();
                while (true)
                {
                    Thread.Sleep(10);
                    clientData.Y = r.Next(0, 10000);
                }
            });

            UdpClientSide client = new UdpClientSide();
            client.StartService();
            client.SendData(clientData);
            Console.WriteLine("Waiting ...");
            Console.WriteLine("*********Client*******");
            Console.ReadLine();

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
