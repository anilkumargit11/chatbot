namespace AgenticKnowledgeAssistant.DTO.CommonDTOs;

public static class CommonResponse
{
    public enum CommonResponseErrorCodes
    {
        Success = 200,
        BadRequest = 400,
        Unauthorized = 401,
        NotFound = 404,
        TooManyRequests = 429,
        TechnicalError = 500,
        InvalidRequest = 1001,
        MissingParameters = 1002,
        FailedToSave = 1003
    }
}
