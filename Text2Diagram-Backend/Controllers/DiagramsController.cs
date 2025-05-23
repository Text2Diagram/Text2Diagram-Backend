﻿using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.ViewModels;

namespace Text2Diagram_Backend.Controllers;

[ApiController]
[Route("[controller]")]

public class DiagramsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public DiagramsController(ApplicationDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int page, int pageSize)
    {
        page = page == 0 ? 1 : page;
        pageSize = pageSize == 0 ? 20 : pageSize;
        var temp = await _dbContext.Diagrams.ToListAsync();
        var data = temp.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPage = (int)Math.Ceiling(temp.Count() * 1.0 / pageSize);
        return Ok(FormatData.FormatDataFunc(page, pageSize, totalPage, data));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSingle(Guid id)
    {
        var result = await _dbContext.Diagrams.FindAsync(id);

        if (result == null)
            return NotFound($"Diagram with id {id} not found.");

        return Ok(FormatData.FormatDataFunc(0, 0, 0, result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(DiagramVM item)
    {
        var newItem = _mapper.Map<Diagram>(item);
        _dbContext.Diagrams.Add(newItem);
        await _dbContext.SaveChangesAsync();
        return Ok(FormatData.FormatDataFunc(0, 0, 0, newItem));
    }

    [HttpPut]
    public async Task<IActionResult> Update(DiagramVM item)
    {
        var newItem = _mapper.Map<Diagram>(item);
        var editItem = _dbContext.Diagrams.Single(x => x.Id == item.Id);
        if (editItem == null)
            return NotFound("Không tồn tại dữ liệu");
        _mapper.Map<DiagramVM, Diagram>(item, editItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
    //"e1ab165c-ba3c-482d-88d5-098b29109472"

    [HttpPatch("{id}")]
    public ActionResult PartialUpdate(Guid id, [FromBody] JsonPatchDocument patchModel)
    {
        var editItem = _dbContext.Diagrams.Single(x => x.Id == id);
        if (editItem == null)
            return BadRequest("Không tồn tại dữ liệu.");

        var itemVm = _mapper.Map<Diagram, DiagramVM>(editItem);

        patchModel.ApplyTo(itemVm);

        _mapper.Map<DiagramVM, Diagram>(itemVm, editItem);

        _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var editItem = _dbContext.Diagrams.Single(x => x.Id == id);
        if (editItem == null)
            return BadRequest("Không tồn tại dữ liệu");
        _dbContext.Diagrams.Remove(editItem);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

}
