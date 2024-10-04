using Microsoft.AspNetCore.SignalR;

namespace CloudDebugger.Features.WebHooks;

/// <summary>
/// This is the SignalR webhook hub that will send messages to the browser.
/// </summary>
public class WebHookHub : Hub
{
    /// <summary>
    /// Valid messages are:
    /// 
    /// ReceiveMessage1
    /// ReceiveMessage2
    /// ReceiveMessage3
    /// ReceiveMessage4
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="color">Color name, like red, blue, green, black</param>
    /// <param name="content">The content to send to the browser</param>
    /// <returns></returns>
    public async Task SendMessage(string message, string color, string content)
    {
        await Clients.All.SendAsync(message, color, content);
    }
}

