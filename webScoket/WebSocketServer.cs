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
        // Substitua serveIp pelo endereço IP do servidor na rede local
        
            string serverIp = "192.168.0.1";
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://{serverIp}:65432/");
            try
            {
                httpListener.Start();
                Console.WriteLine($"Listening on http://{serverIp}:65432/");

                while (true)
                {
                    HttpListenerContext context = await httpListener.GetContextAsync();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    string responseString = "Hello World!";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                    response.ContentLength64 = buffer.Length;
                    using (var output = response.OutputStream)
                    {
                        await output.WriteAsync(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (ObjectDisposedException ex)
            {
                Console.WriteLine($"ObjectDisposedException: {ex.Message}");
            }
            finally
            {
                httpListener.Stop();
                httpListener.Close();
            }
        
    }

    private static async Task HandleWebSocketConnection(WebSocket webSocket)
    {
        byte[] buffer = new byte[BufferSize];

        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        string fileName = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"Receiving file: {fileName}");

        using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
        {
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
