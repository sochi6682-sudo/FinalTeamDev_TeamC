using Equipment1.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Equipment1.Services;

public class DeviceController
{
    private readonly State _state;
    private readonly ServerApiClient _apiClient;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private DateTime? _onlineStartTime;

    public DeviceController()
    {
        _state = new State();
        _apiClient = new ServerApiClient();
    }

    //起動時1回だけ処理
    public async Task InitAsync() 
    {
        _state.CommunicationStatus = CommunicationStatus.Online;
        _state.CommandReceptionStatus = CommandReceptionStatus.Idle;
        _state.LocalAlarmStatus = LocalAlarmStatus.NoAlarm;
        _state.OperatingStatus = OperatingStatus.Stop;
        _state.RetrieveAvailability = RetrieveAvailability.Available;

        StateReport report = CreateStateReport();

        await _apiClient.ReportOnlineAsync(report);

    }

    //報告用状態作成
    private StateReport CreateStateReport()
    {
        StateReport report = new StateReport();

        report.EqpName = _state.EqpName;

        if (_state.CommunicationStatus == CommunicationStatus.Online)
        {
            report.ControlState = ControlState.Online;
        }
        else
        {
            report.ControlState = ControlState.Offline;
        }

        if (_state.CommandReceptionStatus == CommandReceptionStatus.Active)
        {
            report.EquipmentStatus = EquipmentStatus.Active;
        }
        else
        {
            report.EquipmentStatus = EquipmentStatus.Idle;
        }

        if (_state.LocalAlarmStatus == LocalAlarmStatus.Alarm)
        {
            report.AlarmStatus = AlarmStatus.Alarm;
        }
        else
        {
            report.AlarmStatus = AlarmStatus.NoAlarm;
        }

        return report;
    }

    //IDLE時5秒毎処理
    public async Task RunIdleLoopAsync()
    {
        Logger.Info("搬送指示要求GET開始");
        HttpResponseMessage? response = await _apiClient.GetCommandAsync();

        if (response == null)
        {
            // 通信失敗
            Logger.Error("設備状態取得失敗");
            _state.CommunicationStatus = CommunicationStatus.Offline;
            _state.LocalAlarmStatus = LocalAlarmStatus.Alarm;
            await _apiClient.ReportAlarmAsync(_state.EqpName);

        }
        else {

            if (!response.IsSuccessStatusCode)
            {
                // 200以外
                Logger.Warn("JSONでPOSTする項目が空白");
                _state.CommunicationStatus = CommunicationStatus.Offline;
                _state.LocalAlarmStatus = LocalAlarmStatus.Alarm;
                await _apiClient.ReportAlarmAsync(_state.EqpName);
            }
            else
            {
                //レスポンスをcommandに入れる
                Command? command = await response.Content.ReadFromJsonAsync<Command>();

                if (command == null)
                {
                    // 200だけど指示なし
                    Logger.Info("搬送指示なし");
                    _state.CommunicationStatus = CommunicationStatus.Online;
                }
                else
                {
                    // 200かつ指示あり
                    Logger.Info("搬送指示受信");
                    _state.CommunicationStatus = CommunicationStatus.Online;
                    _state.CommandReceptionStatus = CommandReceptionStatus.Active;
                    await _apiClient.ReportStartAsync(command);

                }
            }

        }

        await Task.Delay(5000);
    }

    //BUSY時時処理
    public async Task RunBusyProcessAsync()
    {
        //入庫
        //出庫
            //出庫可能状態になったら(なるまで待つ)
    }

    //ALARM時処理
    public async Task RunAlarmProcessAsync()
    {
        //ONLINE状態5秒経過
        if (_state.CommunicationStatus == CommunicationStatus.Online)
        {
            if (_onlineStartTime == null)
            {
                _onlineStartTime = DateTime.Now;
            }

            if (DateTime.Now - _onlineStartTime >= TimeSpan.FromSeconds(5))
            {
                _state.LocalAlarmStatus = LocalAlarmStatus.NoAlarm;
                _onlineStartTime = null;
                await _apiClient.ReportRecoveryAsync(_state.EqpName);
            }
        }
        else
        {
            _onlineStartTime = null;
        }
    }
}
