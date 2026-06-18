using Dapper;
using FinalAssigenment.Models;
using Microsoft.Data.SqlClient;
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

    public List<Shelf> ShelfList;
    public Command RequestCommand;
    public SystemInformation GetSystemInfo;

    public async Task<SystemInformation> SelectInfoAsync()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                //var sql = @"";
                //GetSystemInfo = await connection.QueryAsync<SystemInformation>(sql);

                return GetSystemInfo;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "DB接続異常");
                throw;
            }
        }
    }

    public async Task<Command> GetCommandRequestAsync(DateTime sendAt)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                //var sql = @"";

                //GetSystemInfo = await connection.QueryAsync<SystemInformation>(sql);

                return RequestCommand;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "予約一覧取得失敗");
                throw;
            }
        }
    }
}
