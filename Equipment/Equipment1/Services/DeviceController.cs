using Equipment1.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Equipment1.Services;

public class DeviceController
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly State _state;
    private readonly ServerApiClient _apiClient;
    
    private DateTime? _onlineStartTime;
    private Command? _currentCommand;

    public DeviceController()
    {
        _state = new State();
        _apiClient = new ServerApiClient();
        _currentCommand = null;
    }

    //プロパティ
    public State CurrentState
    {
        get { return _state; }
    }



    //起動時1回だけ処理　
    //-------------------------------------------------------------------------------
    public async Task InitAsync() 
    {
        //状態初期化
        _state.CommunicationStatus = CommunicationStatus.Offline;　
        _state.CommandReceptionStatus = CommandReceptionStatus.Idle;
        _state.LocalAlarmStatus = LocalAlarmStatus.NoAlarm;
        _state.OperatingStatus = OperatingStatus.Stop;
        _state.RetrieveAvailability = RetrieveAvailability.Available;
        Logger.Info("サーバー通信状態：OFF-LINE");
        Logger.Info("指示実行状態：IDLE");
        Logger.Info("異常状態：NO ALARM");
        Logger.Info("動作状態：STOP");
        Logger.Info("出庫可能状態");

        StateReport report = CreateStateReport();

        await _apiClient.ReportOnlineAsync(report);

    }

 
    //報告用状態作成　
    //-------------------------------------------------------------------------------
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

    //状態受渡
    //-------------------------------------------------------------------------------
    public StateReport GetStateReport()
    {
        return CreateStateReport();
    }


    //IDLE時5秒毎処理　
    //-------------------------------------------------------------------------------
    public async Task RunIdleLoopAsync()
    {
        
        HttpResponseMessage? response = await _apiClient.GetCommandAsync();

        if (response == null)
        {
            // 通信失敗
            Logger.Error("搬送指示要求GET 失敗");

            _state.CommunicationStatus = CommunicationStatus.Offline;
            _state.LocalAlarmStatus = LocalAlarmStatus.Alarm;
            UpdateOperatingStatus();
            Logger.Info("サーバー通信状態：OFF-LINE");
            Logger.Info("異常状態：ALARM");

            await _apiClient.ReportAlarmAsync(_state.EqpName);

        }
        else 
        {

            if (!response.IsSuccessStatusCode)
            {
                // 200以外
                Logger.Warn($"搬送指示要求GET 失敗 StatusCode={(int)response.StatusCode}");

                _state.CommunicationStatus = CommunicationStatus.Offline;
                _state.LocalAlarmStatus = LocalAlarmStatus.Alarm;
                UpdateOperatingStatus();
                Logger.Info("サーバー通信状態：OFF-LINE");
                Logger.Info("異常状態：ALARM");

                await _apiClient.ReportAlarmAsync(_state.EqpName);
            }
            else
            {
                //レスポンスをcommandに入れる
                Command? command = await response.Content.ReadFromJsonAsync<Command>();

                if (command == null || string.IsNullOrEmpty(command.CommandId))
                {
                    // 200だけど指示なし
                    Logger.Info("搬送指示要求GET 搬送指示なし");
                    _state.CommunicationStatus = CommunicationStatus.Online;
                    UpdateOperatingStatus();
                    Logger.Info("サーバー通信状態：ON-LINE");
                }
                else
                {
                    // 200かつ指示あり
                    Logger.Info("搬送指示要求GET 搬送指示受信");
                    _state.CommunicationStatus = CommunicationStatus.Online;
                    _state.CommandReceptionStatus = CommandReceptionStatus.Active;
                    UpdateOperatingStatus();
                    Logger.Info("サーバー通信状態：ON-LINE");
                    Logger.Info("指示実行状態：ACTIVE");

                    _currentCommand = command;
                    await _apiClient.ReportStartAsync(command);
                    
                }
            }
        }

        await Task.Delay(5000);

    }


    //BUSY時時処理　
    //-------------------------------------------------------------------------------
    public async Task RunBusyProcessAsync()
    {
        if (_currentCommand == null)
        {
            //指示なし
            return;
        }
        
        if (_currentCommand.CommandType == 0)
        {
            //入庫であればすぐに正常/異常完了選択処理へ
            await SelectCompleteResult();
        }
        else
        {
            //出庫であれば、出庫可能状態になったら正常/異常完了選択処理へ
            while (_state.RetrieveAvailability != RetrieveAvailability.Available)
            {
                await Task.Delay(100);
            }
                await SelectCompleteResult();

        }
    }



    //正常/異常完了選択処理　
    //-------------------------------------------------------------------------------
    public async Task SelectCompleteResult()
    {
        while (true)
        {
            //正常/異常か選択させる
            Console.WriteLine("結果を選択してください");
            Console.WriteLine("1 : 正常完了");
            Console.WriteLine("2 : 異常完了");
            Console.WriteLine("==================================================");

            string? input = Console.ReadLine();

            if (input == "1")
            {
                Logger.Info("正常完了選択");
                Console.WriteLine("正常完了");
                Console.WriteLine("==================================================");

                //正常完了であれば、IDLEになって報告し、実行した指示を消す
                //出庫であれば出庫不可に移行する
                if (_currentCommand?.CommandType == 1)
                {
                    _state.RetrieveAvailability = RetrieveAvailability.Unavailable;
                    Logger.Info("出庫不可状態");
                }

                _state.CommandReceptionStatus = CommandReceptionStatus.Idle;
                UpdateOperatingStatus();
                Logger.Info("指示実行状態：IDLE");

                await _apiClient.ReportCommandCompleteAsync(_currentCommand!);

                _currentCommand = null;

                break;
            }

            if (input == "2")
            {
                Logger.Info("異常完了選択");
                Console.WriteLine("異常完了");
                Console.WriteLine("==================================================");

                //異常完了であれば、IDLE・ALARMになって報告し、実行した指示を消す
                _state.CommandReceptionStatus = CommandReceptionStatus.Idle;
                _state.LocalAlarmStatus = LocalAlarmStatus.Alarm;
                UpdateOperatingStatus();
                Logger.Info("指示実行状態：IDLE");
                Logger.Info("異常状態：ALARM");

                await _apiClient.ReportCommandFailedAsync(_currentCommand!);
                await _apiClient.ReportAlarmAsync(_state.EqpName);

                _currentCommand = null;

                break;
            }

            Console.WriteLine("入力エラーです。1 または 2 を入力してください。");
            Console.WriteLine("==================================================");

        }
    }
    

    //ALARM時処理　
    //-------------------------------------------------------------------------------

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
                Logger.Info("異常状態：NO ALARM");
                UpdateOperatingStatus();

                _onlineStartTime = null;
                await _apiClient.ReportRecoveryAsync(_state.EqpName);
                
            }
        }
        else
        {
            _onlineStartTime = null;
        }
    }


    //動作状態更新
    //-------------------------------------------------------------------------------
    private void UpdateOperatingStatus()
    {
        if (_state.CommandReceptionStatus == CommandReceptionStatus.Active &&
            _state.LocalAlarmStatus == LocalAlarmStatus.NoAlarm)
        {
            Logger.Info("動作状態：BUSY");
            _state.OperatingStatus = OperatingStatus.Busy;
        }
        else
        {
            Logger.Info("動作状態：STOP");
            _state.OperatingStatus = OperatingStatus.Stop;
        }
    }


    //払出完了受信時に出庫可能常態に移行
    //-------------------------------------------------------------------------------
    public void SetRetrieveAvailable()
    {
        _state.RetrieveAvailability = RetrieveAvailability.Available;
        Logger.Info("出庫可能状態になりました");
    }


}

