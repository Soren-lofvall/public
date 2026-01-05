using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Enable WebSockets
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
};

app.UseWebSockets(webSocketOptions);

// WebSocket endpoint: ws://localhost:5000/ws
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        Console.WriteLine("WebSocket connected");

        // Send a welcome banner with ANSI colors
        await SendAnsi(webSocket, "\x1b[1;32mWelcome to the Blazor xterm.js server!\x1b[0m\r\n");
        await SendAnsi(webSocket, "\x1b[36mType something and it will echo back.\x1b[0m\r\n\r\n");

        var buffer = new byte[4096];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("WebSocket closing");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }

            var receivedText = Encoding.UTF8.GetString(buffer, 0, result.Count);

            // Simple echo with color
            var response = $"\r\n\x1b[33mYou typed:\x1b[0m {receivedText}";
            await SendAnsi(webSocket, response);
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});


app.Run();

// Helper to send text with ANSI sequences
static Task SendAnsi(WebSocket ws, string text)
{
    var bytes = Encoding.UTF8.GetBytes(text);
    return ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
}
