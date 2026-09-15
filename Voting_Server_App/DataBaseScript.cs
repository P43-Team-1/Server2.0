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
                context.Users.Add(new Tables.User { Login = "admin", EncryptedPassword = "hello_world", NickName = "Administrator", Role = "Admin" });
                context.SaveChanges();
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
            Task.Run(() => BroadcastListener());
            while (true)
            {
                Socket client;
                try
                {
                    client = listener.Accept();
                }
                catch { break; }
                Task.Run(() => HandleClient(client));
            }
        }

        public void BroadcastListener()
        {
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            listener.Bind(new IPEndPoint(IPAddress.Any, 4568));

            byte[] buffer = new byte[1024];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                int len = listener.ReceiveFrom(buffer, ref remoteEP);
                string message = Encoding.UTF8.GetString(buffer, 0, len);
                if (message == "discover_server")
                {
                    string response = "server_here";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    listener.SendTo(responseBytes, remoteEP);
                }
            }
        }

        public void HandleClient(Socket socket)
        {
            byte[] buffer = new byte[1024];
            int len = socket.Receive(buffer);

            if(len == 0) { socket.Close(); return; };
            
            string request = Encoding.UTF8.GetString(buffer, 0, len);
            string[] packets = request.Split(';');
            if (packets[0] == "login")
            {
                CheckLogin(packets[1], packets[2], socket);
            }
            else if(packets[0] == "register")
            {

            }
            else if (packets[0] == "vote")
            {

            }
            socket.Close();
        }

        private void CheckLogin(string login, string password, Socket socket)
        {   
            using var context = new Context.VoteContext();
            var user = context.Users.FirstOrDefault(u => u.Login == login);
            if (user != null && user.EncryptedPassword == password)
            {
                socket.Send(Encoding.UTF8.GetBytes($"login_success;{user.NickName}"));
            }
            else
            {
                socket.Send(Encoding.UTF8.GetBytes("login_failed"));
            }
        }

        public void StopServer()
        {
            ServerRunning = false;
            listener.Close();
        }
    }
}