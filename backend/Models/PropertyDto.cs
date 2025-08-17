using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

public class PropertyDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Location { get; set; }
    public int UserId { get; set; }

    // Çoklu resim için:
    public List<IFormFile>? ImageFiles { get; set; }
}
