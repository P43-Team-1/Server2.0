using System;
using System.Collections.Generic;
using System.Text;

namespace Voting_Server_App.Tables
{
    public class Vote
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public List<VoteOption> Options { get; set; } = new();
    }
}
