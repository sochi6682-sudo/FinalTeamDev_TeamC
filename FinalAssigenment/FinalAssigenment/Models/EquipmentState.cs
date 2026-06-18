using System.ComponentModel.DataAnnotations;

namespace FinalAssigenment.Models;

public class EquipmentState
{
    [Required]
    public string EqpName { get; set; }
    [Required]
    public int ControlState { get; set; } = 0;
    [Required]
    public int EquipmentStatus { get; set; } = 0;
    [Required] 
    public int AlarmStatus { get; set; } = 0;
}
