using FinalAssigenment.Models;
using FinalAssigenment.Repositories;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.ComponentModel.Design;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FinalAssigenment.Services;

public class ShelfSystemService
{
    private readonly ILogger<ShelfSystemService> _logger;
    private readonly SqlRepository _repository;
    private readonly HttpClient _httpClient;
    public ShelfSystemService(
        ILogger<ShelfSystemService> logger,
        SqlRepository repository,
        HttpClient httpClient)
    {
        _logger = logger;
        _repository = repository;
        _httpClient = httpClient;
    }
    private readonly object _lockObject = new object();
    private List<EquipmentState> _eqpStateList = [
            new(){EqpName = "EQP01", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP02", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP03", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0}
        ];

    private readonly List<string> _eqpBaseUrls = [
            "http://localhost:8090",
            "http://localhost:8091",
            "http://localhost:8092"
        ];
    public List<EquipmentState> EqpStateList => _eqpStateList;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _timeoutCancell = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _onlineWatchCancell = new();

    public async Task GetAllEqpStateAsync()
    {
        List<Task> tasks = _eqpBaseUrls.Select(u => GetEqpStateAsync(u)).ToList();

        await Task.WhenAll(tasks);
    }
    public async Task GetEqpStateAsync(string url)
    {
        try
        {
            _logger.LogInformation("[Info] 設備状態取得開始");
            var response = await _httpClient.GetAsync($"{url}/api/shelf-system/status");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Info] 設備状態取得成功");
                var eqpState = await response.Content.ReadFromJsonAsync<EquipmentState>();
                lock (_lockObject)
                {
                    var targetEqp = _eqpStateList.First(e => e.EqpName == eqpState.EqpName);
                    targetEqp.ControlState = 1;
                    _logger.LogInformation("[Info] ControlState : ON-LINE");
                    targetEqp.AlarmStatus = eqpState.AlarmStatus;
                    if(targetEqp.AlarmStatus == 1) _logger.LogInformation("[Info] AlarmStatus : ALARM");
                    else _logger.LogInformation("[Info] AlarmStatus : NO ALARM");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("【[Error] 設備状態取得失敗");
        }

    }
    public async Task InsertValidationAsync(InsertCommand newCommand, DateTime receptionAt)
    {
        if (!Regex.IsMatch(newCommand.CarrierId, @"^CAR[0-9]{6}$"))
        {
            throw new HttpRequestException("キャリアID の命名規則が不正です。", null, System.Net.HttpStatusCode.BadRequest);
        }
        
        var targetEqp = _eqpStateList.FirstOrDefault(e => e.EqpName == newCommand.EqpName);
        if (targetEqp == null)
        {

            throw new HttpRequestException("設備IDが存在しません。", null, System.Net.HttpStatusCode.NotFound);
        }
        if (targetEqp.ControlState == 0)
        {
            throw new HttpRequestException("指定された設備がOFFLINEです。", null, HttpStatusCode.BadRequest);
        }

        string availableLocation = "";
        string prefix = "";
        if (newCommand.EqpName == "EQP01") prefix = "1%"; 
        else if (newCommand.EqpName == "EQP02") prefix = "2%"; 
        else if (newCommand.EqpName == "EQP03") prefix = "3%";
        var (shelfList, incompleteCommandList) = await _repository.SelectShelfInformationAsync(prefix);

        if (newCommand.CommandType == 1)
        {
            bool isCarrierAlreadyStored = shelfList.Any(s => s.StoredCarrierId == newCommand.CarrierId)
                                          || incompleteCommandList.Any(c => c.CarrierId == newCommand.CarrierId);
            if (isCarrierAlreadyStored)
            {
                throw new HttpRequestException("指定されたキャリアIDは既に入庫されています。", null, HttpStatusCode.BadRequest);
            }
            var selectedInShelf = shelfList.Where(s => s.StoredCarrierId == null)
                              .FirstOrDefault(s => !incompleteCommandList.Any(c => c.Location == s.ShelfLocation));
            if (selectedInShelf == null)
            {
                throw new HttpRequestException("棚が満帆のため入庫できません。", null, HttpStatusCode.BadRequest);
            }
            availableLocation = selectedInShelf.ShelfLocation;
        }
        else if(newCommand.CommandType == 0)
        {
            bool isAllEmpty = shelfList.Any(s => s.StoredCarrierId != null);
            if (!isAllEmpty)
            {
                throw new HttpRequestException("棚が空のため出庫できません。", null, HttpStatusCode.BadRequest);
            }
            var matchedShelf = shelfList.FirstOrDefault(s => s.StoredCarrierId == newCommand.CarrierId);
            if (matchedShelf == null)
            {
                throw new HttpRequestException("指定されたキャリアIDは棚に存在しません。", null, HttpStatusCode.NotFound);
            }
            else if (matchedShelf.Reservation != null)
            {
                throw new HttpRequestException("指定されたキャリアがある棚は、すでに予約があります。", null, HttpStatusCode.BadRequest);
            }

            availableLocation = matchedShelf.ShelfLocation;
        }

        Command insertData = new Command()
        {
            CarrierId = newCommand.CarrierId,
            CommandType = (int)newCommand.CommandType,
            EqpName = newCommand.EqpName,
            Location = availableLocation,
            ReceptionAt = receptionAt,
            SendAt = null,
            CompletionAt = null,
            CommandStatus = 0
        };
        await _repository.InsertCommandsAsync(insertData);
    }

