namespace PumpAndDumpBot.Models
{
    public class Affiliate
    {
        public ulong RoleID { get; private set; }
        public int Invites { get; private set; }

        public Affiliate(ulong roleId, int invites)
        {
            RoleID = roleId;
            Invites = invites;
        }
    }
}