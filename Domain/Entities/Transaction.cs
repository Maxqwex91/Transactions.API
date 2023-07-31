using Domain.Enums;

namespace Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public TransactionStatus Status { get; set; }
        public PaymentDestination Destination { get; set; }
        public string? ClientName { get; set; }
        public double AmountUSD { get; set; }
    }
}