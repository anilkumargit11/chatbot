using AgenticKnowledgeAssistant.DTO.CommonDTOs;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface ICommonBAL
{
    Response<object> Success(object? data, DateTime startTime);
    Response<object> Failure(int returnCode, string returnMessage, DateTime startTime);
}
