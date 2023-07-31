using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Models.DTOs.Input;
using Services.Helpers;
using Services.Interfaces;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Services.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IFileService _fileService;
        private readonly ConnectionOptions _connectionOptions;

        public TransactionService(IFileService fileService, ConnectionOptions connectionOptions)
        {
            _fileService = fileService;
            _connectionOptions = connectionOptions;
        }

        public async Task ImportTransactionsFromCsvAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var transactions = new List<Transaction>();

            using (var stream = file.OpenReadStream())
            {
                transactions.AddRange(_fileService.ImportTransactionsFromCSV(stream));
            }

            if (!transactions.Any())
                throw new BadRequestException("Imported file is empty");

            await UpdateOrInsertTransactionsAsync(transactions, cancellationToken);
        }

        public async Task<byte[]> ExportTransactionsByConditionAsync(RequestConfigurationDto requestConfiguration, CancellationToken cancellationToken)
        {
            using var sqlConnection = new SqlConnection(_connectionOptions.ConnectionString);
            sqlConnection.Open();
            using var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandType = CommandType.Text;
            var filters = ConfigureFiltersAndFields(requestConfiguration);

            var commandText = new StringBuilder("SELECT ");
            commandText.Append(filters[0]);
            commandText.Append(" FROM Transactions WHERE Status in ");
            commandText.Append(filters[1]);
            commandText.Append(" AND Destination in ");
            commandText.Append(filters[2]);
            commandText.Append(";");
            sqlCommand.CommandText = commandText.ToString();

            var dataReader = await sqlCommand.ExecuteReaderAsync(cancellationToken);
            var csvData = BuildCsvDataFromDb(dataReader);
            dataReader.Close();
            return csvData;
        }

        public async Task<List<Transaction>> GetTransactionsByConditionAsync(RequestFiltersDto requestFilters, CancellationToken cancellationToken)
        {
            using var sqlConnection = new SqlConnection(_connectionOptions.ConnectionString);
            sqlConnection.Open();
            using var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandType = CommandType.Text;
            var filters = ConfigureFilters(requestFilters);

            var commandText = new StringBuilder("SELECT * FROM Transactions WHERE Status in ");
            commandText.Append(filters[0]);
            commandText.Append(" AND Destination in ");
            commandText.Append(filters[1]);
            commandText.Append(";");
            sqlCommand.CommandText = commandText.ToString();

            var dataReader = await sqlCommand.ExecuteReaderAsync(cancellationToken);
            var result = new List<Transaction>();
            while (dataReader.Read())
            {
                result.Add(new Transaction
                {
                    Id = dataReader.GetInt32("Id"),
                    Status = (TransactionStatus)dataReader.GetInt32("Status"),
                    Destination = (PaymentDestination)dataReader.GetInt32("Destination"),
                    ClientName = dataReader.GetString("ClientName"),
                    AmountUSD = double.Parse(dataReader.GetValue("AmountUSD").ToString())
                });
            }
            return result;
        }

        public async Task<List<Transaction>> GetTransactionsOfClientNameAsync(string name, CancellationToken cancellationToken)
        {
            using var sqlConnection = new SqlConnection(_connectionOptions.ConnectionString);
            sqlConnection.Open();

            using var sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.CommandText = $"SELECT * FROM Transactions WHERE ClientName LIKE '%{name}%';";

            var dataReader = await sqlCommand.ExecuteReaderAsync(cancellationToken);
            var result = new List<Transaction>();

            while (dataReader.Read())
            {
                result.Add(new Transaction
                {
                    Id = dataReader.GetInt32("Id"),
                    Status = (TransactionStatus)dataReader.GetInt32("Status"),
                    Destination = (PaymentDestination)dataReader.GetInt32("Destination"),
                    ClientName = dataReader.GetString("ClientName"),
                    AmountUSD = double.Parse(dataReader.GetValue("AmountUSD").ToString())
                });
            }

            return result;
        }

        public async Task UpdateTransactionByIdAsync(RequestUpdateStatusDto updateStatusDto, CancellationToken cancellationToken)
        {
            if (updateStatusDto.Status > 2 || updateStatusDto.Status < 0)
                throw new BadRequestException("Status does not exist");
            using var sqlConnection = new SqlConnection(_connectionOptions.ConnectionString);
            sqlConnection.Open();
            using var sqlTransaction = sqlConnection.BeginTransaction();
            using var sqlTurnOnIdentityInsert = sqlConnection.CreateCommand();
            using var sqlTurnOffIdentityInsert = sqlConnection.CreateCommand();
            using var sqlCommand = sqlConnection.CreateCommand();

            ConfigureSqlCommands(new Dictionary<SqlCommand, string>
            {
                { sqlTurnOnIdentityInsert, "SET IDENTITY_INSERT Transactions ON"},
                { sqlTurnOffIdentityInsert, "SET IDENTITY_INSERT Transactions OFF"},
                { sqlCommand, "UPDATE Transactions SET Status=@status WHERE Id=@id"}
            }, sqlTransaction);

            sqlCommand.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
            sqlCommand.Parameters.Add(new SqlParameter("@status", SqlDbType.Int));
            
            try
            {
                sqlTurnOnIdentityInsert.ExecuteNonQuery();
                sqlCommand.Parameters[0].Value = updateStatusDto.Id;
                sqlCommand.Parameters[1].Value = updateStatusDto.Status;

                if (await Task.Run(() => sqlCommand.ExecuteNonQuery()) != 1)
                    throw new DataIsNotUpdatedException();
                sqlTurnOffIdentityInsert.ExecuteNonQuery();
                sqlTransaction.Commit();
            }
            catch (Exception)
            {
                sqlTransaction.Rollback();
                throw;
            }
        }

        private void ConfigureSqlCommands(Dictionary<SqlCommand, string> commands, SqlTransaction transaction)
        {
            foreach (var commandPair in commands)
            {
                commandPair.Key.CommandType = CommandType.Text;
                commandPair.Key.Transaction = transaction;
                commandPair.Key.CommandText = commandPair.Value;
            }
        }

        private async Task InsertTransactionsAsync(SqlCommand sqlCommand, IEnumerable<Transaction> transactions, CancellationToken cancellationToken)
        {
            foreach (var transaction in transactions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sqlCommand.Parameters[0].Value = transaction.Id;
                sqlCommand.Parameters[1].Value = transaction.Status;
                sqlCommand.Parameters[2].Value = transaction.Destination;
                sqlCommand.Parameters[3].Value = transaction.ClientName;
                sqlCommand.Parameters[4].Value = transaction.AmountUSD;
                if (await Task.Run(() => sqlCommand.ExecuteNonQuery()) != 1)
                {
                    throw new InvalidProgramException();
                }
            }
        }

        private StringBuilder[] ConfigureFiltersAndFields(RequestConfigurationDto requestConfiguration)
        {
            var fieldsFilter = new StringBuilder("Status, Destination, ClientName, AmountUSD, Id");
            var statusesFilter = new StringBuilder("(0, 1, 2)");
            var destinationsFilter = new StringBuilder("(0, 1)");
            if (!requestConfiguration.Fields.IsNullOrEmpty())
            {
                fieldsFilter.Clear();
                var transactionType = typeof(Transaction);
                var transactionFields = transactionType.GetProperties();
                foreach (var field in requestConfiguration.Fields.Distinct())
                {
                    fieldsFilter.Append(transactionFields[field].Name);
                    fieldsFilter.Append(", ");
                }
                fieldsFilter.Remove(fieldsFilter.Length - 2, 2);
            }
            if (!requestConfiguration.Statuses.IsNullOrEmpty())
            {
                statusesFilter = new StringBuilder("(" + string.Join(", ", requestConfiguration.Statuses) + ")");
            }
            if (!requestConfiguration.Destinations.IsNullOrEmpty())
            {
                destinationsFilter = new StringBuilder("(" + string.Join(", ", requestConfiguration.Destinations) + ")");
            }
            return new StringBuilder[] { fieldsFilter, statusesFilter, destinationsFilter };
        }

        private StringBuilder[] ConfigureFilters(RequestFiltersDto requestFilters)
        {
            var statusesFilter = new StringBuilder("(0, 1, 2)");
            var destinationsFilter = new StringBuilder("(0, 1)");

            if (!requestFilters.Statuses.IsNullOrEmpty())
            {
                statusesFilter = new StringBuilder("(" + string.Join(", ", requestFilters.Statuses) + ")");
            }

            if (!requestFilters.Destinations.IsNullOrEmpty())
            {
                destinationsFilter = new StringBuilder("(" + string.Join(", ", requestFilters.Destinations) + ")");
            }

            return new StringBuilder[] { statusesFilter, destinationsFilter };
        }

        private byte[] BuildCsvDataFromDb(SqlDataReader dataReader)
        {
            var csvData = new StringBuilder();
            var currentRow = new StringBuilder();
            while (dataReader.Read())
            {
                for (int i = 0; i < dataReader.FieldCount; i++)
                {
                    currentRow.Append(dataReader[i]);
                    currentRow.Append(", ");
                }
                currentRow.Remove(currentRow.Length - 2, 2);
                csvData.AppendLine(currentRow.ToString());
                currentRow.Clear();
            }
            return Encoding.UTF8.GetBytes(csvData.ToString());
        }

        private async Task UpdateOrInsertTransactionsAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken)
        {
            using var sqlConnection = new SqlConnection(_connectionOptions.ConnectionString);
            sqlConnection.Open();
            using var sqlTransaction = sqlConnection.BeginTransaction();
            using var sqlTurnOnIdentityInsert = sqlConnection.CreateCommand();
            using var sqlTurnOffIdentityInsert = sqlConnection.CreateCommand();
            using var sqlCommand = sqlConnection.CreateCommand();

            ConfigureSqlCommands(new Dictionary<SqlCommand, string>
            {
                { sqlTurnOnIdentityInsert, "SET IDENTITY_INSERT Transactions ON"},
                { sqlTurnOffIdentityInsert, "SET IDENTITY_INSERT Transactions OFF"},
                { sqlCommand, "UPDATE Transactions SET Status=@status, Destination=@destination, ClientName=@clientname, AmountUSD=@amountusd WHERE Id=@id IF @@ROWCOUNT = 0 INSERT INTO Transactions (Id, Status, Destination, ClientName, AmountUSD) VALUES (@id, @status, @destination, @clientname, @amountusd)"}
            }, sqlTransaction);

            sqlCommand.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
            sqlCommand.Parameters.Add(new SqlParameter("@status", SqlDbType.Int));
            sqlCommand.Parameters.Add(new SqlParameter("@destination", SqlDbType.Int));
            sqlCommand.Parameters.Add(new SqlParameter("@clientname", SqlDbType.NVarChar));
            sqlCommand.Parameters.Add(new SqlParameter("@amountusd", SqlDbType.Float));
            try
            {
                sqlTurnOnIdentityInsert.ExecuteNonQuery();
                await InsertTransactionsAsync(sqlCommand, transactions, cancellationToken);
                sqlTurnOffIdentityInsert.ExecuteNonQuery();
                sqlTransaction.Commit();
            }
            catch (Exception)
            {
                sqlTransaction.Rollback();
                throw;
            }
        }
    }
}