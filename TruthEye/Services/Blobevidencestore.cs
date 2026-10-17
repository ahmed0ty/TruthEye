// OPTIONAL: only add this file after running:  dotnet add package Azure.Storage.Blobs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace TruthEye.Services
{
    // Private Azure Blob container. Files are never reachable by URL; they are streamed through the API after a role check.
    public class BlobEvidenceStore : IEvidenceStore
    {
        private readonly BlobContainerClient _container;

        public BlobEvidenceStore(IConfiguration cfg)
        {
            var connection = cfg["Evidence:Blob:ConnectionString"]
                ?? throw new InvalidOperationException("Evidence:Blob:ConnectionString is not set");
            var name = cfg["Evidence:Blob:Container"] ?? "evidence";

            _container = new BlobContainerClient(connection, name);
            _container.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task SaveAsync(string name, Stream content, CancellationToken ct = default)
        {
            EvidenceName.Require(name);
            await _container.GetBlobClient(name).UploadAsync(content, overwrite: false, cancellationToken: ct);
        }

        public async Task<Stream?> OpenReadAsync(string name, CancellationToken ct = default)
        {
            EvidenceName.Require(name);
            var blob = _container.GetBlobClient(name);
            if (!(await blob.ExistsAsync(ct)).Value) return null;
            return await blob.OpenReadAsync(cancellationToken: ct);
        }

        public async Task DeleteAsync(string name, CancellationToken ct = default)
        {
            EvidenceName.Require(name);
            await _container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: ct);
        }
    }
}