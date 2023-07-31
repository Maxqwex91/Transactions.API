using Domain.Entities;
using Services.Interfaces;
using Services.Mappers;

namespace Services.Services
{
    public class FileService : IFileService
    {
        public IEnumerable<Transaction> ImportTransactionsFromCSV(Stream file)
        {
            using var streamReader = new StreamReader(file);
            var sData = streamReader.ReadToEnd();
            var data = TransactionsMapper.FromString(sData);
            return data;
        }
    }
}