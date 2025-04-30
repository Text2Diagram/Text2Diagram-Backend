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
public class WorkspaceMembersController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public WorkspaceMembersController(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int page, int pageSize)
    {
        page = page == 0 ? 1 : page;
        pageSize = pageSize == 0 ? 20 : pageSize;
        var temp = await _dbContext.WorkspaceMembers.ToListAsync();
        var data = temp.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPage = (int)Math.Ceiling(temp.Count() * 1.0 / pageSize);
        return Ok(FormatData.FormatDataFunc(page, pageSize, totalPage, data));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSingle(Guid id)
    {
        var result = await _dbContext.WorkspaceMembers.FindAsync(id);
        if (result == null)
            return NotFound("Không tồn tại dữ liệu");
        return Ok(FormatData.FormatDataFunc(0, 0, 0, result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(WorkspaceMemberVM item)
    {
        var newItem = _mapper.Map<WorkspaceMember>(item);
        _dbContext.WorkspaceMembers.Add(newItem);
        await _dbContext.SaveChangesAsync();
        return Ok(FormatData.FormatDataFunc(0, 0, 0, newItem));
    }

    [HttpPut]
    public async Task<IActionResult> Update(WorkspaceMemberVM item)
    {
        var newItem = _mapper.Map<WorkspaceMember>(item);
        var editItem = await _dbContext.WorkspaceMembers.FirstOrDefaultAsync(x => x.Id == item.Id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu");
        _mapper.Map<WorkspaceMemberVM, WorkspaceMember>(item, editItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PartialUpdate(Guid id, [FromBody] JsonPatchDocument patchModel)
    {
        var editItem = await _dbContext.WorkspaceMembers.FirstOrDefaultAsync(x => x.Id == id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu.");

        var itemVm = _mapper.Map<WorkspaceMember, WorkspaceMemberVM>(editItem);

        patchModel.ApplyTo(itemVm);

        _mapper.Map<WorkspaceMemberVM, WorkspaceMember>(itemVm, editItem);

        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var editItem = await _dbContext.WorkspaceMembers.FirstOrDefaultAsync(x => x.Id == id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu");
        _dbContext.WorkspaceMembers.Remove(editItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}
