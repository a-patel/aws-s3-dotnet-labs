#region Imports
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using AWSS3Labs.Web.Configuration;
using AWSS3Labs.Web.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#endregion

namespace AWSS3Labs.Web.Services
{
    public partial class AmazonS3Service : IAmazonS3Service
    {
        #region Members

        private readonly IAmazonS3 _client;

        private readonly AWSS3Labs.Web.Configuration.AmazonS3Config _config;

        private readonly string _bucketName;

        private readonly string _serverSideEncryptionMethod;

        private readonly string _serviceUrl;

        private readonly bool _useChunkEncoding;

        private readonly S3CannedACL _cannedAcl;

        #endregion

        #region Ctor

        public AmazonS3Service(AWSS3Labs.Web.Configuration.AmazonS3Config config)
        {
            _config = config;

            _bucketName = config.BucketName;
            //_serviceUrl = config.ServiceUrl;
            _serviceUrl = string.IsNullOrEmpty(config.ServiceUrl) ? AmazonS3Defaults.DefaultServiceUrl : config.ServiceUrl;
            _serverSideEncryptionMethod = string.IsNullOrEmpty(config.ServiceUrl) ? AmazonS3Defaults.ServerSideEncryptionMethod : config.ServerSideEncryptionMethod;
            _useChunkEncoding = true;
            _cannedAcl = S3CannedACL.Private;  // TODO: Convert it properly

            _client = GetAmazonS3Client(_config);
        }

        #endregion

        #region Methods

        public async Task<string> GetUrl(string key)
        {
            return $"{_serviceUrl}/{_bucketName}/{key}";
        }

        public async Task<string> GetSignedUrl(string key, TimeSpan expiresIn, string method = "GET")
        {
            var urlRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddSeconds(expiresIn.TotalSeconds),
                Verb = method.ToUpper() == "GET" ? HttpVerb.GET : HttpVerb.PUT,
                //ResponseHeaderOverrides = headers,
            };

            if (!string.IsNullOrEmpty(_serverSideEncryptionMethod))
            {
                urlRequest.ServerSideEncryptionMethod = _serverSideEncryptionMethod;
            }

            return _client.GetPreSignedURL(urlRequest);
        }

        public async Task<Stream> GetStream(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key.Replace('\\', '/')
            };

