namespace CarManagement.Services;

public interface IDatabaseCredentialsService
{
    Task<string> GetConnectionStringAsync(string secretName, string databaseName, string hostName, int portNumber = 5432);
}
