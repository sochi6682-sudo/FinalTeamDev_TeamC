using Equipment2.Models;
using Equipment2.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Equipment2.Controllers;

public class DeviceController
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly State _state;
    private readonly ServerApiClient _apiClient;
    private readonly StateController _stateController;
    private readonly ConsoleView _consoleView;

    private DateTime? _onlineStartTime;
    private Command? _currentCommand;

    public DeviceController()
    {
        _state = new State();
        _apiClient = new ServerApiClient();
        _stateController = new StateController(_state);
        _consoleView = new ConsoleView();

        _currentCommand = null;
    }

    //プロパティ
    //-------------------------------------------------------------------------------
    //状態取出
    public State CurrentState
    {
        get { return _state; }
    }

	//報告用状態受渡
	//-------------------------------------------------------------------------------
	public StateReport GetStateReport()
    {
        return CreateStateReport();
    }

    //報告用状態作成　
    //-------------------------------------------------------------------------------
    private StateReport CreateStateReport()
    {
        StateReport report = new StateReport();
        report.EqpName = _state.EqpName;

        //通信状態変換
        if (_state.CommunicationStatus == CommunicationStatus.Online)
        {
            report.ControlStates = ControlStates.Online;
        }
        else
        {
            report.ControlStates = ControlStates.Offline;
        }

        //設備状態変換
        if (_state.CommandReceptionStatus == CommandReceptionStatus.Active)
        {
            report.EquipmentStatus = EquipmentStatus.Active;
        }
        else
        {
            report.EquipmentStatus = EquipmentStatus.Idle;
        }

        //異常状態変換
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


    //起動時1回だけ処理　
    //-------------------------------------------------------------------------------
    public async Task InitAsync() 
    {
        _consoleView.ShowInfo(" <<<<< 保管設備起動 >>>>>");

        //状態初期化
        _stateController.InitStatus();

        //ON-LINE報告POST送信
        StateReport report = CreateStateReport();　
        await _apiClient.ReportOnlineAsync(report);

    }


    //IDLE時5秒毎処理　
    //-------------------------------------------------------------------------------
    public async Task RunIdleLoopAsync()
    {
        //搬送指示要求GET送信
        HttpResponseMessage? response = await _apiClient.GetCommandAsync(_state.EqpName);

        //通信成功/失敗
        if (response == null)　
        {
            // 通信異常：OFF-LINE・IDLE・ALARM状態へ
            _stateController.SetCommunicationOffline();
            _stateController.SetAlarm();

            _consoleView.ShowAlarm("※　通信異常発生　※");
            Logger.Error("搬送指示要求GET 失敗");

        }
        else 
        {
            // 通信成功：200/200以外
            if (!response.IsSuccessStatusCode)　
            {
                // 200以外：OFF-LINE・IDLE・ALARM状態へ
                _stateController.SetCommunicationOffline();
                _stateController.SetAlarm();

                await _apiClient.ReportAlarmAsync(_state.EqpName);

                _consoleView.ShowAlarm("※　通信異常発生　※");
                Logger.Warn($"搬送指示要求GET 失敗 StatusCode={(int)response.StatusCode}");

            }
            else
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    // 204 指示なし
                    _stateController.SetCommunicationOnline();

                    Logger.Info("搬送指示要求GET 搬送指示なし");

                    await Task.Delay(5000);
                    return;
                }

                //通信成功
                //レスポンスをcommandに入れる
                Command? command = await response.Content.ReadFromJsonAsync<Command>();

                //指示あり/なし
                if (command == null || string.IsNullOrEmpty(command.CommandId))
                {
                    _stateController.SetCommunicationOnline();
                    Logger.Info("搬送指示要求GET 搬送指示なし");

                    await Task.Delay(5000);
                    return;
                }
                else　
                {
                    // 200かつ指示あり　ON-LINE・ACTIVE状態へ
                    _stateController.SetCommunicationOnline();
                    _stateController.SetActive();

                    _currentCommand = command; //実行中搬送指示更新

                    await _apiClient.ReportStartAsync(command);

                    _consoleView.ShowInfo("搬送指示受信");
                    _consoleView.ShowCommand(_currentCommand);
                    Logger.Info("搬送指示要求GET 搬送指示受信");
                    
                    return;
                }
            }
        }

        await Task.Delay(5000);

    }


    //BUSY時時処理　
    //-------------------------------------------------------------------------------
    public async Task RunBusyProcessAsync()
    {
        //実行中指示あり/なし
        if (_currentCommand == null)
        {
            //指示なし
            return;
        }

        //入庫/出庫
        if (_currentCommand.CommandType == 1)
        {
            //入庫：正常/異常完了選択処理へ
            _consoleView.ShowInfo("動作開始");
            await SelectCompleteResult();
        }
        else
        {
            _consoleView.ShowInfo("出庫口が空くまで待機中");
            //出庫：出庫可能状態まで待ち、正常/異常完了選択処理へ
            while (_state.RetrieveAvailability != RetrieveAvailability.Available)
            {
                await Task.Delay(100);
            }
            _consoleView.ShowInfo("動作開始");
            await SelectCompleteResult();
        }

    }



    //正常/異常完了選択処理　
    //-------------------------------------------------------------------------------
    public async Task SelectCompleteResult()
    {
        _consoleView.ShowSelectCompleteInput();

        while (true)
        {
            //１）正常/異常　選択
            string? input = Console.ReadLine();

            //２）正常完了：IDLEになって報告し、実行した指示を消す
            if (input == "1")
            {
                _consoleView.ShowSuccess("正常完了");
                Logger.Info("正常完了選択");

                //出庫であれば出庫不可に移行する
                if (_currentCommand?.CommandType == 0)
                {
                    _stateController.SetRetrieveUnavailable();

                    _consoleView.ShowInfo("出庫口にキャリアが置かれて、出庫不可になりました");
                }

                _stateController.SetIdle();

                await _apiClient.ReportCommandCompleteAsync(_currentCommand!);

                _currentCommand = null;

                break;
            }

            //３）異常完了：IDLE・ALARMになって報告し、実行した指示を消す
            if (input == "2")
            {
                _consoleView.ShowAlarm("異常完了");
                Logger.Info("異常完了選択");

                _stateController.SetIdle();
                _stateController.SetAlarm();

                await _apiClient.ReportCommandFailedAsync(_currentCommand!);
                await _apiClient.ReportAlarmAsync(_state.EqpName);

                _consoleView.ShowAlarm("※　設備異常発生　※");
                
                _currentCommand = null;

                break;
            }

            //４）１・２以外入力
            _consoleView.ShowInfo("入力エラーです。1 または 2 を入力してください。");

        }
    }
    

    //ALARM時処理　
    //-------------------------------------------------------------------------------

    public async Task RunAlarmProcessAsync()
    {
        //ONLINE/OFFLINE
        if (_state.CommunicationStatus == CommunicationStatus.Online)
        {
            //ONLINE
            if (_onlineStartTime == null)
            {
                //ONLINE検知時間記録
                _onlineStartTime = DateTime.Now;
            }

            if (DateTime.Now - _onlineStartTime >= TimeSpan.FromSeconds(5))
            {
                //ONLINE状態5秒経過　異常解除
                _stateController.ClearAlarm();

                await _apiClient.ReportRecoveryAsync(_state.EqpName);

                _consoleView.ShowInfo("異常解除");

                _onlineStartTime = null;
            }
        }
        else
        {
            //OFFLINE
            _onlineStartTime = null;
        }
    }


    //払出完了受信時に出庫可能常態に移行
    //-------------------------------------------------------------------------------
    public void SetRetrieveAvailable()
    {
        _stateController.SetRetrieveAvailable();

        _consoleView.ShowInfo("出庫口のキャリアが払出され、出庫可能になりました");

    }


}

