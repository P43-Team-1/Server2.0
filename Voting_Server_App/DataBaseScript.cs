using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace Voting_Server_App
{
    public class DataBaseScript
    {
        IPEndPoint localEP;
        Socket listener;
        public void CreateDatabase()
        {
            using (var context = new Context.VoteContext())
            {
                context.Database.EnsureCreated();
            }
        }
        private bool ServerRunning = false;
        public void StartServer()
        {
            ServerRunning = true;
            localEP = new IPEndPoint(IPAddress.Any, 4567);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEP);
            listener.Listen(10);
            while (true)
            {
                Socket client;
                try
                {
                    client = listener.Accept();
                }
                catch { break; }
                string message = "Welcome to the Voting Server!";
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                client.Send(buffer);
                client.Close();
            }
        }

        public void StopServer()
        {
            ServerRunning = false;
            listener.Close();
        }
    }
}