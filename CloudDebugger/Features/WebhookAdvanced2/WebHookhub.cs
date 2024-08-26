using Microsoft.AspNetCore.SignalR;

namespace CloudDebugger.Features.WebhookAdvanced;

public class WebHookHub : Hub
{
    public async Task SendMessage(string destionationBox, string color, string message)
    {
        await Clients.All.SendAsync(destionationBox, color, message);
    }
}

