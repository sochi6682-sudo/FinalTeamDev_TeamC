using Equipment1.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Equipment1.Services;

public class StateController
{
    private readonly State _state;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public StateController(State state)
    {
        _state = state;
    }

    public void InitStatus()
    {
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

    }

    public void SetCommunicationOnline()
    {
        _state.CommunicationStatus = CommunicationStatus.Online;
        Logger.Info("サーバー通信状態：ON-LINE");
    }

    public void SetCommunicationOffline()
    {
        _state.CommunicationStatus = CommunicationStatus.Offline;
        Logger.Info("サーバー通信状態：OFF-LINE");
    }

    public void SetAlarm()
    {
        _state.LocalAlarmStatus = LocalAlarmStatus.Alarm;
        Logger.Info("異常状態：ALARM");
        UpdateOperatingStatus();
    }

    public void ClearAlarm()
    {
        _state.LocalAlarmStatus = LocalAlarmStatus.NoAlarm;
        Logger.Info("異常状態：NO ALARM");
        UpdateOperatingStatus();
    }

    public void SetActive()
    {
        _state.CommandReceptionStatus = CommandReceptionStatus.Active;
        Logger.Info("指示実行状態：ACTIVE");
        UpdateOperatingStatus();
    }

    public void SetIdle()
    {
        _state.CommandReceptionStatus = CommandReceptionStatus.Idle;
        Logger.Info("指示実行状態：IDLE");
        UpdateOperatingStatus();
    }

    public void SetRetrieveAvailable()
    {
        _state.RetrieveAvailability = RetrieveAvailability.Available;
        Logger.Info("出庫可能");
    }

    public void SetRetrieveUnavailable()
    {
        _state.RetrieveAvailability = RetrieveAvailability.Unavailable;
        Logger.Info("出庫不可");
    }

    private void UpdateOperatingStatus()
    {
        if (_state.CommandReceptionStatus == CommandReceptionStatus.Active &&
            _state.LocalAlarmStatus == LocalAlarmStatus.NoAlarm)
        {
            _state.OperatingStatus = OperatingStatus.Busy;
            Logger.Info("動作状態：BUSY");
        }
        else
        {
            _state.OperatingStatus = OperatingStatus.Stop;
            Logger.Info("動作状態：STOP");
        }
    }
}