#region Imports
using Amazon.S3.Model;
using AWSS3Labs.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
#endregion

namespace AWSS3Labs.Web
{
    public static class AmazonS3Extensions
    {
        internal static bool IsSuccessful(this HttpStatusCode code)
        {
            return (int)code < 400;
        }

        internal static FileMetadata ToFileInfo(this S3Object blob)
        {
            if (blob == null)
                return null;

            return new FileMetadata
            {
                Path = blob.Key,
                BucketName = blob.BucketName,
                Length = blob.Size,
                LastModifiedOn = blob.LastModified.ToUniversalTime(),
                CreatedOn = blob.LastModified.ToUniversalTime(), // TODO: Need to fix this
                ETag = blob.ETag,
            };
        }

        internal static IEnumerable<S3Object> MatchesPattern(this IEnumerable<S3Object> blobs, Regex patternRegex)
        {
            return blobs.Where(blob => patternRegex == null || patternRegex.IsMatch(blob.ToFileInfo().Path));
        }

        public static IDictionary<string, string> ToMetadata(this MetadataCollection metadata)
        {
            return metadata.Keys.ToDictionary(k => k.Replace("x-amz-meta-", string.Empty), k => metadata[k]);
        }

        public static void AddMetadata(this MetadataCollection metadata, IDictionary<string, string> meta)
        {
            if (meta == null)
            {
                return;
            }

            foreach (var kvp in meta)
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }
    }
}
