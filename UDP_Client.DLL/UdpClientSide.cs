using Microsoft.Extensions.Configuration;
using Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UDPClient.DLL.Helper;

namespace UDPClient.DLL
{
    public class UdpClientSide
    {
        public UdpClientSide()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();

            client_listenPort = int.Parse(configuration[nameof(client_listenPort)]);
            server_listenPort = int.Parse(configuration[nameof(server_listenPort)]);
            server_ip = configuration.GetSection(nameof(server_ip)).Value;
            pause_between_ping = int.Parse(configuration[nameof(pause_between_ping)]);
            pause_between_send_data = int.Parse(configuration[nameof(pause_between_send_data)]);
            if (client_listenPort == 0 || server_listenPort == 0 || string.IsNullOrEmpty(server_ip) || pause_between_ping < 10 || pause_between_send_data < 10)
            {
                throw new Exception("configuration data is wrong");
            }
            listener = new UdpClient(client_listenPort);
            //listener.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            StartService();
        }
        private IConfigurationRoot configuration;
        private int client_listenPort = 0;
        private int server_listenPort = 0;
        private string server_ip = string.Empty;
        ///ping
        private List<DateTime> outputTime = new List<DateTime>();
        private List<DateTime> inputTime = new List<DateTime>();
        private byte[] ping = Encoding.ASCII.GetBytes("ping");
        private int pause_between_ping = 0;
        private int pause_between_send_data = 0;
        ///
        private int countOfsmsPerSecond = 0;
        private const int SIO_UDP_CONNRESET = -1744830452;

        private object LockerPing = new object();
        private object LockerCollection = new object();
        private TimeSpan pingTime = new TimeSpan(0, 0, 0, 0, 0);
        UdpClient listener = null;
        private List<ClientData> myVisibleClients = new List<ClientData>();
        public List<ClientData> MyVisibleClients
        {
            get
            {
                List<ClientData> _temp = new List<ClientData>();
                lock (LockerCollection)
	            {
                    _temp = myVisibleClients.ToList();
                    //for (int t = 0; t < myVisibleClients.Count; t++)
                    //{
                    //    _temp.Add(new ClientData() { X = myVisibleClients[t].X, Y = myVisibleClients[t].Y, Z = myVisibleClients[t].Z});
                    //}
	            }
                return _temp;
            }
        }
        public TimeSpan PingTime
        {
            get
            {
                TimeSpan _pingTime;
                lock (LockerPing)
                {
                    _pingTime = new TimeSpan(pingTime.Hours, pingTime.Minutes, pingTime.Seconds, pingTime.Milliseconds);
                }
                return _pingTime;
            }
        }
        private void StartService()
        {
            try
            {
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
                                inputTime.Add(DateTime.UtcNow);
                            }
                            else
                            {
                                //myVisibleClients = (List<ClientData>)bytes.Deserializer();
                                //16 - 4 prop by 4 bytes every(used type float) = 16 bytes 1 object
                                if(bytes.Length % 16 != 0)
                                {
                                    //client receive not full packet(udp)
                                    //receive new packet
                                    Console.WriteLine($"bytes length is not 16!");
                                    continue;
                                }
                                else
                                {
                                    byte[] tempArray = new byte[4];
                                    ClientData clientData = null;
                                    lock (LockerCollection)
                                    {
                                        myVisibleClients = new List<ClientData>();
                                        //16 - 4 prop by 4 bytes every(used type float) = 16 bytes 1 object
                                        for (int c = 0; c < bytes.Length; c += 16)
                                        {
                                            clientData = new ClientData();
                                            Buffer.BlockCopy(bytes, c, tempArray, 0, tempArray.Length);
                                            clientData.ID = BitConverter.ToSingle(tempArray, 0);

                                            Buffer.BlockCopy(bytes, c + 4, tempArray, 0, tempArray.Length);
                                            clientData.X = BitConverter.ToSingle(tempArray, 0);

                                            Buffer.BlockCopy(bytes, c + 8, tempArray, 0, tempArray.Length);
                                            clientData.Y = BitConverter.ToSingle(tempArray, 0);

                                            Buffer.BlockCopy(bytes, c + 12, tempArray, 0, tempArray.Length);
                                            clientData.Z = BitConverter.ToSingle(tempArray, 0);
                                            myVisibleClients.Add(clientData);
                                        }
                                    }
                                }
                                //only for testing
                                countOfsmsPerSecond++;
                                if (DateTime.UtcNow.Subtract(startOfCount) > OneSecond)
                                {
                                    Console.WriteLine($"Count of sms per 1 second: {countOfsmsPerSecond}");
                                    countOfsmsPerSecond = 0;
                                    startOfCount = DateTime.UtcNow;
                                }
                                //here we must receive and update list of visible(in out game) clients
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        throw e;
                    }
                    finally
                    {
                        listener.Close();
                    }
                });
                //ping proccess
                Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            //5 send to server(without pause), and fix send-time
                            for (int i = 0; i < 5; i++)
                            {
                                outputTime.Add(DateTime.UtcNow);
                                listener.Send(ping, ping.Length, server_ip, server_listenPort);
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
                                lock (LockerPing)
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
                        throw ex;
                    }
                    finally
                    {
                        listener.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// This method must repeat
        /// </summary>
        /// <param name="clientData"></param>
        public void SendData(ClientData clientData)
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            //Thread.Sleep is not so exactly method for stop, but error is not so big(~1 ms)
                            Thread.Sleep(pause_between_send_data);
                            //199 bytes!!!Why?!
                            //to do: use another algorithm? and also in server
                            byte[] bytes = clientData.Serializer();
                            listener.Send(bytes, bytes.Length, server_ip, server_listenPort);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        listener.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }
    }
}
