using System.ComponentModel.DataAnnotations;

namespace FinalAssigenment.Models;

public class EquipmentName
{
    [Required(ErrorMessage = "設備IDが未入力または空白です。")]
    public string EqpName {  get; set; }
}
