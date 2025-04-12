using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.ViewModels;

namespace Text2Diagram_Backend.Controllers
{
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
		public IActionResult GetAll(int page, int pageSize)
		{
			var result = _dbContext.Workspaces.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			return Ok(FormatData.FormatDataFunc(page, pageSize, result));
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetSingle(Guid id)
		{
			var result = await _dbContext.Workspaces.FindAsync(id);
			return Ok(FormatData.FormatDataFunc(0, 0, result));
		}

		[HttpPost]
		public async Task<IActionResult> Create(WorkspaceVM item)
		{
			var newItem = _mapper.Map<Workspace>(item); // DTO → Entity
			_dbContext.Workspaces.Add(newItem);
			await _dbContext.SaveChangesAsync();
			var result = await _dbContext.Workspaces.FindAsync(newItem.Id);
			return Ok(FormatData.FormatDataFunc(0, 0, result));
		}

		[HttpPut]
		public async Task<IActionResult> Update(WorkspaceVM item)
		{
			var newItem = _mapper.Map<Workspace>(item); // DTO → Entity
			var editItem = _dbContext.Workspaces.Single(x => x.Id == item.Id);
			if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu");
			_mapper.Map<WorkspaceVM, Workspace>(item, editItem);
			await _dbContext.SaveChangesAsync();
			return NoContent();
		}

		[HttpPatch("{id}")]
		public ActionResult PartialUpdate(Guid id, [FromBody] JsonPatchDocument patchModel)
		{
			var editItem = _dbContext.Workspaces.Single(x => x.Id == id);
			if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu.");

			var itemVm = _mapper.Map<Workspace, WorkspaceVM>(editItem);

			patchModel.ApplyTo(itemVm);

			_mapper.Map<WorkspaceVM, Workspace>(itemVm, editItem);

			_dbContext.SaveChangesAsync();
			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			var editItem = _dbContext.Workspaces.Single(x => x.Id == id);
			if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu");
			_dbContext.Workspaces.Remove(editItem);
			await _dbContext.SaveChangesAsync();
			return NoContent();
		}
	}
}
