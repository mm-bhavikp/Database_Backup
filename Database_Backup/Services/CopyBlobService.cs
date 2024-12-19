using Azure.Storage.Blobs;

namespace Database_Backup.Services
{
    public interface ICopyBlobService
    {
        Task<string> CopyBlobInOtherBucket(string sourceContainer, string destinationContainer);
    }
    public class CopyBlobService : ICopyBlobService
    {
        private readonly string _sourceConnectionString = "source azure connection string";
        private readonly string _targetConnectionString = "target azure connection string";
        
        public async Task<string> CopyBlobInOtherBucket(string sourceContainer, string destinationContainer)
        {

            try
            {
                var sourceContainerClient = new BlobContainerClient(_sourceConnectionString, sourceContainer);
                var destinationContainerClient = new BlobContainerClient(_sourceConnectionString, destinationContainer);

                var sourceBlob = sourceContainerClient.GetBlobs();

                foreach(var blobs in sourceBlob)
                {
                    var sourceBlobClient = sourceContainerClient.GetBlobClient(blobs.Name);
                    var destinationBlobClient = destinationContainerClient.GetBlobClient(blobs.Name);
                    var sourceBlobUri = new Uri($"{sourceBlobClient.Uri.AbsoluteUri}");
                    await destinationBlobClient.StartCopyFromUriAsync(sourceBlobUri);
                }
                await CopyAcrossAccount(sourceContainer);

                return "Copied Successfully";
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        private async Task CopyAcrossAccount(string sourceContainer)
        {
            var sourceAccountName = "source account name";
            var sourceAccountKey = "source account key";

            Azure.Storage.StorageSharedKeyCredential sourceCredential = new Azure.Storage.StorageSharedKeyCredential(sourceAccountName, sourceAccountKey);

            var targetContainer = "target azure connection container name";

            var sourceBlobContainerClient = new BlobContainerClient(_sourceConnectionString, sourceContainer);
            var targetBlobContainerClient = new BlobContainerClient(_targetConnectionString, targetContainer);
            var sourceBlobs = sourceBlobContainerClient.GetBlobs();

            foreach(var blob in sourceBlobs)
            {
                Azure.Storage.Sas.BlobSasBuilder blobSasBuilder = new Azure.Storage.Sas.BlobSasBuilder()
                {
                    BlobContainerName = sourceContainer,
                    BlobName = blob.Name,
                    ExpiresOn = DateTime.Now.AddDays(3)
                };
                blobSasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);
                var sasToken = blobSasBuilder.ToSasQueryParameters(sourceCredential).ToString();
                var sourceBlobClient = sourceBlobContainerClient.GetBlobClient(blob.Name);
                var targetBlobClient = targetBlobContainerClient.GetBlobClient(blob.Name);
                var sourceBlobUri = new Uri($"{sourceBlobClient.Uri.AbsoluteUri}?{sasToken}");
                await targetBlobClient.StartCopyFromUriAsync(sourceBlobUri);
            }
        }
    }
}
