using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TruthEye.Entities;
using TruthEye.UnitOfWorkk;

namespace TruthEye.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private const long MaxBytes = 2 * 1024 * 1024;
        private const string UrlPrefix = "/uploads/profiles/";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(IUnitOfWork unitOfWork, IWebHostEnvironment env, ILogger<ProfileController> logger)
        {
            _unitOfWork = unitOfWork;
            _env = env;
            _logger = logger;
        }

        private int CurrentUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private string Folder()
        {
            var root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var dir = Path.Combine(root, "uploads", "profiles");
            Directory.CreateDirectory(dir);
            return dir;
        }

        // Decide the type from the file's first bytes, never from its name
        private static string? DetectExtension(byte[] h, int n)
        {
            if (n >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
            if (n >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return ".png";
            if (n >= 12 && h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                        && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50) return ".webp";
            return null;
        }

        private void DeleteOld(string? path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith(UrlPrefix)) return;
            var file = Path.Combine(Folder(), Path.GetFileName(path));
            if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
        }

        [HttpPost("image")]
        [RequestSizeLimit(3_000_000)]
        public async Task<IActionResult> Upload(IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, error = "No file was uploaded" });
                if (file.Length > MaxBytes)
                    return BadRequest(new { success = false, error = "Image must be 2MB or smaller" });

                var header = new byte[12];
                int read;
                await using (var probe = file.OpenReadStream())
                    read = await probe.ReadAsync(header, 0, header.Length);

                var ext = DetectExtension(header, read);
                if (ext == null)
                    return BadRequest(new { success = false, error = "Only JPG, PNG or WebP images are allowed" });

                var user = await _unitOfWork.Repository<User>().GetByIdAsync(CurrentUserId());
                if (user == null) return NotFound(new { success = false, error = "User not found" });

                var name = $"{Guid.NewGuid():N}{ext}";
                await using (var fs = new FileStream(Path.Combine(Folder(), name), FileMode.CreateNew))
                    await file.CopyToAsync(fs);

                DeleteOld(user.ProfileImage);

                user.ProfileImage = UrlPrefix + name;
                await _unitOfWork.Repository<User>().UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, data = new { profileImage = user.ProfileImage } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile image upload failed");
                return BadRequest(new { success = false, error = "Couldn't save the image" });
            }
        }

        [HttpDelete("image")]
        public async Task<IActionResult> Remove()
        {
            try
            {
                var user = await _unitOfWork.Repository<User>().GetByIdAsync(CurrentUserId());
                if (user == null) return NotFound(new { success = false, error = "User not found" });

                DeleteOld(user.ProfileImage);
                user.ProfileImage = "default.jpg";
                await _unitOfWork.Repository<User>().UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();

                return Ok(new { success = true, data = new { profileImage = user.ProfileImage } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile image removal failed");
                return BadRequest(new { success = false, error = "Couldn't remove the image" });
            }
        }
    }
}