            var response = await _client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.HttpStatusCode.IsSuccessful())
                return null;

            return response.ResponseStream;
        }

        public async Task<FileMetadata> GetMetadata(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key.Replace('\\', '/')
            };

            var response = await _client.GetObjectMetadataAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.HttpStatusCode.IsSuccessful())
                return null;

            return new FileMetadata
            {
                Path = key,
                BucketName = _bucketName,
                Length = response.ContentLength,
                CreatedOn = response.LastModified.ToUniversalTime(),  // TODO: Need to fix this
                LastModifiedOn = response.LastModified.ToUniversalTime(),
                ETag = response.ETag,
                Metadata = response.Metadata.ToMetadata()
            };
        }

        public async Task<List<FileMetadata>> List(string prefix = null)
        {
            var descriptors = new List<FileMetadata>();

            var objectsRequest = new ListObjectsRequest
            {
                BucketName = _bucketName,
                Prefix = prefix,
                MaxKeys = 100000
            };

            do
            {
                var objectsResponse = await _client.ListObjectsAsync(objectsRequest);

                foreach (S3Object entry in objectsResponse.S3Objects)
                {
                    var objectMetaRequest = new GetObjectMetadataRequest
                    {
                        BucketName = _bucketName,
                        Key = entry.Key
                    };

                    var objectMetaResponse = await _client.GetObjectMetadataAsync(objectMetaRequest);

                    var objectAclRequest = new GetACLRequest
                    {
                        BucketName = _bucketName,
                        Key = entry.Key
                    };

                    var objectAclResponse = await _client.GetACLAsync(objectAclRequest);
                    var isPublic = objectAclResponse.AccessControlList.Grants.Any(x => x.Grantee.URI == "http://acs.amazonaws.com/groups/global/AllUsers");

                    descriptors.Add(new FileMetadata
                    {
                        Name = entry.Key,
                        BucketName = _bucketName,
                        Length = entry.Size,
                        ETag = entry.ETag,
                        ContentMD5 = entry.ETag,
                        ContentType = objectMetaResponse.Headers.ContentType,
                        LastModifiedOn = entry.LastModified,
                        //Security = isPublic ? FileSecurity.Public : FileSecurity.Private,
                        ContentDisposition = objectMetaResponse.Headers.ContentDisposition,
                        Metadata = objectMetaResponse.Metadata.ToMetadata(),
                    });
                }

                // If response is truncated, set the marker to get the next set of keys.
                if (objectsResponse.IsTruncated)
                {
                    objectsRequest.Marker = objectsResponse.NextMarker;
                }
                else
                {
                    objectsRequest = null;
                }
            } while (objectsRequest != null);

            return descriptors;
        }

        public async Task<bool> Exist(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var result = await GetMetadata(key, cancellationToken).ConfigureAwait(false);
            return result != null;
        }

        public async Task<bool> Upload(string key, Stream stream, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var request = new PutObjectRequest
            {
                CannedACL = _cannedAcl,
                BucketName = _bucketName,
                Key = key.Replace('\\', '/'),
                AutoResetStreamPosition = false,
                AutoCloseStream = !stream.CanSeek,
                InputStream = stream.CanSeek ? stream : AmazonS3Util.MakeStreamSeekable(stream),
                UseChunkEncoding = _useChunkEncoding,
            };

            var response = await _client.PutObjectAsync(request, cancellationToken).ConfigureAwait(false);
            return response.HttpStatusCode.IsSuccessful();
        }

        public async Task<bool> Delete(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key.Replace('\\', '/')
            };

            var response = await _client.DeleteObjectAsync(request, cancellationToken).ConfigureAwait(false);
            return response.HttpStatusCode.IsSuccessful();
        }

        #endregion

        #region Utilities

        private IAmazonS3 GetAmazonS3Client(AWSS3Labs.Web.Configuration.AmazonS3Config config)
        {
            var credentials = GetAwsCredentials(config);

            RegionEndpoint region = null;
            if (!string.IsNullOrEmpty(config.Region))
            {
                region = RegionEndpoint.GetBySystemName(config.Region);

                if (region.DisplayName == "Unknown")
                    region = FallbackRegionFactory.GetRegionEndpoint();
            }


            if (string.IsNullOrEmpty(_serviceUrl))
            {
                return new AmazonS3Client(credentials, region);
            }
            else
            {
                var s3Config = new Amazon.S3.AmazonS3Config
                {
                    ServiceURL = _serviceUrl,
                    RegionEndpoint = region
                };

                return new AmazonS3Client(credentials, s3Config);
            }
        }

        private AWSCredentials GetAwsCredentials(AWSS3Labs.Web.Configuration.AmazonS3Config config)
        {
            if (!string.IsNullOrWhiteSpace(config.Profile))
            {
                var credentialProfileStoreChain = new CredentialProfileStoreChain();

                if (credentialProfileStoreChain.TryGetAWSCredentials(config.Profile, out AWSCredentials profileCredentials))
                    return profileCredentials;
                else
                {
                    throw new AmazonClientException($"Failed to find AWS credentials for the profile {config.Profile}");
                }
            }

            if (!string.IsNullOrEmpty(config.AccessKeyId) && !string.IsNullOrWhiteSpace(config.SecretAccessKey))
            {
                return new BasicAWSCredentials(config.AccessKeyId, config.SecretAccessKey);
            }

            var credentials = FallbackCredentialsFactory.GetCredentials();
            if (credentials == null)
            {
                throw new AmazonClientException("Failed to find AWS Credentials for constructing AWS service client");
            }

            return credentials;
        }

        #endregion
    }
}
