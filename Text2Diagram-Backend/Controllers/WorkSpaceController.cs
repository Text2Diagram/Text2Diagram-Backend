using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.ViewModels;

namespace Text2Diagram_Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class WorkSpaceController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    public WorkSpaceController(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int page, int pageSize)
    {
        page = page == 0 ? 1 : page;
        pageSize = pageSize == 0 ? 20 : pageSize;
        var temp = await _dbContext.Workspaces.ToListAsync();
        var data = temp.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPage = (int)Math.Ceiling(temp.Count() * 1.0 / pageSize);
        return Ok(FormatData.FormatDataFunc(page, pageSize, totalPage, data));
    }

    [HttpGet("ownerId/{id}")]
    public async Task<IActionResult> GetByOwnerId(string id)
    {
        var result = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.OwnerId == id);

        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSingle(Guid id)
    {
        var result = await _dbContext.Workspaces.FindAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkspaceVM item)
    {
        var newItem = _mapper.Map<Workspace>(item); // DTO → Entity
        _dbContext.Workspaces.Add(newItem);
        await _dbContext.SaveChangesAsync();
        var result = await _dbContext.Workspaces.FindAsync(newItem.Id);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, WorkspaceVM item)
    {
        var newItem = _mapper.Map<Workspace>(item); // DTO → Entity
        var editItem = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu");
        _mapper.Map<WorkspaceVM, Workspace>(item, editItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PartialUpdate(Guid id, [FromBody] JsonPatchDocument patchModel)
    {
        var editItem = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu.");

        var itemVm = _mapper.Map<Workspace, WorkspaceVM>(editItem);

        patchModel.ApplyTo(itemVm);

        _mapper.Map<WorkspaceVM, Workspace>(itemVm, editItem);

        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var editItem = await _dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu");
        _dbContext.Workspaces.Remove(editItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}
