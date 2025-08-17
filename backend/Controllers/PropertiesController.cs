using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PropertiesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        //  Tüm ilanları filtrele
        [HttpGet]
        public IActionResult GetAll(
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? location,
            [FromQuery] string? title
        )
        {
            var query = _context.Properties.AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);
            if (!string.IsNullOrEmpty(location))
                query = query.Where(p => p.Location.ToLower().Contains(location.ToLower()));
            if (!string.IsNullOrEmpty(title))
                query = query.Where(p => p.Title.ToLower().Contains(title.ToLower()));

            return Ok(query.ToList());
        }

        //  İlanı ID’ye göre getir
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var property = _context.Properties.Find(id);
            if (property == null)
                return NotFound();

            return Ok(property);
        }

        //  İlan oluştur (çoklu dosya destekli)
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] PropertyDto newProperty)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized();

                var userId = int.Parse(userIdClaim.Value);
                var imageUrls = new List<string>();

                if (newProperty.ImageFiles != null && newProperty.ImageFiles.Any())
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    foreach (var imageFile in newProperty.ImageFiles)
                    {
                        if (imageFile.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                            var filePath = Path.Combine(uploadsFolder, fileName);

                            using var stream = new FileStream(filePath, FileMode.Create);
                            await imageFile.CopyToAsync(stream);

                            imageUrls.Add("/uploads/" + fileName);
                        }
                    }
                }

                var property = new Property
                {
                    Title = newProperty.Title,
                    Description = newProperty.Description,
                    Price = newProperty.Price,
                    Location = newProperty.Location ?? "",
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow, //  Utc olarak düzeltildi
                    ImageUrls = string.Join(",", imageUrls)
                };

                _context.Properties.Add(property);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = property.Id }, property);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "Inner exception yok";
                return StatusCode(500, new
                {
                    message = "Sunucu hatası: " + ex.Message,
                    innerException = innerMessage,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // İlan güncelle
        [HttpPut("{id}")]
        [Authorize]
        public IActionResult Update(int id, [FromBody] PropertyDto updatedProperty)
        {
            var property = _context.Properties.Find(id);
            if (property == null)
                return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || property.UserId != int.Parse(userIdClaim.Value))
                return Forbid();

            property.Title = updatedProperty.Title;
            property.Description = updatedProperty.Description;
            property.Price = updatedProperty.Price;
            property.Location = updatedProperty.Location ?? "";

            _context.SaveChanges();
            return Ok(property);
        }

        // İlan sil
        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var property = _context.Properties.Find(id);
            if (property == null)
                return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || property.UserId != int.Parse(userIdClaim.Value))
                return Forbid();

            _context.Properties.Remove(property);
            _context.SaveChanges();

            return NoContent();
        }

        //  Giriş yapan kullanıcının ilanlarını getir
        [HttpGet("my")]
        [Authorize]
        public IActionResult GetMyProperties()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            var myProperties = _context.Properties.Where(p => p.UserId == userId).ToList();

            return Ok(myProperties);
        }
    }
}
