using System;
using System.Collections.Generic;
using System.Text;

namespace Voting_Server_App
{
    public class DataBaseScript
    {
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
            while (true)
            {
                // Запуст слуїання та відповіді на запити клієнтів
                Thread.Sleep(1000); // Затримка для імітації роботи сервера
            }
        }

        public void StopServer()
        {
            ServerRunning = false;
        }
    }
}