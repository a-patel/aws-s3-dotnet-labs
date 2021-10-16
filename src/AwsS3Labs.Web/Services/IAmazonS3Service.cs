using AWSS3Labs.Web.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AWSS3Labs.Web.Services
{
    public interface IAmazonS3Service
    {
        Task<bool> Delete(string key, CancellationToken cancellationToken = default);

        Task<bool> Exist(string key, CancellationToken cancellationToken = default);

        Task<FileMetadata> GetMetadata(string key, CancellationToken cancellationToken = default);

        Task<string> GetSignedUrl(string key, TimeSpan expiresIn, string method = "GET");

        Task<Stream> GetStream(string key, CancellationToken cancellationToken = default);

        Task<string> GetUrl(string key);

        Task<System.Collections.Generic.List<FileMetadata>> List(string prefix = null);

        Task<bool> Upload(string key, Stream stream, CancellationToken cancellationToken = default);
    }
}