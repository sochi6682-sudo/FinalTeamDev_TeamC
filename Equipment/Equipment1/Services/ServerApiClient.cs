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



    //搬送指示要求 GET(ポーリング)
    //-------------------------------------------------------------------------------
    public async Task<HttpResponseMessage?> GetCommandAsync()
    {
        Logger.Info("搬送指示要求GET 開始");

        try
        {
            return await _httpClient.GetAsync("/api/shelf-system/request");
        }
        catch (Exception ex)
        {
            return null;
        }
    }


    //POST共通例外処理 
    //-------------------------------------------------------------------------------
    private async Task PostAsync<T>(string url, T data)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, data);

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn($"POST失敗 URL={url} Status={(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"POST例外 URL={url}");
        }
    }



    //設備ONLINE報告 POST 
    //-------------------------------------------------------------------------------
    public async Task ReportOnlineAsync(StateReport stateReport)
    {
        Logger.Info("設備ONLINE報告POST 開始");
        await PostAsync("/api/shelf-system/online", stateReport);
    }



    //搬送指示開始報告 POST
    //-------------------------------------------------------------------------------
    public async Task ReportStartAsync(Command command)
    {
        Logger.Info("搬送指示開始報告POST 開始");
        command.CommandStatus = 1;
        await PostAsync("/api/shelf-system/start",command);
    }



    //搬送指示完了報告(正常) POST
    //-------------------------------------------------------------------------------
    public async Task ReportCommandCompleteAsync(Command command)
    {
        Logger.Info("搬送指示正常完了報告POST 開始");
        command.CommandStatus = 2;
        await PostAsync("/api/shelf-system/completion", command);
    }


    //搬送指示完了報告(異常) POST
    //-------------------------------------------------------------------------------
    public async Task ReportCommandFailedAsync(Command command)
    {
        Logger.Info("搬送指示異常完了報告POST 開始");
        command.CommandStatus = 3;
        await PostAsync("/api/shelf-system/completion", command);
    }


    //異常発生報告 POST
    //-------------------------------------------------------------------------------
    public async Task ReportAlarmAsync(string eqpName)
    {
        Logger.Info("異常発生報告POST 開始");
        await PostAsync("/api/shelf-system/incident",
            new
            {
                EqpName = eqpName
            });
    }



    //異常復旧報告 POST
    //-------------------------------------------------------------------------------
    public async Task ReportRecoveryAsync(string eqpName)
    {
        Logger.Info("異常解除報告POST 開始");
        await PostAsync("/api/shelf-system/recovery",
            new
            {
                EqpName = eqpName
            });
    }




}

