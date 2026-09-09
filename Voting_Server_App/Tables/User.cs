using System;
using System.Collections.Generic;
using System.Text;

namespace Voting_Server_App.Tables
{
    public class User
    {
        public int Id { get; set; }
        public string Nick { get; set; } = default!;
        public string EncryptedPassword { get; set; } = default!;

    }
}
