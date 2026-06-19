using FinalAssigenment.Models;
using FinalAssigenment.Repositories;
using System.ComponentModel.Design;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

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

    public List<EquipmentState> eqpStatusList = [
            new(){EqpName = "EQP01", ControlState = 1, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP02", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP03", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0}
        ];

    private readonly List<string> eqpBaseUrls = [
            "http://localhost:8090",
            "http://localhost:8091",
            "http://localhost:8092"
        ];

    public async Task GetAllEqpStateAsync()
    {
        List<Task> tasks = eqpBaseUrls.Select(u => GetEqpStateAsync(u)).ToList();

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
                int statusType = 1;
                UpdateEqpStatus(eqpState.EqpName, statusType);
                if (eqpState?.AlarmStatus == 1)
                {
                    statusType = 2; 
                    UpdateEqpStatus(eqpState.EqpName, statusType);
                }
            }
        }
        catch (Exception ex)
        {
            throw;
        }
        
    }
    public async Task InsertValidationAsync(InsertCommand newCommand)
    {
        if (!Regex.IsMatch(newCommand.CarrierId, @"^CAR[0-9]{6}$"))
        {
            throw new HttpRequestException("キャリアID の命名規則が不正です。", null, System.Net.HttpStatusCode.BadRequest);
        }
        if (newCommand.EqpName != "EQP01" && newCommand.EqpName != "EQP02" && newCommand.EqpName != "EQP03")
        {
            throw new HttpRequestException("設備IDが存在しません。", null, System.Net.HttpStatusCode.NotFound);
        }

        string availableLocation = "";
        string prefix = "";
        if (newCommand.EqpName == "EQP01") { prefix = "1%"; } 
        else if (newCommand.EqpName == "EQP02") { prefix = "2%"; }
        else if (newCommand.EqpName == "EQP03") { prefix = "3%"; }
        var shelfList = await _repository.GetShelfAsync(prefix);

        bool isCarrierAlreadyStored = shelfList.Any(s => s.StoredCarrierId == newCommand.CarrierId);
        if (isCarrierAlreadyStored)
        {
            throw new HttpRequestException("指定されたキャリアIDは既に入庫されています。", null, HttpStatusCode.BadRequest);
        }
        var targetEqp = eqpStatusList.First(e => e.EqpName == newCommand.EqpName);
        if (targetEqp.ControlState == 0)
        {
            throw new HttpRequestException("指定された設備がOFFLINEです。", null, HttpStatusCode.BadRequest);
        }
        Shelf? selectedInShelf = shelfList.FirstOrDefault(s => s.StoredCarrierId == null);
        bool isAllEmpty = shelfList.Any(s => s.StoredCarrierId != null);
        Shelf? matchedShelf = shelfList.FirstOrDefault(s => s.StoredCarrierId == newCommand.CarrierId);
        if (newCommand.CommandType == 1)
        {
            if (selectedInShelf == null)
            {
                throw new HttpRequestException("棚が満帆のため入庫できません。", null, HttpStatusCode.BadRequest);
            }
            else if (selectedInShelf != null)
            {
                availableLocation = selectedInShelf.ShelfLocation;
            }
        }
        else
        {
            if (!isAllEmpty)
            {
                throw new HttpRequestException("棚が空のため出庫できません。", null, HttpStatusCode.BadRequest);
            }
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
        // ⭕ 正しい書き方（$マークを付けて、波カッコで囲む）
        Console.WriteLine($"CarrierId: {newCommand.CarrierId}, Type: {newCommand.CommandType}, Eqp: {newCommand.EqpName}, Location: {availableLocation}");

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

    public void UpdateEqpStatus(string eqpName, int statusType)
    {
        var targetEqp = eqpStatusList.First(e => e.EqpName == eqpName);
        if (statusType == 1)
        {
            targetEqp.ControlState = (targetEqp.ControlState == 1) ? 0 : 1;
        }
        else if(statusType == 2)
        {
            targetEqp.EquipmentStatus = (targetEqp.EquipmentStatus == 1) ? 0 : 1;
        }
        else if(statusType == 3)
        {
            targetEqp.AlarmStatus = (targetEqp.AlarmStatus == 1) ? 0 : 1;
        }
    }

    public async Task<bool> UnloadValidationAsync(RelayCommand unload)
    {
        if(unload.EqpName != "Eqp01" && unload.EqpName != "Eqp02" && unload.EqpName != "Eqp03")
        {
            return false;
        }
        await PostHttpClientAsync(unload);
        return true;
    }

    public async Task PostHttpClientAsync(RelayCommand sendCommand)
    {
        string url = "";
        if (sendCommand.EqpName == "EQP01") url = eqpBaseUrls[0];
        else if (sendCommand.EqpName == "EQP02") url = eqpBaseUrls[1];
        else if (sendCommand.EqpName == "EQP03") url = eqpBaseUrls[2];

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{url}/api/shelf-system/unload", sendCommand);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
