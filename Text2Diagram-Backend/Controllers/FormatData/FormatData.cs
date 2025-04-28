namespace Text2Diagram_Backend.Controllers
{
	public class FormatData
	{
		public static object FormatDataFunc(int page, int pageSize, int totalPage, object data)
		{
			return new
			{
				page = page,
				pageSize = pageSize,
				totalPage = totalPage,
				data = data
			};
		}
	}
}
