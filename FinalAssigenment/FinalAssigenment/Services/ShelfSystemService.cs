using FinalAssigenment.Models;
using FinalAssigenment.Repositories;
using System.ComponentModel.Design;
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

    private List<EquipmentState> eqpStatusList = [
            new(){EqpName = "EQP01", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP02", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0 },
            new(){EqpName = "EQP03", ControlState = 0, EquipmentStatus = 0, AlarmStatus = 0}
        ];

    private readonly List<string> eqpBaseUrls = [
            "http://localhost:8090", // 1台目
            "http://localhost:8091", // 2台目
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
    public async Task<bool> InsertValidationAsync(InsertCommand newCommand)
    {

        return true;
    }

    public void UpdateEqpStatus(string eqpName, int statusType)
    {
        var target = eqpStatusList.First(e => e.EqpName == eqpName);
        if (statusType == 1)
        {
            target.ControlState = (target.ControlState == 1) ? 0 : 1;
        }
        else if(statusType == 2)
        {
            target.EquipmentStatus = (target.EquipmentStatus == 1) ? 0 : 1;
        }
        else if(statusType == 3)
        {
            target.AlarmStatus = (target.AlarmStatus == 1) ? 0 : 1;
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
        if (sendCommand.EqpName == "Eqp01") url = eqpBaseUrls[0];
        else if (sendCommand.EqpName == "Eqp02") url = eqpBaseUrls[1];
        else if (sendCommand.EqpName == "Eqp03") url = eqpBaseUrls[2];

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
