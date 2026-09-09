using AskMe.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AskMe.API.Controllers;

// Serves previously-uploaded images through the IFileStorageService
// abstraction - works the same whether the active implementation is
// PostgresImageStorageService or CloudflareR2StorageService, and the
// controller never touches EF Core/Infrastructure directly.
[ApiController]
[Route("api/images")]
[AllowAnonymous]
public class ImagesController : ControllerBase
{
    private readonly IFileStorageService _storage;

    public ImagesController(IFileStorageService storage) => _storage = storage;

    [HttpGet("{id}")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var file = await _storage.GetAsync(id, ct);
        if (file is null)
            return NotFound();

        return File(file.Data, file.ContentType);
    }
}