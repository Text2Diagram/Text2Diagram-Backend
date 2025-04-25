using AutoMapper;
using Text2Diagram_Backend.Data.Models;
using Text2Diagram_Backend.ViewModels;

namespace Text2Diagram_Backend.Services
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<Diagram, DiagramVM>();         // Entity → DTO
			CreateMap<DiagramVM, Diagram>();         // DTO → Entity

			CreateMap<Workspace, WorkspaceVM>();         // Entity → DTO
			CreateMap<WorkspaceVM, Workspace>();         // DTO → Entity

			CreateMap<Share, ShareVM>();         // Entity → DTO
			CreateMap<ShareVM, Share>();         // DTO → Entity

			CreateMap<WorkspaceMember, WorkspaceMemberVM>();         // Entity → DTO
			CreateMap<WorkspaceMemberVM, WorkspaceMember>();         // DTO → Entity

			CreateMap<Project, ProjectVM>();         // Entity → DTO
			CreateMap<ProjectVM, Project>();         // DTO → Entity
		}
	}
}