    public void UpdateEqpState(string eqpName, string endPoint)
    {
        //同時に複数の設備機器の状態が遷移する可能性がある
        lock (_lockObject)
        {
            var targetEqp = _eqpStateList.First(e => e.EqpName == eqpName);
            if (endPoint == "online" || endPoint == "request")
            {
                targetEqp.ControlState = 1;
                _logger.LogInformation("[Info] ControlState : ON-LINE"); 
            }
            else if (endPoint == "start")
            {
                targetEqp.EquipmentStatus = 1;
                _logger.LogInformation("[Info] EquipmentStatus : ACTIVE");
            }
            else if (endPoint == "completion")
            {
                targetEqp.ControlState = 1;
                _logger.LogInformation("[Info] ControlState : ON-LINE");
                targetEqp.EquipmentStatus = 0;
                _logger.LogInformation("[Info] EquipmentStatus : IDLE"); 
            }
            else if (endPoint == "incident")
            {
                targetEqp.AlarmStatus = 1;
                _logger.LogInformation("[Info] AlarmStatus : ALARM");
            }
            else if (endPoint == "recovery")
            {
                targetEqp.AlarmStatus = 0;
                _logger.LogInformation("[Info] AlarmStatus : NO ALARM"); 
            }
            else
            {
                targetEqp.ControlState = 0;
                _logger.LogInformation("[Info] ControlState : OFF-LINE");
            }
        }
    }

    public async Task<bool> UnloadValidationAsync(RelayCommand unload)
    {
        var targetEqp = _eqpStateList.FirstOrDefault(e => e.EqpName == unload.EqpName);
        if (targetEqp == null) return false;
        await _repository.UpdateCommandStatusAsync(unload.CommandId, 4);
        //await PostHttpClientAsync(unload);
        return true;
        
    }

    public async Task PostHttpClientAsync(RelayCommand sendCommand)
    {
        string url = "";

        if (sendCommand.EqpName == "EQP01") url = _eqpBaseUrls[0];
        else if (sendCommand.EqpName == "EQP02") url = _eqpBaseUrls[1];
        else if (sendCommand.EqpName == "EQP03") url = _eqpBaseUrls[2];
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{url}/api/shelf-system/unload", sendCommand);
            if (response.IsSuccessStatusCode)
            {
                // コンソールやファイルにログを出力する
                _logger.LogInformation("[Info] 設備へ払出完了報告成功");
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "[Error] 設備へ払出完了報告失敗");
            throw;
        }
    }

    public void TimeoutOccurred(EquipmentCommand sendCommand)
    {
        //ディクショナリに追加
        var cts = new CancellationTokenSource();
        bool added = _timeoutCancell.TryAdd(sendCommand.CommandId, cts);
        Console.WriteLine($"[Timer] タイマーを追加しました。CommandId:{sendCommand.CommandId}, 結果:{added}");
        Task task = Task.Run(async() =>
        {
            try
            {
                await Task.Delay(30000, cts.Token);
                await _repository.UpdateTimeOutAsync(sendCommand);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("タイマーを停止しました。");
                return;
            }
            catch (Exception ex)
            {
                return;
            }
            finally
            {
                //ディクショナリから完全に削除
                if (_timeoutCancell.TryRemove(sendCommand.CommandId, out var expiredCts))
                {
                    expiredCts.Dispose(); 
                }
            }
            _logger.LogInformation("タイムアウト検知しました。");
            UpdateEqpState(sendCommand.EqpName, "");
        });
    }

    public void CancelTimeoutTimer(string commandId)
    {
        //搬送完了POSTによりDBの更新がされたら、タイマーストップし、削除
        if (_timeoutCancell.TryRemove(commandId, out var cts))
        {
            cts.Cancel();  
            cts.Dispose(); 
        }
    }

    public void RefreshOnlineTimer(string eqpName)
    {
        if (_onlineWatchCancell.TryRemove(eqpName, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }
        var cts = new CancellationTokenSource();
        bool added = _onlineWatchCancell.TryAdd(eqpName, cts);

        Task.Run(async () =>
        {
            try
            {
                //10秒タイマー開始
                await Task.Delay(10000, cts.Token);
                
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                return;
            }
            finally
            {
                // ディクショナリから完全に削除してリソース解放
                if (_onlineWatchCancell.TryRemove(eqpName, out var expiredCts))
                {
                    expiredCts.Dispose();
                }
            }
            _logger.LogInformation($"{eqpName}がOFF-LINEです。");
            UpdateEqpState(eqpName, "");
        });
    }
}
