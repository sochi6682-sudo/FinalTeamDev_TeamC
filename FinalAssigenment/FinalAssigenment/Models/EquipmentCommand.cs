using System.ComponentModel.DataAnnotations;

namespace FinalAssigenment.Models;

public class EquipmentCommand
{
    [Required]
    public string CommandId { get; set; }
    [Required]
    public int CommandType { get; set; }
    [Required]
    public string CarrierId { get; set; }
    [Required]
    public string EqpName { get; set; }
    [Required]
    public string Location { get; set; }
    [Required]
    public int CommandStatus { get; set; }

}
