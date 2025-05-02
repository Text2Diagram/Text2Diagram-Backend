using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.Data;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Text2Diagram_Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public ProjectsController(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int page, int pageSize)
    {
        page = page == 0 ? 1 : page;
        pageSize = pageSize == 0 ? 20 : pageSize;
        var temp = await _dbContext.Projects.ToListAsync();
        var data = temp.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPage = (int)Math.Ceiling(temp.Count() * 1.0 / pageSize);
        return Ok(FormatData.FormatDataFunc(page, pageSize, totalPage, data));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSingle(Guid id)
    {
        var result = await _dbContext.Projects.FindAsync(id);
        return Ok(result);
    }

    [HttpGet("by-workspaceid/{code}")]
    public async Task<IActionResult> GetByWorkSpaceId(Guid code, int page, int pageSize)
    {
        page = page == 0 ? 1 : page;
        pageSize = pageSize == 0 ? 20 : pageSize;
        var temp = await _dbContext.Projects.Where(x => x.WorkspaceId == code).OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).ToListAsync();
        var data = temp.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPage = (int)Math.Ceiling(temp.Count() * 1.0 / pageSize);
        return Ok(FormatData.FormatDataFunc(page, pageSize, totalPage, data));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectVM item)
    {
        var newItem = _mapper.Map<Project>(item);
        _dbContext.Projects.Add(newItem);
        await _dbContext.SaveChangesAsync();
        var result = await _dbContext.Projects.FindAsync(newItem.Id);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProjectVM item)
    {
        var newItem = _mapper.Map<Project>(item);
        var editItem = await _dbContext.Projects.FirstOrDefaultAsync(x => x.Id == id);

        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu");

        editItem.UpdatedAt = DateTime.UtcNow;
        if (editItem == null)
            return BadRequest("Không tồn tại dữ liệu");
        _mapper.Map<ProjectVM, Project>(item, editItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
    //"e1ab165c-ba3c-482d-88d5-098b29109472"

    [HttpPatch("{id}")]
    public async Task<ActionResult> PartialUpdate(Guid id, [FromBody] JsonPatchDocument patchModel)
    {
        var editItem = await _dbContext.Projects.FirstOrDefaultAsync(x => x.Id == id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu.");
        editItem.UpdatedAt = DateTime.UtcNow;
        if (editItem == null)
            return BadRequest("Không tồn tại dữ liệu.");

        var itemVm = _mapper.Map<Project, ProjectVM>(editItem);

        patchModel.ApplyTo(itemVm);

        _mapper.Map<ProjectVM, Project>(itemVm, editItem);

        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var editItem = await _dbContext.Projects.FirstOrDefaultAsync(x => x.Id == id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu");
        _dbContext.Projects.Remove(editItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

}
