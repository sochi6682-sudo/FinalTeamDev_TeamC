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
    private string _endPointName;

    [HttpGet]
    public async Task<IActionResult> GetInformationAsync()
    {
        try
        {
            _logger.LogInformation($"[Info] 情報取得受信");
            var systemInfo = await _repository.SelectInfomationAsync();
            systemInfo.Status = _service.EqpStatusList;
            return Ok(systemInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"情報取得失敗");
        }
    }
    [HttpGet("request")]
    public async Task<IActionResult> GetRequestAsync([FromQuery] string eqpName)
    {
        try
        {
            DateTime sendAt = DateTime.Now;
            _endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(eqpName, _endPointName);
            var sendCommand = await _repository.SelectCommandRequestAsync(eqpName, sendAt);
            if (sendCommand != null)
            {
                _service.TimeoutOccurred(sendCommand);
                _logger.LogInformation($"[Info] 搬送指示送信 CommandID＝{sendCommand.CommandId},EqpName＝{sendCommand.EqpName}");
            }
            return Ok(sendCommand);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"[Error]搬送指示要求GET失敗");
        }
    }
    [HttpPost("command")]
    public async Task<IActionResult> PostCommandAsync([FromBody] InsertCommand newCommand)
    {
        try
        {
            _logger.LogInformation($"[Info] 搬送指示受信");
            DateTime receptionAt = DateTime.Now;
            await _service.InsertValidationAsync(newCommand, receptionAt);
            return StatusCode(201, new 
            { 
                message = "搬送指示登録成功" 
            });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode((int)ex.StatusCode.Value, ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"[Error]搬送指示登録失敗");
        }
    }
    [HttpPost("unload")]
    public async Task<IActionResult> PostUnloadAsync([FromBody] RelayCommand unload)
    {
        try
        {
            _logger.LogInformation("[Info] 設備へ払出完了報告開始");
            _endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            bool isValueCheck = await _service.UnloadValidationAsync(unload, _endPointName);
            if (!isValueCheck)
            {
                return NotFound();
            }
            _logger.LogInformation("[Info] 設備へ払出完了報告成功");
            return Ok();
        }

        catch (Exception ex)
        {
            
            return StatusCode(500, $"[Error]サーバへ払出完了報告失敗");
        }
    }
    [HttpPost("online")]
    public IActionResult PostOnline([FromBody] EquipmentState online)
    {
        try
        {
            _endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(online.EqpName, _endPointName);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"[Error] 設備ONLINE報告失敗");
        }
    }
    [HttpPost("start")]
    public async Task<IActionResult> PostStartAsync([FromBody] EquipmentCommand start)
    {
        try
        {
            _endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(start.EqpName, _endPointName);
            await _repository.UpdateCommandStatusAsync(start.CommandId, (int)start.CommandStatus);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"[Error] 搬送指示開始報告失敗");
        }
    }
    [HttpPost("completion")]
    public async Task<IActionResult> PostCompletionAsync([FromBody] EquipmentCommand completion)
    {
        _logger.LogInformation($"[Info] 搬送指示完了受信 CommandID＝{completion.CommandId},EqpID＝{completion.EqpName}");
        try
        {
            DateTime completionAt = DateTime.Now;
            _endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(completion.EqpName, _endPointName);
            await _repository.UpdateCompletionAsync(completion, completionAt);
            _service.CancelTimeoutTimer(completion.CommandId);
            if (completion.CommandType == 0)
            {
                _logger.LogInformation("[Info] スマホへ出庫完了報告開始");
                RelayCommand sendCommand = new()
                {
                    CommandId = completion.CommandId,
                    CarrierId = completion.CarrierId,
                    EqpName = completion.EqpName
                };
                await _service.PostHttpClientAsync(sendCommand, _endPointName);
                _logger.LogInformation("[Info] スマホへ出庫完了報告成功");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"[Error] 搬送指示完了報告失敗");
        }
    }
    [HttpPost("incident")]
    public IActionResult PostIncident([FromBody] IncidentEqp incident)
    {
        try
        {
            _logger.LogInformation("[Info] 異常発生報告開始");
            if (string.IsNullOrWhiteSpace(incident.EqpName)) return BadRequest("eqpNameが未入力です。");
            _endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(incident.EqpName, _endPointName);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"[Error]異常発生報告失敗");
        }
    }
    [HttpPost("recovery")]
    public IActionResult PostRecovery([FromBody] IncidentEqp recovery)
    {
        try
        {
            _logger.LogInformation("[Info] 異常復旧報告開始");
            if (string.IsNullOrWhiteSpace(recovery.EqpName)) return BadRequest("eqpNameが未入力です。");
            _endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(recovery.EqpName, _endPointName);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"[Error]異常復旧報告失敗");
        }
    }


}
