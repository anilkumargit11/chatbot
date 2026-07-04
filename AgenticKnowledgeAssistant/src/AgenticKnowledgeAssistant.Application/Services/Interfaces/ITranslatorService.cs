using System.Threading;
using System.Threading.Tasks;

namespace AgenticKnowledgeAssistant.BAL.Interfaces;

public interface ITranslatorService
{
    Task<string> TranslateAsync(string text, string targetLanguageCode, CancellationToken cancellationToken = default);
    Task<string> DetectLanguageAsync(string text, CancellationToken cancellationToken = default);
}
