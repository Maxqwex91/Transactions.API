using Domain.Entities;
using Domain.Enums;

namespace Services.Mappers
{
    public class TransactionsMapper
    {
        public static List<Transaction> FromString(string data)
        {
            var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1);
            var finalList = new List<Transaction>();

            foreach (var line in lines)
            {
                var items = line.Split(',');
                var current = new Transaction
                {
                    Id = Convert.ToInt32(items[0]),
                    Status = Enum.Parse<TransactionStatus>(items[1]),
                    Destination = Enum.Parse<PaymentDestination>(items[2]),
                    ClientName = items[3],
                    AmountUSD = double.Parse(items[4].Trim(new char[] { '$', '\n' }).Replace(".", ","))
                };
                finalList.Add(current);
            }
            return finalList;
        }
    }
}