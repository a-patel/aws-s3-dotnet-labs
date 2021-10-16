namespace AWSS3Labs.Web.Configuration
{
    public partial class AmazonS3Defaults
    {
        public static string SettingsSection = "AmazonS3Config";

        public static string DefaultServiceUrl = "https://s3.amazonaws.com";

        public static string ServerSideEncryptionMethod = Amazon.S3.ServerSideEncryptionMethod.AES256;
    }
}
