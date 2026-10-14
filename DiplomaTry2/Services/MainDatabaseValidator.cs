using DiplomaTry2.Data;

namespace DiplomaTry2.Services;

public interface IMainDatabaseValidator
{
    Task ValidateAsync(string connection);
}

public sealed class MainDatabaseValidator : IMainDatabaseValidator
{
    public Task ValidateAsync(string connection) => RuntimeDatabase.ValidateAsync(connection);
}
