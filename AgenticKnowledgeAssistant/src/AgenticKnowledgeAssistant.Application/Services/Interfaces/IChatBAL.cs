using AgenticKnowledgeAssistant.DTO.CommonDTOs;
using AgenticKnowledgeAssistant.DTO.RequestDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IChatBAL
{
    Task<Response<object>> Chat(ChatRequestDTO request, CancellationToken cancellationToken = default);
}
