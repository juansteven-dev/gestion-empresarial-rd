using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GestionEmpresarial.UnitTests;

[CollectionDefinition("API integration")]
public sealed class ApiIntegrationCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "API integration";
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly DirectoryInfo dataProtectionKeys = new(
        Path.Combine(Path.GetTempPath(), $"GestionEmpresarialTests-{Guid.NewGuid():N}"));

    public SaleTestSaveChangesInterceptor SaleSaveChangesInterceptor { get; } = new();

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Server=localhost;Database=GestionEmpresarialTests;Trusted_Connection=True;TrustServerCertificate=True");
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    public async Task<TestAccount> CreateUserAsync(string role)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var userName = $"test-{Guid.NewGuid():N}";
        var password = $"A9!b{Guid.NewGuid():N}Z";
        var user = new IdentityUser
        {
            UserName = userName,
            Email = $"{userName}@example.test"
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Test user could not be created: {string.Join("; ", createResult.Errors.Select(error => error.Code))}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Test user role could not be assigned: {string.Join("; ", roleResult.Errors.Select(error => error.Code))}");
        }

        return new TestAccount(userName, password);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Error);
        });
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().PersistKeysToFileSystem(dataProtectionKeys);
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseInMemoryDatabase("GestionEmpresarialAuthenticationTests")
                    .AddInterceptors(SaleSaveChangesInterceptor));
        });
    }
}

public sealed class SaleTestSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly object rowVersionLock = new();
    private readonly Dictionary<int, byte[]> currentRowVersions = [];
    private int isBarrierArmed;
    private int arrivals;
    private TaskCompletionSource releaseBarrier = CompletedSource();

    public void ArmForTwoConcurrentSales()
    {
        arrivals = 0;
        releaseBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Volatile.Write(ref isBarrierArmed, 1);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return result;
        }

        if (Volatile.Read(ref isBarrierArmed) == 1
            && context.ChangeTracker.Entries<Sale>().Any(entry => entry.State == EntityState.Added))
        {
            if (Interlocked.Increment(ref arrivals) == 2)
            {
                Volatile.Write(ref isBarrierArmed, 0);
                releaseBarrier.TrySetResult();
            }

            await releaseBarrier.Task.WaitAsync(cancellationToken);
        }

        var modifiedProducts = context.ChangeTracker.Entries<Product>()
            .Where(entry => entry.State == EntityState.Modified)
            .ToArray();
        lock (rowVersionLock)
        {
            foreach (var entry in modifiedProducts)
            {
                var originalVersion = entry.Property(nameof(Product.RowVersion)).OriginalValue as byte[] ?? [];
                if (currentRowVersions.TryGetValue(entry.Entity.ProductId, out var savedVersion)
                    && !savedVersion.AsSpan().SequenceEqual(originalVersion))
                {
                    throw new DbUpdateConcurrencyException("The product stock changed during the test sale.");
                }
            }

            foreach (var entry in modifiedProducts)
            {
                var nextVersion = Guid.NewGuid().ToByteArray();
                currentRowVersions[entry.Entity.ProductId] = nextVersion;
                var rowVersion = entry.Property(nameof(Product.RowVersion));
                rowVersion.CurrentValue = nextVersion;
                rowVersion.IsModified = true;
            }
        }

        return result;
    }

    private static TaskCompletionSource CompletedSource()
    {
        var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult();
        return source;
    }
}

public sealed record TestAccount(string UserName, string Password);
