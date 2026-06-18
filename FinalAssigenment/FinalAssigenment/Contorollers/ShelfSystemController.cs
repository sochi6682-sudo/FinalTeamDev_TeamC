using FinalAssigenment.Repositories;
using FinalAssigenment.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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


}
