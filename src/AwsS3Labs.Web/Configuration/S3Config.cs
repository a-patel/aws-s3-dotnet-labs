#region Imports
#endregion

namespace AwsS3Labs.Web.Configuration
{
    public class S3Config
    {
        public string BucketName { get; set; }

        public string AWSRegion { get; set; }

        public string AWSAccessKey { get; set; }

        public string AWSSecretKey { get; set; }
    }
}
