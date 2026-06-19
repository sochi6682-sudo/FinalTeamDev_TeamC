using System.ComponentModel.DataAnnotations;

namespace FinalAssigenment.Models;

public class RelayCommand
{
    [Required(ErrorMessage = "搬送指示IDが未入力または空白です。")]
    public string CommandId { get; set; }
    [Required(ErrorMessage = "キャリアIDが未入力または空白です。")]
    public string CarrierId { get; set; }
    [Required(ErrorMessage = "設備IDが未入力または空白です。")]
    public string EqpName { get; set; }
}
