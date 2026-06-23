using FinalAssigenment.Models;
using FinalAssigenment.Repositories;
using System.ComponentModel.Design;
using System.Net;
using System.Net.NetworkInformation;
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

    private List<EquipmentState> _eqpStatusList = [
            new(){EqpName = "EQP01", ControlState = 1, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP02", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP03", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0}
        ];

    private readonly List<string> _eqpBaseUrls = [
            "http://localhost:8090",
            "http://localhost:8091",
            "http://localhost:8092"
        ];
    public List<EquipmentState> EqpStatusList => _eqpStatusList;

    public async Task GetAllEqpStateAsync()
    {
        List<Task> tasks = _eqpBaseUrls.Select(u => GetEqpStateAsync(u)).ToList();

        await Task.WhenAll(tasks);
    }
    public async Task GetEqpStateAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{url}/api/shelf-system/status");

            if (response.IsSuccessStatusCode)
            {
                var eqpState = await response.Content.ReadFromJsonAsync<EquipmentState>();
                var targetEqp = _eqpStatusList.First(e => e.EqpName == eqpState.EqpName);
                targetEqp.ControlState = eqpState.ControlState;
                targetEqp.AlarmStatus = eqpState.AlarmStatus;

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"【通信失敗】{url} への接続に失敗しました。理由: {ex.Message}");
        }

    }
    public async Task InsertValidationAsync(InsertCommand newCommand)
    {
        if (!Regex.IsMatch(newCommand.CarrierId, @"^CAR[0-9]{6}$"))
        {
            throw new HttpRequestException("キャリアID の命名規則が不正です。", null, System.Net.HttpStatusCode.BadRequest);
        }
        var targetEqp = _eqpStatusList.FirstOrDefault(e => e.EqpName == newCommand.EqpName);
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
        else
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
            ReceptionAt = DateTime.Now,
            SendAt = null,
            CompletionAt = null,
            CommandStatus = 0
        };
        await _repository.InsertCommandsAsync(insertData);
    }

    public void UpdateEqpStatus(string eqpName, string endPoint)
    {
        var targetEqp = _eqpStatusList.First(e => e.EqpName == eqpName);
        if (endPoint == "online") targetEqp.ControlState = 1;
        else if (endPoint == "start") targetEqp.EquipmentStatus = 1;
        else if (endPoint == "completion") targetEqp.EquipmentStatus = 0;
        else if (endPoint == "incident") targetEqp.AlarmStatus = 1;
        else if (endPoint == "recovery") targetEqp.AlarmStatus = 0;
    }

    public async Task<bool> UnloadValidationAsync(RelayCommand unload, string endPoint)
    {
        if (unload.EqpName != "Eqp01" && unload.EqpName != "Eqp02" && unload.EqpName != "Eqp03")
        {
            return false;
        }
        await PostHttpClientAsync(unload, endPoint);
        return true;
    }

    public async Task PostHttpClientAsync(RelayCommand sendCommand, string endPoint)
    {
        string url = "";
        if (sendCommand.EqpName == "EQP01") url = _eqpBaseUrls[0];
        else if (sendCommand.EqpName == "EQP02") url = _eqpBaseUrls[1];
        else if (sendCommand.EqpName == "EQP03") url = _eqpBaseUrls[2];

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{url}/api/shelf-system/{endPoint}", sendCommand);
        }
        catch (Exception)
        {
            throw;
        }
    }
}
