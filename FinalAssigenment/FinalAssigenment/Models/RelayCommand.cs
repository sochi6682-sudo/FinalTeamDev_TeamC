using System.ComponentModel.DataAnnotations;

namespace FinalAssigenment.Models;

public class RelayCommand
{
    [Required]
    public string CommandId { get; set; }
    [Required]
    public string CarrierId { get; set; }
    [Required]
    public string EqpName { get; set; }
}
