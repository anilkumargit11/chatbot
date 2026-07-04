using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AgenticKnowledgeAssistant.API.Hubs;

public sealed class AssistantHub : Hub
{
    public async Task JoinSession(string sessionGuid)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionGuid);
    }

    public async Task LeaveSession(string sessionGuid)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionGuid);
    }
}
