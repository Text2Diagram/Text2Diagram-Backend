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
	public class ShareController : Controller
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly IMapper _mapper;
		public ShareController(ApplicationDbContext dbContext, IMapper mapper)
		{
			_dbContext = dbContext;
			_mapper = mapper;
		}

		[HttpGet]
		public async Task<IActionResult> GetAll(int page, int pageSize)
		{
			page = page == null || page == 0 ? 1 : page;
			pageSize = pageSize == null || pageSize == 0 ? 20 : pageSize;
			var temp = await _dbContext.Shares.ToListAsync();
			var data = temp.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			var totalPage = (int)Math.Ceiling(temp.Count() * 1.0 / pageSize);
			return Ok(FormatData.FormatDataFunc(page, pageSize, totalPage, data));
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetSingle(Guid id)
		{
			var result = await _dbContext.Shares.FindAsync(id);
			return Ok(FormatData.FormatDataFunc(0, 0, 0, result));
		}

		[HttpPost]
		public async Task<IActionResult> Create(ShareVM item)
		{
			var newItem = _mapper.Map<Share>(item); // DTO → Entity
			_dbContext.Shares.Add(newItem);
			await _dbContext.SaveChangesAsync();
			var result = await _dbContext.Shares.FindAsync(newItem.Id);
			return Ok(FormatData.FormatDataFunc(0, 0, 0, result));
		}

		[HttpPut]
		public async Task<IActionResult> Update(ShareVM item)
		{
			var newItem = _mapper.Map<Share>(item); // DTO → Entity
			var editItem = _dbContext.Shares.Single(x => x.Id == item.Id);
			if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu");
			_mapper.Map<ShareVM, Share>(item, editItem);
			await _dbContext.SaveChangesAsync();
			return NoContent();
		}

		[HttpPatch("{id}")]
		public ActionResult PartialUpdate(Guid id, [FromBody] JsonPatchDocument patchModel)
		{
			var editItem = _dbContext.Shares.Single(x => x.Id == id);
			if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu.");

			var itemVm = _mapper.Map<Share, ShareVM>(editItem);

			patchModel.ApplyTo(itemVm);

			_mapper.Map<ShareVM, Share>(itemVm, editItem);

			_dbContext.SaveChangesAsync();
			return Ok();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			var editItem = _dbContext.Shares.Single(x => x.Id == id);
			if (editItem == null)
				return BadRequest("Không tồn tại dữ liệu");
			_dbContext.Shares.Remove(editItem);
			await _dbContext.SaveChangesAsync();
			return NoContent();
		}
	}
}
