namespace FinalAssigenment.Repositories;

public class SqlRepository
{
    private readonly ILogger<SqlRepository> _logger;
    private readonly IConfiguration _config;
    private readonly string connectionString;
    public SqlRepository(IConfiguration config, ILogger<SqlRepository> logger)
    {
        _config = config;
        connectionString = _config.GetConnectionString("DefaultConnection");
        _logger = logger;
    }
}
