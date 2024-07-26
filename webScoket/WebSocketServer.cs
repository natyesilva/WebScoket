using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

class WebSocketServer
{
    private const int BufferSize = 1024;

    public static async Task Main(string[] args)
    {
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:65432/");
        httpListener.Start();
        Console.WriteLine("Server listening on http://localhost:65432/");

        while (true)
        {
            HttpListenerContext context = await httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                await HandleWebSocketConnection(webSocketContext.WebSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private static async Task HandleWebSocketConnection(WebSocket webSocket)
    {
        byte[] buffer = new byte[BufferSize];

        // Receive the file name
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        string fileName = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"Receiving file: {fileName}");

        using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
        {
            // Receive file content
            while (webSocket.State == WebSocketState.Open)
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "File received", CancellationToken.None);
                    break;
                }
                await fileStream.WriteAsync(buffer, 0, result.Count);
            }
        }

        Console.WriteLine($"File {fileName} received successfully");
    }
}
