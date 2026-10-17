using System.Text.RegularExpressions;

namespace TruthEye.Services
{
    public interface IEvidenceStore
    {
        Task SaveAsync(string name, Stream content, CancellationToken ct = default);
        Task<Stream?> OpenReadAsync(string name, CancellationToken ct = default);
        Task DeleteAsync(string name, CancellationToken ct = default);
    }

    public static class EvidenceName
    {
        private static readonly Regex Pattern = new(@"^[a-f0-9]{32}\.jpg$", RegexOptions.Compiled);

        public static bool IsValid(string? name) => name != null && Pattern.IsMatch(name);

        public static void Require(string? name)
        {
            if (!IsValid(name)) throw new ArgumentException("Invalid evidence name");
        }
    }

    // Files on disk. Point Evidence:Path at a persistent folder (a mounted volume) in production.
    public class LocalEvidenceStore : IEvidenceStore
    {
        private readonly string _root;

        public LocalEvidenceStore(IConfiguration cfg, IWebHostEnvironment env)
        {
            var configured = cfg["Evidence:Path"];
            _root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(env.ContentRootPath, "App_Data", "evidence")
                : configured;
            Directory.CreateDirectory(_root);
        }

        public async Task SaveAsync(string name, Stream content, CancellationToken ct = default)
        {
            EvidenceName.Require(name);
            await using var fs = new FileStream(Path.Combine(_root, name), FileMode.CreateNew, FileAccess.Write);
            await content.CopyToAsync(fs, ct);
        }

        public Task<Stream?> OpenReadAsync(string name, CancellationToken ct = default)
        {
            EvidenceName.Require(name);
            var path = Path.Combine(_root, name);
            if (!System.IO.File.Exists(path)) return Task.FromResult<Stream?>(null);
            return Task.FromResult<Stream?>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true));
        }

        public Task DeleteAsync(string name, CancellationToken ct = default)
        {
            EvidenceName.Require(name);
            var path = Path.Combine(_root, name);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            return Task.CompletedTask;
        }
    }
}