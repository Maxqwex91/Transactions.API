using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Models.DTOs.Input;

namespace Services.Interfaces
{
    public interface ITransactionService
    {
        public Task ImportTransactionsFromCsvAsync(IFormFile file, CancellationToken cancellationToken);
        public Task<byte[]> ExportTransactionsByConditionAsync(RequestConfigurationDto requestConfiguration, CancellationToken cancellationToken);
        public Task<List<Transaction>> GetTransactionsByConditionAsync(RequestFiltersDto requestFilters, CancellationToken cancellationToken);
        public Task<List<Transaction>> GetTransactionsOfClientNameAsync(string name, CancellationToken cancellationToken);
        public Task UpdateTransactionByIdAsync(RequestUpdateStatusDto updateStatusDto, CancellationToken cancellationToken);

    }
}