using Dapper;
using FinalAssigenment.Models;
using Microsoft.Data.SqlClient;
using System.ComponentModel.Design;
using System.Transactions;
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

    private List<Shelf> ShelfList;
    private Command RequestCommand;
    private SystemInformation GetResultInfomation;

    public async Task<SystemInformation> SelectInfomationAsync()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sqlCommands = @"
                                SELECT command_id AS CommandId,
                                command_type AS CommandType,
                                carrier_id AS CarrierId,
                                eqp_name AS EqpName,
                                location AS Location,
                                reception_at AS ReceptionAt,
                                send_at AS SendAt,
                                completion_at AS CompletionAt,
                                command_status AS CommandStatus
                                FROM commands
                                WHERE command_status IN (0,1)
                                UNION ALL
                                SELECT TOP(100) * FROM commands
                                WHERE command_status IN (2,3)
                                ;";
                var sqlShelves = @"
                                SELECT shelf_location AS ShelfLocation,
                                stored_carrier_id AS StoredCarrierId,
                                reservation AS Reservation,
                                storage_at AS StorageAt
                                FROM shelves
                                ;";
                var resultCommands = await connection.QueryAsync<Command>(sqlCommands);
                var commands = resultCommands.ToList();
                var resultShelves = await connection.QueryAsync<Shelf>(sqlShelves);
                var shelves = resultShelves.ToList();
                GetResultInfomation = new()
                {
                    Status = [],
                    Commands = commands,
                    Shelves = shelves
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
    public async Task InsertCommandsAsync(Command insertData)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sqlInsert = @"
                INSERT INTO commands (
                    carrier_id,
                    command_type,
                    eqp_name,
                    location,
                    reception_at,
                    send_at,
                    completion_at,
                    command_status
                )
                VALUES (
                    @CarrierId,
                    @CommandType,
                    @EqpName,
                    @Location,
                    @ReceptionAt,
                    @SendAt,
                    @CompletionAt,
                    @CommandStatus
                )
                ;";

                string generatedId = await connection.QuerySingleAsync<string>(sqlInsert, insertData);
                if (insertData.CommandType == 0)
                {
                    var sqlUpdateShelf = @"
                        UPDATE shelves
                        SET reservation = @Reservation
                        WHERE shelf_location = @ShelfLocation
                        ;";

                    var updateParams = new
                    {
                        Reservation = generatedId,
                        ShelfLocation = insertData.Location   
                    };
                    await connection.ExecuteAsync(sqlUpdateShelf, updateParams);
                }
            }
            catch (Exception ex)
            {
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

    public async Task<List<Shelf>> GetShelfAsync(string prefix)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sql = @"
                        SELECT shelf_location AS ShelfLocation,
                        stored_carrier_id AS StoredCarrierId,
                        reservation AS Reservation,
                        storage_at AS StorageAt
                        FROM shelves
                        WHERE shelf_location LIKE @Prefix
                        ; ";

                var result = await connection.QueryAsync<Shelf>(sql, new { Prefix = prefix });
                ShelfList = result.ToList();
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "DB接続異常");
                throw;
            }
        }
        return ShelfList;
    }
}
