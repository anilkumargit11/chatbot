using AgenticKnowledgeAssistant.DTO.CommonDTOs;

namespace AgenticKnowledgeAssistant.BAL;

public sealed class CommonBAL : Interfaces.ICommonBAL
{
    public Response<object> Success(object? data, DateTime startTime)
    {
        return new Response<object>
        {
            ReturnCode = (int)CommonResponse.CommonResponseErrorCodes.Success,
            ReturnMessage = "success",
            Data = data,
            ResponseTime = Math.Round((DateTime.Now - startTime).TotalMilliseconds).ToString()
        };
    }

    public Response<object> Failure(int returnCode, string returnMessage, DateTime startTime)
    {
        return new Response<object>
        {
            ReturnCode = returnCode,
            ReturnMessage = returnMessage,
            ResponseTime = Math.Round((DateTime.Now - startTime).TotalMilliseconds).ToString()
        };
    }
}
