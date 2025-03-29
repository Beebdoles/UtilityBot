using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UtilityBot.Commands.GGST
{
    public class Structs
    {
        public class DuelData
        {
            public string player1Character { get; set; }
            public int player1Score { get; set; }
            public string player2Character { get; set; }
            public int player2Score { get; set; }
            public int matchNumber { get; set; }
        }

        public class PlayerMatches
        {
            public string player1 { get; set; }
            public int player1Wins { get; set; }
            public string player2 { get; set; }
            public int player2Wins { get; set; }
            public List<DuelData> matches { get; set; }
            public int totalMatches { get; set; }
        }
    }
}
