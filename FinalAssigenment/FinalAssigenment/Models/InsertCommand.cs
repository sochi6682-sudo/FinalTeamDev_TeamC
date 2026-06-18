using System.ComponentModel.DataAnnotations;

namespace FinalAssigenment.Models;

public class InsertCommand
{
    [Required]
    public int CommandType { get; set; }
    [Required]
    public string CarrierId { get; set; }
    [Required]
    public string EqpName { get; set; }
}
