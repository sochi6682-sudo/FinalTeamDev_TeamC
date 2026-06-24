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

    private SystemInformation _getResultInfomation;

    public async Task<SystemInformation> SelectInfomationAsync()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sql = @"
                      -- 搬送指示の未完了を全件、完了を100件取得
                      SELECT 
                      command_id AS CommandId,
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
                      SELECT 
                      TOP(100) * FROM commands
                      WHERE command_status IN (2,3);
                      -- 棚の情報を全件取得
                      SELECT 
                      shelf_location AS ShelfLocation,
                      stored_carrier_id AS StoredCarrierId,
                      reservation AS Reservation,
                      storage_at AS StorageAt
                      FROM shelves;";
                using var multi = await connection.QueryMultipleAsync(sql);

                var commands = (await multi.ReadAsync<Command>()).ToList();
                var shelves = (await multi.ReadAsync<Shelf>()).ToList();

                _getResultInfomation = new()
                {
                    Status = [],
                    Commands = commands,
                    Shelves = shelves
                };

                return _getResultInfomation;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[Error] DB接続異常");

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
                var sql = @"
                      -- 搬送指示IDを格納する変数を定義
                      DECLARE @OutputTable TABLE (id VARCHAR(10));

                      -- INSERTと同時に、生成された搬送指示IDを変数に直接代入する
                      INSERT INTO commands (
                          carrier_id, command_type, eqp_name, location, reception_at,command_status
                      )
                      OUTPUT INSERTED.command_id INTO @OutputTable
                      VALUES (
                         @CarrierId, @CommandType, @EqpName, @Location, @ReceptionAt, @CommandStatus
                      );

                      -- CommandTypeが0なら棚を更新
                      IF @CommandType = 0
                      BEGIN
                         UPDATE shelves
                         SET 
                           reservation = (SELECT id FROM @OutputTable)
                         WHERE shelf_location = @Location;
                      END";

                await connection.ExecuteAsync(sql, insertData);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[Error] DB接続異常");
                throw;
            }
        }
    }
    public async Task<EquipmentCommand> SelectCommandRequestAsync(string eqpName, DateTime sendAt)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sql = @"
                      -- 後半の処理で使い回すための変数を宣言
                      DECLARE @SelectedId VARCHAR(10), @SelectedType INT, @SelectedLocation VARCHAR(50);

                      -- 条件（0→1）に合う最も古い未送信の1件を特定
                      WITH TargetCommand AS (
                           SELECT TOP 1 *
                           FROM commands
                           WHERE command_status = 0 
                           AND eqp_name = @EqpName
                           ORDER BY 
                           command_type ASC,
                           reception_at ASC
                      )
                      -- 送信時刻と搬送指示状態を更新
                      UPDATE TargetCommand
                      SET 
                        send_at = @SendAt,
                        command_status = 1,
                        @SelectedId = command_id,
                        @SelectedType = command_type,
                        @SelectedLocation = location;

                      -- 出庫（0）の時だけ、棚の予約を更新
                      IF @SelectedType = 0
                      BEGIN
                        UPDATE shelves
                        SET 
                          reservation = @SelectedId
                        WHERE shelf_location = @SelectedLocation;
                      END

                      -- 設備機器へ返すデータをSELECT
                      SELECT 
                      command_id AS CommandId,
                      command_type AS CommandType,
                      carrier_id AS CarrierId,
                      eqp_name AS EqpName,
                      location AS Location,
                      command_status AS CommandStatus
                      FROM commands
                      WHERE command_id = @SelectedId;";

                return await connection.QueryFirstOrDefaultAsync<EquipmentCommand>(sql,new 
                  { 
                    EqpName = eqpName,
                    SendAt = sendAt
                  });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Error] DB接続異常");
                throw;
            }
            
        }
    }
    public async Task UpdateCommandStatusAsync(string commandId, int commandStatus)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sql = @"
                      UPDATE commands 
                      SET 
                        command_status = @CommandStatus 
                      WHERE command_id = @CommandId;";

                await connection.ExecuteAsync(sql, new
                {
                    CommandStatus = commandStatus,
                    CommandId = commandId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Error] DB接続異常");
                throw;
            }
            
        }
    }
    public async Task UpdateCompletionAsync(EquipmentCommand completion, DateTime CompletionAt)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sql = @"
                      -- commandsテーブルの更新（入庫・出庫共通）
                      UPDATE commands
                      SET 
                        command_status = @CommandStatus,
                        completion_at = @CompletionAt
                      WHERE 
                        command_id = @CommandId;

                      -- shelvesテーブルの更新（CommandTypeで分岐）
                      IF @CommandType = 1 AND @CommandStatus = 2
                      BEGIN
                        -- 入庫の場合：キャリアIDと入庫時刻を更新
                        UPDATE shelves
                        SET 
                          stored_carrier_id = @CarrierId,
                          storage_at = @CompletionAt
                        WHERE 
                          shelf_location = @Location;
                      END
                      ELSE IF @CommandType = 0
                      BEGIN
                         -- 出庫の場合：キャリアID、入庫時刻、予約をnullにする
                         UPDATE shelves
                         SET 
                           stored_carrier_id = NULL,
                           reservation = NULL,
                           storage_at = NULL
                         WHERE 
                           shelf_location = @Location;
                      END";
                await connection.ExecuteAsync(sql, new
                {
                    CommandId = completion.CommandId,
                    CommandType = completion.CommandType,
                    CarrierId = completion.CarrierId,
                    Location = completion.Location,
                    CommandStatus = completion.CommandStatus,
                    CompletionAt = CompletionAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Error] DB接続異常");
                throw;
            }

        }
    }

    public async Task<(List<Shelf> ShelfList, List<(string CarrierId, string Location)>)>SelectShelfInformationAsync(string prefix)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            try
            {
                var sql = @"
                      --指定された設備IDの棚を取得
                      SELECT 
                      shelf_location AS ShelfLocation,
                      stored_carrier_id AS StoredCarrierId,
                      reservation AS Reservation,
                      storage_at AS StorageAt
                      FROM shelves
                      WHERE shelf_location LIKE @Prefix; 
                      --未完了の搬送指示のキャリアIDと棚を取得
                      SELECT 
                      carrier_id AS CarrierId,
                      location AS Location
                      FROM commands
                      WHERE command_status IN (0,1);";

                using var multi = await connection.QueryMultipleAsync(sql, new
                {
                    Prefix = prefix
                });

                var shelfList = (await multi.ReadAsync<Shelf>()).ToList();
                var incompleteCommandList = (await multi.ReadAsync<(string CarrierId, string Location)>()).ToList();
                return (shelfList, incompleteCommandList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Error] DB接続異常");
                throw;
            }

        }
    }

    public async Task UpdateTimeOutAsync(EquipmentCommand sendCommand)
    {
        using var connection = new SqlConnection(connectionString);

        try
        {
            var sql = @"
              -- 該当する搬送指示を異常完了にする
              UPDATE commands 
              SET 
　　　　　　　　command_status = 3,
                completion_at = GETDATE()
              WHERE command_id = @CommandId and command_status IN (0,1);

              -- 出庫なら、棚の予約とキャリアIDをnullにする
              IF @CommandType = 0
              BEGIN
                  UPDATE shelves 
                  SET stored_carrier_id = NULL, 
                      reservation = NULL,
                      storage_at = NULL
                  WHERE shelf_location = @Location;
              END";

            await connection.ExecuteAsync(sql, new
            {
                CommandId = sendCommand.CommandId,
                Location = sendCommand.Location,
                CommandType = sendCommand.CommandType
            });
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[Error] DB接続異常");
            throw;
        }
    }
}
