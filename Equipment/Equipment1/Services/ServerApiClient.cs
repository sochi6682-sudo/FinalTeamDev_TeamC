using Equipment1.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Equipment1.Services;
public class ServerApiClient
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly HttpClient _httpClient;

    public ServerApiClient()
    {
        _httpClient = new HttpClient();

        _httpClient.BaseAddress = new Uri("http://localhost:5000/");
    }

   
    //設備ONLINE報告 POST 
    //-------------------------------------------------------------------------------
    public async Task ReportOnlineAsync(StateReport stateReport)
    {
        await _httpClient.PostAsJsonAsync("/api/shelf-system/online", stateReport);
    }



   
    //搬送指示要求 GET(ポーリング)
    //-------------------------------------------------------------------------------
    public async Task<HttpResponseMessage?> GetCommandAsync()
    {
        try
        {
            return await _httpClient.GetAsync( "/api/shelf-system/request");
        }
        catch
        {
            return null;
        }
    }



    //搬送指示開始報告 POST
    //-------------------------------------------------------------------------------
    public async Task ReportStartAsync(Command command)
    {
        command.CommandStatus = 1;
        await _httpClient.PostAsJsonAsync("/api/shelf-system/start",command);
    }



    //搬送指示完了報告(正常) POST
    //-------------------------------------------------------------------------------
    public async Task ReportCommandCompleteAsync(Command command)
    {
        command.CommandStatus = 2;
        await _httpClient.PostAsJsonAsync("/api/shelf-system/completion", command);
    }


    //搬送指示完了報告(異常) POST
    //-------------------------------------------------------------------------------
    public async Task ReportCommandFailedAsync(Command command)
    {
        command.CommandStatus = 3;
        await _httpClient.PostAsJsonAsync("/api/shelf-system/completion", command);
    }


    //異常発生報告 POST
    //-------------------------------------------------------------------------------
    public async Task ReportAlarmAsync(string eqpName)
    {
        await _httpClient.PostAsJsonAsync(
            "/api/shelf-system/incident",
            new
            {
                EqpName = eqpName
            });
    }



    //異常復旧報告 POST
    //-------------------------------------------------------------------------------
    public async Task ReportRecoveryAsync(string eqpName)
    {
        await _httpClient.PostAsJsonAsync(
            "/api/shelf-system/recovery",
            new
            {
                EqpName = eqpName
            });
    }
}

