using System;

namespace PumpAndDumpBot.Data.Objects
{
    public class Announcement
    {
        public DateTime Date { get; set; }
        public string Coin { get; set; }
        public string Pair { get; set; }
        public string PairGoal { get; set; }
    }
}