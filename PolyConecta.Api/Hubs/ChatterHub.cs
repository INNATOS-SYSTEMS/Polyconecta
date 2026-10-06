using Microsoft.AspNetCore.SignalR;

// Copia del hub del prototipo (PolyConecta.Presentation/Hubs/ChatterHub.cs), que no se toca (D-58, D-60).

namespace PolyConecta.Api.Hubs;

public class ChatterHub : Hub
{
    public async Task SendMessage(string documentId, string author, string text)
    {
        await Clients.All.SendAsync("ReceiveChatterMessage", documentId, author, text, DateTime.UtcNow.ToString("g"));
    }
}
