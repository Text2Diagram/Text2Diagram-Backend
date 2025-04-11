using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Text2Diagram_Backend.Data;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.ViewModels;

namespace Text2Diagram_Backend.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class DiagramController : Controller
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IMapper _mapper;

		public DiagramController(ApplicationDbContext dbContext, IMapper mapper)
		{
			_dbContext = dbContext;
			_mapper = mapper;
		}

		[HttpGet]
		public IActionResult GetAll(int page, int pageSize)
		{
			try
			{
				var result = _dbContext.Diagrams.Skip(page).Take(pageSize).ToList();
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
				var result = await _dbContext.Diagrams.FindAsync(id);
				return Ok(FormatData.FormatDataFunc(0,0,result));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPost]
		public async Task<IActionResult> Create(DiagramVM item)
		{
			try
			{
				var newItem = _mapper.Map<Diagram>(item); // DTO → Entity
				_dbContext.Diagrams.Add(newItem);
				await _dbContext.SaveChangesAsync();
				var result = await _dbContext.Diagrams.FindAsync(newItem.Id);
				return Ok(FormatData.FormatDataFunc(0, 0, result));
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPut]
		public async Task<IActionResult> Update(DiagramVM item)
		{
			try
			{
				var newItem = _mapper.Map<Diagram>(item); // DTO → Entity
				var editItem = _dbContext.Diagrams.Single(x => x.Id == item.Id);
				if (editItem == null)
					return BadRequest("Không tồn tại dữ liệu");
				_dbContext.Diagrams.Add(newItem);
				_mapper.Map<DiagramVM, Diagram>(item, editItem);
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
				var editItem = _dbContext.Diagrams.Single(x => x.Id == id);
				if (editItem == null)
					return BadRequest("Không tồn tại dữ liệu.");

				var itemVm = _mapper.Map<Diagram, DiagramVM>(editItem);

				patchModel.ApplyTo(itemVm);

				_mapper.Map<DiagramVM, Diagram>(itemVm, editItem);

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
				var editItem = _dbContext.Diagrams.Single(x => x.Id == id);
				if (editItem == null)
					return BadRequest("Không tồn tại dữ liệu");
				_dbContext.Diagrams.Remove(editItem);
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
