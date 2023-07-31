using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Models.DTOs.Input;

namespace Transactions.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("import-transactions-from-csv")]
        public async Task<IActionResult> ImportTransactionsFromCsvAsync(IFormFile file, CancellationToken cancellationToken)
        {
            await _transactionService.ImportTransactionsFromCsvAsync(file, cancellationToken);
            return Ok();
        }

        [HttpPost("update-transaction-by-id")]
        public async Task<IActionResult> UpdateTransactionByIdAsync([FromQuery] RequestUpdateStatusDto updateStatusDto, CancellationToken cancellationToken)
        {
            await _transactionService.UpdateTransactionByIdAsync(updateStatusDto, cancellationToken);
            return Ok();
        }

        [HttpGet("export-transactions-to-csv")]
        public async Task<FileContentResult> ExportTransactionsToCsvByConditionAsync([FromQuery] RequestConfigurationDto requestConfiguration, CancellationToken cancellationToken)
        {
            var tableData = await _transactionService.ExportTransactionsByConditionAsync(requestConfiguration, cancellationToken);
            return File(tableData, "text/csv", "exported");
        }

        [HttpGet("get-transactions-by-condition")]
        public async Task<IActionResult> GetTransactionsByConditionAsync([FromQuery] RequestFiltersDto requestFilters, CancellationToken cancellationToken)
        {
            var tableData = await _transactionService.GetTransactionsByConditionAsync(requestFilters, cancellationToken);
            return Ok(tableData);
        }

        [HttpGet("get-transactions-of-client")]
        public async Task<IActionResult> GetTransactionsOfClientAsync([FromQuery] string name, CancellationToken cancellationToken)
        {
            var tableData = await _transactionService.GetTransactionsOfClientNameAsync(name, cancellationToken);
            return Ok(tableData);
        }
    }
}