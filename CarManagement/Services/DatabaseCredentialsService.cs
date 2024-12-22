namespace CarManagement.Services;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

public class DatabaseCredentialsService : IDatabaseCredentialsService
{
    private readonly IAmazonSecretsManager _secretsManagerClient;

    public DatabaseCredentialsService(IAmazonSecretsManager secretsManagerClient)
    {
        _secretsManagerClient = secretsManagerClient;
    }

    public async Task<string> GetConnectionStringAsync(string secretName, string databaseName, string hostName, int portNumber = 5432)
    {
        var secretResponse = await _secretsManagerClient.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = secretName
        });

        string secretString = secretResponse.SecretString;
        var secret = JsonSerializer.Deserialize<Dictionary<string, string>>(secretString);
        if (secret is null) throw new KeyNotFoundException($"Secret Name {secretName} Not Found or is Empty.");

        var username = secret["username"];
        var password = secret["password"];
        var database = databaseName;
        var host = hostName;
        var port = portNumber;

        // Construct the PostgreSQL connection string
        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }
}

