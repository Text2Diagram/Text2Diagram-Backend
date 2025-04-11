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
	public class WorkspaceMemberController : Controller
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IMapper _mapper;

		public WorkspaceMemberController(ApplicationDbContext dbContext, IMapper mapper)
		{
			_dbContext = dbContext;
			_mapper = mapper;
		}

		[HttpGet]
		public IActionResult GetAll(int page, int pageSize)
		{
			try
			{
				var result = _dbContext.WorkspaceMembers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
				return Ok(FormatData.FormatDataFunc(page, pageSize, result));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetSingle(Guid id)
		{
			try
			{
				var result = await _dbContext.WorkspaceMembers.FindAsync(id);
				return Ok(FormatData.FormatDataFunc(0, 0, result));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPost]
		public async Task<IActionResult> Create(WorkspaceMemberVM item)
		{
			try
			{
				var newItem = _mapper.Map<WorkspaceMember>(item); // DTO → Entity
				_dbContext.WorkspaceMembers.Add(newItem);
				await _dbContext.SaveChangesAsync();
				var result = await _dbContext.WorkspaceMembers.FindAsync(newItem.Id);
				return Ok(FormatData.FormatDataFunc(0, 0, result));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPut]
		public async Task<IActionResult> Update(WorkspaceMemberVM item)
		{
			try
			{
				var newItem = _mapper.Map<WorkspaceMember>(item); // DTO → Entity
				var editItem = _dbContext.WorkspaceMembers.Single(x => x.Id == item.Id);
				if (editItem == null)
					return BadRequest("Không tồn tại dữ liệu");
				_dbContext.WorkspaceMembers.Add(newItem);
				_mapper.Map<WorkspaceMemberVM, WorkspaceMember>(item, editItem);
				await _dbContext.SaveChangesAsync();
				return NoContent();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPatch("{id}")]
		public ActionResult PartialUpdate(Guid id, [FromBody] JsonPatchDocument patchModel)
		{
			try
			{
				var editItem = _dbContext.WorkspaceMembers.Single(x => x.Id == id);
				if (editItem == null)
					return BadRequest("Không tồn tại dữ liệu.");

				var itemVm = _mapper.Map<WorkspaceMember, WorkspaceMemberVM>(editItem);

				patchModel.ApplyTo(itemVm);

				_mapper.Map<WorkspaceMemberVM, WorkspaceMember>(itemVm, editItem);

				_dbContext.SaveChangesAsync();
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			try
			{
				var editItem = _dbContext.WorkspaceMembers.Single(x => x.Id == id);
				if (editItem == null)
					return BadRequest("Không tồn tại dữ liệu");
				_dbContext.WorkspaceMembers.Remove(editItem);
				await _dbContext.SaveChangesAsync();
				return NoContent();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}
