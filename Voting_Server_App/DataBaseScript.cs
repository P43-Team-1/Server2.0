using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Voting_Server_App.Tables;

namespace Voting_Server_App
{
    public class DataBaseScript
    {
        IPEndPoint localEP;
        Socket listener;

        public event Action<string> OnLogMessage;

        private void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }
        public void CreateDatabase()
        {
            using (var context = new Context.VoteContext())
            {
                context.Database.EnsureDeleted();
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
            try {
                byte[] buffer = new byte[4096];
                int len = socket.Receive(buffer);

                if (len == 0) { socket.Close(); return; }
                ;

                string request = Encoding.UTF8.GetString(buffer, 0, len);
                string[] packets = request.Split(';');
                if (packets[0] == "login")
                {
                    CheckLogin(packets[1], packets[2], socket);
                }
                else if (packets[0] == "register")
                {
                    Registration(packets[1], packets[2], packets[3], socket);
                }
                else if (packets[0] == "vote")
                {

                }
                else if (packets[0] == "create_vote")
                {
                    CreateVote(packets[1], packets[2], packets[3], socket);
                }
                else if (packets[0] == "get_votes")
                {
                    GetVotes(socket);
                }
                else if (packets[0] == "get_vote_options")
                {
                    GetVoteOptions(packets[1], socket);
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
            finally
            {
                socket.Close();
            }            
        }

        private void GetVotes(Socket socket)
        {
            using var context = new Context.VoteContext();

            var activeVotes = context.Votes
                .Include(v => v.Options)
                .ThenInclude(o => o.UserVotes) 
                .Where(v => v.IsActive && v.EndDate > DateTime.Now)
                .ToList();

            var voteEntries = activeVotes.Select(v =>
            {
                int totalVotes = v.Options.Sum(o => o.UserVotes.Count);
                return $"{v.Id},{v.Title},{totalVotes}";
            });

            string response = "votes_list;" + string.Join("|", voteEntries);

            socket.Send(Encoding.UTF8.GetBytes(response));
            Log($"Send a vote list({activeVotes.Count}.)");
        }

        private void CheckLogin(string login, string password, Socket socket)
        {   
            using var context = new Context.VoteContext();
            var user = context.Users.FirstOrDefault(u => u.Login == login);
            string DecryptPassword = null;
            try { DecryptPassword = Encoding.UTF8.GetString(Convert.FromBase64String(password)); }
            catch { }
            

            if (user != null && user.EncryptedPassword == DecryptPassword)
            {
                socket.Send(Encoding.UTF8.GetBytes($"login_success;{user.NickName};{user.Role}"));
                Log($"User {login} login");
            }
            else
            {
                socket.Send(Encoding.UTF8.GetBytes("login_failed"));
                Log($"User {login} not found. Login failed");
            }
        }

        private void Registration(string login, string password, string nickname, Socket socket)
        {
            using var context = new Context.VoteContext();
            var existingUser = context.Users.FirstOrDefault(u => u.Login == login);
            if (existingUser != null)
            {
                Log($"Register failed: login '{login}' in use");
                socket.Send(Encoding.UTF8.GetBytes("register_failed; UserName in use"));
                return;
            }
            string decryptedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(password));

                var NewUser = new User
                {
                    Login = login,
                    EncryptedPassword = decryptedPassword,
                    NickName = nickname,
                    Role = "User"
                };
                context.Users.Add(NewUser);
                context.SaveChanges();

                Log($"New user success register: {login}");
                socket.Send(Encoding.UTF8.GetBytes("register_success"));
                return;
        }

        private void CreateVote(string title, string voteOptions, string endTime, Socket socket)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    socket.Send(Encoding.UTF8.GetBytes("create_vote_failed;empty_title"));
                    return;
                }

                string[] options = voteOptions.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (options.Length < 2)
                {
                    socket.Send(Encoding.UTF8.GetBytes("create_vote_failed;not_enough_options"));
                    return;
                }
                DateTime endDateTime = DateTime.Parse(endTime, null, System.Globalization.DateTimeStyles.RoundtripKind);

                if(endDateTime < DateTime.Now)
                {
                    socket.Send(Encoding.UTF8.GetBytes("create_vote_failed;invalid_date"));
                    return;
                }

                using var context = new Context.VoteContext();
                var newVote = new Vote
                {
                    Title = title,
                    StartDate = DateTime.Now,
                    EndDate = endDateTime,
                    IsActive = true
                };
                foreach (string optionText in options)
                {
                    newVote.Options.Add(new Tables.VoteOption { Text = optionText.Trim() });
                }

                context.Votes.Add(newVote);
                context.SaveChanges();
                Log($"Створено голосування: '{title}' ({options.Length} варіантів), завершення: {endDateTime:g}");
                socket.Send(Encoding.UTF8.GetBytes("create_vote_success"));
            }
            catch (Exception ex) {
                Log($"Помилка створення голосування: {ex.Message}");
                socket.Send(Encoding.UTF8.GetBytes("create_vote_failed;server_error"));
            }
        }

        private void GetVoteOptions(string voteIdStr, Socket socket)
        {
            if (!int.TryParse(voteIdStr, out int voteId))
            {
                socket.Send(Encoding.UTF8.GetBytes("vote_options_failed;invalid_id"));
                return;
            }

            using var context = new Context.VoteContext();
            var vote = context.Votes.Include(v => v.Options).FirstOrDefault(v => v.Id == voteId);

            if (vote == null)
            {
                socket.Send(Encoding.UTF8.GetBytes("vote_options_failed;not_found"));
                return;
            }

            var entries = vote.Options.Select(o => $"{o.Id},{o.Text}");
            string response = $"vote_options;{vote.Title};" + string.Join("|", entries);
            socket.Send(Encoding.UTF8.GetBytes(response));
        }

        public void StopServer()
        {
            ServerRunning = false;
            listener.Close();
        }
    }
}