using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketWebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebSocketController : ControllerBase
    {
        private static Dictionary<WebSocket, int> _sockets = []; // To keep track of all connected WebSockets

        private static List<MessageStatus> _rows = new List<MessageStatus>
            {
                new() { id = 4, status = "Active" },
                new() { id = 2, status = "Pending" },
                new() { id = 1, status = "Pending" },
                new() { id = 1, status = "Active" },
                new() { id = 4, status = "Pending" },
                new() { id = 3, status = "Pending" },
                new() { id = 2, status = "Active" },
                new() { id = 3, status = "Active" },
            };

        private int requestCount = 0;

        [HttpGet("ws")]
        public async Task Get()
        {
            // Check if the request is a WebSocket request
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                // Accept the WebSocket connection
                WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _sockets.Add(webSocket,0);
                //await SendMessagesToClient(webSocket);

                await KeepConnectionAlive(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }
        // HTTP POST endpoint to receive data updates and broadcast to all connected clients
        [HttpPost("update-row")]
        public IActionResult UpdateRow([FromBody] MessageStatus updatedRow)
        {
            // Find and update the row with the matching ID
            if (updatedRow != null)
            {
                // Broadcast the updated row to all connected WebSocket clients
                BroadcastUpdateToClients(updatedRow);

                return Ok(new { message = "Row updated successfully" });
            }

            return NotFound(new { message = "Row not found" });
        }

        // Method to keep the WebSocket connection open
        private async Task KeepConnectionAlive(WebSocket webSocket)
        {
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {
                // Wait for a message from the client (or just keep the connection alive)
                await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
        private async Task SendMessagesToClient(WebSocket webSocket)
        {
            // Simulated data (representing rows)


            // Iterate over the rows and send a message for each
            foreach (var row in _rows)
            {
                // Simulate some delay
                await Task.Delay(5000); // Adjust the delay as per your need

                // Send the message to the client
                var messageJson = JsonConvert.SerializeObject(row);
                var buffer = Encoding.UTF8.GetBytes(messageJson);

                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            //// Optionally close the WebSocket connection after sending all the messages
            //await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Completed", CancellationToken.None);
        }
        // Broadcast update to all WebSocket clients
        private async void BroadcastUpdateToClients(MessageStatus updatedRow)
        {
            var message = new
            {
                id = updatedRow.id,
                status = updatedRow.status
            };

            var messageJson = JsonConvert.SerializeObject(message);
            var buffer = Encoding.UTF8.GetBytes(messageJson);

            var sockets = _sockets;

            // Send the update to all connected WebSocket clients
            foreach (var webSocket in sockets)
            {
                if (webSocket.Key.State == WebSocketState.Open)
                {
                    var count = webSocket.Value;

                    if (count > 4)
                    {
                        await webSocket.Key.CloseAsync(WebSocketCloseStatus.NormalClosure, "Completed", CancellationToken.None);
                    }
                    else
                    {
                        await webSocket.Key.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

                        _sockets[webSocket.Key] = count + 1;
                    }
                }
                else
                {
                    _sockets.Remove(webSocket.Key);
                }
            }
        }
    }
    public class MessageStatus
    {
        public int id { get; set; }
        public string status { get; set; }
    }
}
