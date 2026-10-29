using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocumentFlow.Infrastructure.Persistence;

// EF tooling must be able to create the model before the API bootstrap runs.
public sealed class DocumentFlowDbContextFactory : IDesignTimeDbContextFactory<DocumentFlowDbContext>
{
    public DocumentFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Database=documentflow;Username=documentflow";
        var options = new DbContextOptionsBuilder<DocumentFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new DocumentFlowDbContext(options);
    }
}
