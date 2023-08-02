using Services.Services;

namespace UnitTests
{
    public class FileServiceTests
    {
        private readonly FileService _fileService;
        private const string _csvData = "TransactionId,Status,Type,ClientName,Amount\r\n1,Pending,Withdrawal,Dale Cotton,$28.43\r\n2,Completed,Refill,Paul Carter,$45.16";
        private const string _csvDataEmpty = "";
        private const string _csvDataInvalid = "InvalidData";


        public FileServiceTests()
        {
            _fileService = new FileService();
        }

        [Fact]
        public void ImportTransactionsFromCSV_ValidData_ReturnsTransactions()
        {
            // Arrange
            var inputStream = GenerateStreamFromString(_csvData);

            // Act
            var result = _fileService.ImportTransactionsFromCSV(inputStream);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void ImportTransactionsFromCSV_EmptyData_ReturnsEmptyList()
        {
            // Arrange
            var inputStream = GenerateStreamFromString(_csvDataEmpty);

            // Act
            var result = _fileService.ImportTransactionsFromCSV(inputStream);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ImportTransactionsFromCSV_InvalidData_ReturnsEmptyList()
        {
            // Arrange
            var inputStream = GenerateStreamFromString(_csvDataInvalid);

            // Act
            var result = _fileService.ImportTransactionsFromCSV(inputStream);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        private Stream GenerateStreamFromString(string data)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(data);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}