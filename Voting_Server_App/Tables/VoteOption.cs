using System;
using System.Collections.Generic;
using System.Text;

namespace Voting_Server_App.Tables
{
    public class VoteOption
    {
        public int Id { get; set; }
        public string Text { get; set; } = default!;
        public int VoteId { get; set; }

        public Vote Vote { get; set; }
        public List<UserVote> UserVotes { get; set; }
    }
}
