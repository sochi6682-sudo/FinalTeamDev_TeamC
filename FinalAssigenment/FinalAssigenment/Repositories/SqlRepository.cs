using Dapper;
using FinalAssigenment.Models;
using Microsoft.Data.SqlClient;
using System.ComponentModel.Design;
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
    public SystemInformation GetResultInfomation;

    public async Task<SystemInformation> SelectInfomationAsync()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sqlCommands = @"";
                var sqlShelves = "SELECT * FROM shelves;";
                Task CommandsTask = connection.QueryAsync<Command>(sqlCommands);
                Task shelvesTask = connection.QueryAsync<Shelf>(sqlShelves);
                await Task.WhenAll(CommandsTask, shelvesTask);
                GetResultInfomation = new()
                {
                    InfomationEqpStates = [],

                };

                return GetResultInfomation;
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
                //_logger.LogError(ex, "DB接続異常");
                throw;
            }
        }
    }
    public async Task UpdateCommandStatusAsync(string commandId, int? commandStatus)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sql = @"
                        UPDATE commands 
                        SET command_status = @CommandStatus 
                        WHERE id = @CommandId;";

                await connection.ExecuteAsync(sql, new
                {
                    CommandStatus = commandStatus,
                    CommandId = commandId
                });

            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "DB接続異常");
                throw;
            }
        }
    }
    public async Task UpdateStatusAndTimeAsync(EquipmentCommand completion, DateTime CompletionAt)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                //var sql = @"";

                //GetSystemInfo = await connection.QueryAsync<SystemInformation>(sql);

            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "DB接続異常");
                throw;
            }
        }
    }
}
