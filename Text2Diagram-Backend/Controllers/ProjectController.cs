using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.ViewModels;
using Microsoft.AspNetCore.JsonPatch;

namespace Text2Diagram_Backend.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ProjectController : ControllerBase
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IMapper _mapper;

		public ProjectController(ApplicationDbContext dbContext, IMapper mapper)
		{
			_dbContext = dbContext;
			_mapper = mapper;
		}

		[HttpGet]
		public IActionResult GetAll(int page, int pageSize)
		{
			var result = _dbContext.Projects.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			return Ok(FormatData.FormatDataFunc(page, pageSize, result));
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetSingle(Guid id)
		{
			var result = await _dbContext.Projects.FindAsync(id);
			return Ok(result);
		}

		[HttpPost]
		public async Task<IActionResult> Create(ProjectVM item)
		{
			var newItem = _mapper.Map<Project>(item); // DTO → Entity
			_dbContext.Projects.Add(newItem);
			await _dbContext.SaveChangesAsync();
			var result = await _dbContext.Projects.FindAsync(newItem.Id);
			return Ok(result);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Update(Guid id, ProjectVM item)
		{
			var newItem = _mapper.Map<Project>(item); // DTO → Entity
			var editItem = _dbContext.Projects.Single(x => x.Id == id);
			editItem.UpdatedAt = DateTime.UtcNow;
			if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu");
			_mapper.Map<ProjectVM, Project>(item, editItem);
			await _dbContext.SaveChangesAsync();
			return NoContent();
		}
		//"e1ab165c-ba3c-482d-88d5-098b29109472"

		[HttpPatch("{id}")]
		public ActionResult PartialUpdate(Guid id, [FromBody] JsonPatchDocument patchModel)
		{
			var editItem = _dbContext.Projects.Single(x => x.Id == id);
            editItem.UpdatedAt = DateTime.UtcNow;
            if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu.");

			var itemVm = _mapper.Map<Project, ProjectVM>(editItem);

			patchModel.ApplyTo(itemVm);

			_mapper.Map<ProjectVM, Project>(itemVm, editItem);

			_dbContext.SaveChangesAsync();
			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			var editItem = _dbContext.Projects.Single(x => x.Id == id);
			if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu");
			_dbContext.Projects.Remove(editItem);
			await _dbContext.SaveChangesAsync();
			return NoContent();
		}

	}
}
