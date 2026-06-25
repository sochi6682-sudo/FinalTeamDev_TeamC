using System;
using System.Collections.Generic;
using System.Text;

namespace Equipment1.Models;

public enum ControlStates 
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
    public string EqpName { get; set; } = "EQP01";
    public ControlStates ControlStates { get; set; } = ControlStates.Offline;
    public EquipmentStatus EquipmentStatus { get; set; } = EquipmentStatus.Idle;
    public AlarmStatus AlarmStatus { get; set; } = AlarmStatus.NoAlarm;

}
