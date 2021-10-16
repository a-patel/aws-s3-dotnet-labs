#region Imports
using System;
using System.Collections.Generic;
#endregion

namespace AWSS3Labs.Web.Models
{
    public class FileMetadata
    {
        public string ContentType { get; set; }

        public string ContentMD5 { get; set; }

        public string ContentDisposition { get; set; }

        public string ETag { get; set; }

        public long Length { get; set; }


        public DateTime CreatedOn { get; set; }

        public DateTime LastModifiedOn { get; set; }

        public string Name { get; set; }

        public string BucketName { get; set; }

        public string Url { get; set; }

        public string Path { get; set; }


        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
