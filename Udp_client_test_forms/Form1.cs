using Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UDPClient.DLL;

namespace Udp_client_test_forms
{
    public partial class Form1 : Form
    {
        UdpClientSide udpClient = new UdpClientSide();
        ClientData clientData = new ClientData();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RandClick();
        }

        private void DrawCircle(PaintEventArgs e, ClientData clientData, int width, int height, Pen pen)
        {
            //e.Graphics.Clear(Color.White);
            e.Graphics.DrawEllipse(pen, clientData.X - width / 2, clientData.Y - height / 2, width, height);
            //e.Dispose();
        }

        private void RandClick()
        {
            Random r = new Random();
            Graphics g = this.CreateGraphics();

            Task.Run(() =>
            {
                Pen myPen = new Pen(Color.Green, 3);
                Pen anotherUsers = new Pen(Color.Red, 3);
                clientData.ID = r.Next(1,1000000);
                g.Clear(Color.White);
                while (true)
                {
                    clientData.X = r.Next(10, 400);
                    clientData.Y = r.Next(10, 400);

                    DrawCircle(new PaintEventArgs(g, new Rectangle()), clientData, 10, 10, myPen);
                    udpClient.SendData(clientData);
                    for (int i = 0; i < udpClient.MyVisibleClients.Count(); i++)
                    {
                        DrawCircle(new PaintEventArgs(g, new Rectangle()), udpClient.MyVisibleClients[i], 10, 10, anotherUsers);
                    }

                    Thread.Sleep(3000);
                }
            });
            //Task.Run(() => 
            //{

            //    while (true)
            //    {
                    
            //        Thread.Sleep(3000);
            //    }
            //});
        }

    }
}
