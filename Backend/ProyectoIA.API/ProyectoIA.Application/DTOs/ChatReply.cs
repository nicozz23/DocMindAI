using System.Collections.Generic;

namespace ProyectoIA.Application.DTOs;

public class ChatReply
{
    public string Response { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
}
