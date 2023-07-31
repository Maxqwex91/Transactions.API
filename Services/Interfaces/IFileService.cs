using Domain.Entities;

namespace Services.Interfaces
{
    public interface IFileService
    {
        public IEnumerable<Transaction> ImportTransactionsFromCSV(Stream file);
    }
}