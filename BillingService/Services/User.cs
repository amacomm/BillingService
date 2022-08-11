namespace BillingService.Services
{
    public class User
    {
        public string name { get; set; }
        public int rating { get; set; }
        public List<long> coin { get; set; }
        public User(string name, int rating)
        {
            this.name = name;
            this.rating = rating;
            this.coin = new List<long>();
        }
    }
}