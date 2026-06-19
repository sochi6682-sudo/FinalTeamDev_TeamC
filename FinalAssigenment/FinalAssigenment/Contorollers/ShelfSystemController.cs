using FinalAssigenment.Models;
using FinalAssigenment.Repositories;
using FinalAssigenment.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.Design;

namespace FinalAssigenment.Contorollers;

[Route("api/shelf-system")]
[ApiController]
public class ShelfSystemController : ControllerBase
{
    private readonly ILogger<ShelfSystemController> _logger;
    private readonly SqlRepository _repository;
    private readonly ShelfSystemService _service;
    public ShelfSystemController(
        ILogger<ShelfSystemController> logger,
        SqlRepository repository,
        ShelfSystemService service)
    {
        _logger = logger;
        _repository = repository;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetInformationAsync()
    {
        try
        {
            var systemInfo = await _repository.SelectInfoAsync();
            return Ok(systemInfo);
        }
        catch (Exception)
        {
            return StatusCode(500, "情報取得失敗");
        }
    }
    [HttpGet("request")]
    public async Task<IActionResult> GetRequestAsync()
    {
        try
        {
            DateTime sendAt = DateTime.Now;
            var sendCommand = await _repository.GetCommandRequestAsync(sendAt);
            //_logger.LogInformation( $"搬送指示送信 CommandID＝{CommandID},EqpName＝{EqpName}");
            return Ok(sendCommand);
        }
        catch (Exception)
        {
            return StatusCode(500, "送信指示取得失敗");
        }
    }
    [HttpPost("command")]
    public async Task<IActionResult> PostCommandAsync([FromBody] InsertCommand newCommand)
    {
        try
        {
            
            return Created("", null);
        }
        catch (Exception)
        {
            return StatusCode(500, "搬送指示登録失敗(想定外例外)");
        }
    }
    [HttpPost("unload")]
    public async Task<IActionResult> PostUnloadAsync([FromBody] RelayCommand unload)
    {
        try
        {
            await _service.UnloadValidationAsync(unload);
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "サーバへ払出完了報告失敗");
        }
    }
    [HttpPost("online")]
    public IActionResult PostOnline([FromBody] EquipmentState online)
    {
        try
        {
            int statusType = 0;
            _service.UpdateEqpStatus(online.EqpName, statusType);
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "設備ONLINE報告失敗");
        }
    }
    [HttpPost("start")]
    public async Task<IActionResult> PostStartAsync([FromBody] EquipmentCommand start)
    {
        try
        {
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "設備ONLINE報告失敗");
        }
    }
    [HttpPost("completion")]
    public async Task<IActionResult> CompletionAsync([FromBody] EquipmentCommand completion)
    {
        try
        {
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "設備ONLINE報告失敗");
        }
    }
    [HttpPost("incident")]
    public async Task<IActionResult> PostIncidentAsync([FromBody] string eqpName)
    {
        try
        {
            if(string.IsNullOrWhiteSpace(eqpName)) return BadRequest("eqpNameが未入力です。");
            int statusType = 2;
            _service.UpdateEqpStatus(eqpName, statusType);
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "設備ONLINE報告失敗");
        }
    }
    [HttpPost("recovery")]
    public async Task<IActionResult> PostRecoveryAsync([FromBody] string eqpName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eqpName)) return BadRequest("eqpNameが未入力です。");
            int statusType = 2;
            _service.UpdateEqpStatus(eqpName, statusType);
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "設備ONLINE報告失敗");
        }
    }


}
