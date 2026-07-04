using System.Threading;
using System.Threading.Tasks;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface IOcrService
{
    Task<string> ExtractTextFromImageAsync(string base64Content, string fileName, CancellationToken cancellationToken = default);
}
