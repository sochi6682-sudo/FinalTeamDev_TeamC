using System;
using System.Collections.Generic;
using System.Text;

namespace Equipment2.Models;

public enum ControlState 
{ 
    Offline = 0, 
    Online = 1 
}
public enum EquipmentStatus
{
    Idle = 0,
    Active = 1
}
public enum AlarmStatus
{
    NoAlarm = 0, 
    Alarm = 1
}


public class StateReport
{
    public string EqpName { get; set; } = "EQP02";
    public ControlState ControlState { get; set; } = ControlState.Offline;
    public EquipmentStatus EquipmentStatus { get; set; } = EquipmentStatus.Idle;
    public AlarmStatus AlarmStatus { get; set; } = AlarmStatus.NoAlarm;

}
