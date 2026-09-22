using System;
using System.Collections.Generic;
using System.Text;

namespace Voting_Server_App.Tables
{
    public class UserVote
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int VoteOptionId { get; set; } 

        public User User { get; set; }
        public VoteOption VoteOption { get; set; }
    }
}
