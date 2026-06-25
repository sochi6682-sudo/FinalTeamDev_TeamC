using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace Equipment3.Models;

public enum CommunicationStatus
{
    Offline = 0,
    Online = 1
}
public enum CommandReceptionStatus
{
    Idle = 0,
    Active = 1
}
public enum LocalAlarmStatus
{
    NoAlarm = 0,
    Alarm = 1
}
public enum OperatingStatus
{
    Stop = 0,
    Busy = 1
}
public enum RetrieveAvailability
{
    Available = 0,
    Unavailable = 1
}

public class State
{
    public string EqpName { get; set; } = "EQP03";
    public CommunicationStatus CommunicationStatus { get; set; } = CommunicationStatus.Offline;
    public CommandReceptionStatus CommandReceptionStatus { get; set; } = CommandReceptionStatus.Idle;
    public LocalAlarmStatus LocalAlarmStatus { get; set; } = LocalAlarmStatus.NoAlarm;
    public OperatingStatus OperatingStatus { get; set; } = OperatingStatus.Stop;
    public RetrieveAvailability RetrieveAvailability { get; set; } = RetrieveAvailability.Available;

}


