namespace Text2Diagram_Backend.Controllers
{
	public class FormatData
	{
		public static object FormatDataFunc(int page, int pageSize, object data)
		{
			return new
			{
				page = page,
				pageSize = pageSize,
				data = data
			};
		}
	}
}
