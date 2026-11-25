using System.Security.Claims;
using GestionEmpresarial.Api.Authorization;
using GestionEmpresarial.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpresarial.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/categories")]
public sealed class CategoriesController(ICatalogService catalogService) : ControllerBase
{
    private const string CatalogManagerRoles = RoleNames.Administrator + "," + RoleNames.WarehouseManager;

    [HttpGet]
    public async Task<ActionResult<PageResult<CategoryDto>>> GetCategories(
        [FromQuery] CategoryQuery query,
        CancellationToken cancellationToken) =>
        Ok(await catalogService.GetCategoriesAsync(query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id, CancellationToken cancellationToken)
    {
        var category = await catalogService.GetCategoryAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = CatalogManagerRoles)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        CategoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        var category = await catalogService.CreateCategoryAsync(
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = CatalogManagerRoles)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(
        int id,
        CategoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        var category = await catalogService.UpdateCategoryAsync(
            id,
            request,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = CatalogManagerRoles)]
    public async Task<IActionResult> DeactivateCategory(int id, CancellationToken cancellationToken)
    {
        var deactivated = await catalogService.DeactivateCategoryAsync(
            id,
            CurrentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
        return deactivated ? NoContent() : NotFound();
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("The authenticated user has no identifier claim.");
}
