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
    private string endPointName;

    [HttpGet]
    public async Task<IActionResult> GetInformationAsync()
    {
        try
        {
            var systemInfo = await _repository.SelectInfomationAsync();
            systemInfo.Status = _service.eqpStatusList;
            return Ok(systemInfo);
        }
        catch (Exception)
        {
            return StatusCode(500, "情報取得失敗");
        }
    }
    [HttpGet("request")]
    public async Task<IActionResult> GetRequestAsync([FromQuery] string epqName)
    {
        try
        {
            var sendCommand = await _repository.GetCommandRequestAsync(epqName);

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
            await _service.InsertValidationAsync(newCommand);
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
            return StatusCode(500, ex.ToString());
        }
    }
    [HttpPost("unload")]
    public async Task<IActionResult> PostUnloadAsync([FromBody] RelayCommand unload)
    {
        try
        {
            endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            bool isValueCheck = await _service.UnloadValidationAsync(unload, endPointName);
            if (!isValueCheck)
            {
                return NotFound();
            }
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
            endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(online.EqpName, endPointName);
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
            endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(start.EqpName, endPointName);
            await _repository.UpdateCommandStatusAsync(start.CommandId, (int)start.CommandStatus);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString());
        }
    }
    [HttpPost("completion")]
    public async Task<IActionResult> PostCompletionAsync([FromBody] EquipmentCommand completion)
    {
        try
        {
            DateTime completionAt = DateTime.Now;
            endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(completion.EqpName, endPointName);
            await _repository.UpdateCompletionAsync(completion, completionAt);
            if (completion.CommandType == 0)
            {
                RelayCommand sendCommand = new()
                {
                    CommandId = completion.CommandId,
                    CarrierId = completion.CarrierId,
                    EqpName = completion.EqpName
                };
                await _service.PostHttpClientAsync(sendCommand, endPointName);
            }
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.ToString());
        }
    }
    [HttpPost("incident")]
    public IActionResult PostIncident([FromBody] string eqpName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eqpName)) return BadRequest("eqpNameが未入力です。");
            endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(eqpName, endPointName);
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "異常発生報告失敗");
        }
    }
    [HttpPost("recovery")]
    public IActionResult PostRecovery([FromBody] string eqpName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(eqpName)) return BadRequest("eqpNameが未入力です。");
            endPointName = HttpContext.Request.Path.Value.Split('/').Last();
            _service.UpdateEqpStatus(eqpName, endPointName);
            return Ok();
        }
        catch (Exception)
        {
            return StatusCode(500, "異常復旧報告失敗");
        }
    }


}
