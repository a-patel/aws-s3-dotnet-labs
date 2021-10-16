namespace AWSS3Labs.Web.Configuration
{
    public class AmazonS3Config
    {
        public string BucketName { get; set; }

        public string ServerSideEncryptionMethod { get; set; }

        public string ServiceUrl { get; set; }



        // http://docs.amazonwebservices.com/AmazonS3/latest/BucketConfiguration.html#LocationSelection
        public string Region { get; set; }

        public string AccessKeyId { get; set; }

        public string SecretAccessKey { get; set; }

        public string Profile { get; set; }
    }
}