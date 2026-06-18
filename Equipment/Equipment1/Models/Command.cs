using System;
using System.Collections.Generic;
using System.Text;

namespace Equipment1.Models;

public class Command
{
    public string CommandId { get; set; } = "";
    public int CommandType { get; set; }
    public string CarrierId { get; set; } = "";
    public string EqpName { get; set; } = "";
    public string Location { get; set; } = "";
    public int CommandStatus { get; set; }
}