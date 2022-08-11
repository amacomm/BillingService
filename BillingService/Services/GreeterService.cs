using Grpc.Core;
using System.Text.Json;

namespace BillingService.Services
{
    public class GreeterService : Greeter.GreeterBase
    {
        static User[] users;
        static SortedDictionary<long, List<string>> coins;
        public GreeterService()
        {
            if (users == null)
            {
                StreamReader r = new StreamReader("Users.json");
                string json = r.ReadToEnd();
                users = JsonSerializer.Deserialize<User[]>(json);
                Array.Sort(users, (x, y) => { return x.rating > y.rating ? 1 : x.rating < y.rating ? -1 : 0; });
                coins = new SortedDictionary<long, List<string>>();
            }
        }
        public override Task ListUsers(None request, IServerStreamWriter<UserProfile> responseStream, ServerCallContext context)
        {
            foreach (var user in users)
                responseStream.WriteAsync(new UserProfile() { Name = user.name, Amount = user.coin.Count });
            return Task.CompletedTask;
        }
        public override Task<Response> MoveCoins(MoveCoinsTransaction request, ServerCallContext context)
        {
            int i=-1, j=-1;
            for(int k = 0; k < users.Length; k++)
            {
                if (users[k].name == request.SrcUser)
                    i = k;
                if (users[k].name == request.DstUser)
                    j = k;
            }
            if (i==-1||j==-1)
                return Task.FromResult(new Response()
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "This person is not in the system"
                });
            else if (request.Amount<0)
                return Task.FromResult(new Response()
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "Transfer of a negative number of coins"
                });
            else if (users[i].coin.Count < request.Amount)
                return Task.FromResult(new Response()
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "Src dosen`t have enough coins"
                });
            long coin;
            for(int k =0; k < request.Amount; k++)
            {
                coin = users[i].coin.Last();
                users[i].coin.RemoveAt(users[i].coin.Count - 1);
                coins[coin].Add(users[j].name);
                users[j].coin.Add(coin);
            }
            return Task.FromResult(new Response()
            {
                Status = Response.Types.Status.Ok,
                Comment = "All done"
            });
        }
        public override Task<Response> CoinsEmission(EmissionAmount request, ServerCallContext context)
        {
            if(request.Amount<users.Length)
                return Task.FromResult(new Response()
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "Coins are not enough"
                });
            else if(users.Length==0)
                return Task.FromResult(new Response()
                {
                    Status = Response.Types.Status.Failed,
                    Comment = "There is no one to give any coins"
                });
            long Amount = request.Amount;
            double all_reting = 0;
            foreach (var user in users)
                all_reting += user.rating;
            Random rand;
            long id;
            for (int i = 0; i < users.Length; i++)
            {
                int coin = (int)Math.Round(Amount * users[i].rating / all_reting);
                coin = coin < 1 ? 1 : coin;
                Amount -= coin;

                for (int j = 0; j < coin; j++)
                {
                    rand = new Random();
                    id = rand.NextInt64();
                    while (coins.ContainsKey(id))
                        id =rand.NextInt64();
                    List<string> s=new List<string>() { users[i].name };
                    coins.Add(id, s);
                    users[i].coin.Add(id);
                }
                all_reting -= users[i].rating;
            }
            return Task.FromResult(new Response()
            {
                Status = Response.Types.Status.Ok,
                Comment = "All done"
            });
        }
        public override Task<Coin> LongestHistoryCoin(None request, ServerCallContext context)
        {
            long id=0;
            int max=0;
            foreach (var el in coins)
                if (el.Value.Count > max)
                {
                    id = el.Key;
                    max = el.Value.Count;
                }
            return Task.FromResult(new Coin() { Id=id,
                History=String.Join(", ", coins[id])
            });
        }
    }
}