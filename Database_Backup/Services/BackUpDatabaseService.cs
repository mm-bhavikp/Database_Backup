using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Database_Backup.Services
{
    public interface IBackUpDatabaseService
    {
        void BackupAllUserDatabases();
    }
    public class BackupDatabaseService : IBackUpDatabaseService
    {

        private readonly string _connectionString = "database connection string";
        private readonly string _backupFolderFullPath = "https://{accountName}.blob.core.windows.net/{containerName}";
        private readonly string[] _systemDatabaseNames = { "master", "tempdb", "model", "msdb" };
        private readonly string _azureConnectionString = "azure connection string";
        private readonly string _containername = "container name";
        private readonly string _accountName = "account name";
        private readonly string _accountKey = "account key";
        public async void BackupAllUserDatabases()
        {
            string folderName = DateTime.Now.ToString("yyyy-MM-dd");
            //await CreateFolder(_azureConnectionString, _containername, folderName); 
            var databaseList = GetAllUserDatabases();
            foreach (string databaseName in databaseList)
            {
                BackupDatabase(databaseName, _containername, _accountName, _accountKey);
            }
        }

        public void BackupDatabase(string databaseName, string containerName, string accountName, string accountKey)
        {
            string fileName = BuildBackupPathWithFilename(databaseName);
            string folderName = DateTime.Now.ToString("yyyy-MM-dd");

            Azure.Storage.StorageSharedKeyCredential sourceCredential = new Azure.Storage.StorageSharedKeyCredential(accountName, accountKey);
            BlobSasBuilder blobSasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                BlobName = fileName,
                ExpiresOn = DateTime.Now.AddDays(3)
            };

            blobSasBuilder.SetPermissions(BlobSasPermissions.Create);
            blobSasBuilder.SetPermissions(BlobSasPermissions.Write);
            blobSasBuilder.SetPermissions(BlobSasPermissions.List);
            var sasToken = blobSasBuilder.ToSasQueryParameters(sourceCredential).ToString();

            //string filePath = _backupFolderFullPath + "/" + folderName +"/" +fileName + "?" + sasToken;
            string filePath = _backupFolderFullPath + "/" + fileName + "?" + sasToken;

            using (var connection = new SqlConnection(_connectionString))
            {
                var query = String.Format("BACKUP DATABASE [{0}] TO URL='{1}'", databaseName, filePath);

                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        private async Task CreateFolder(string connectionString, string containerName, string folderName)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var folderBlobClient = containerClient.GetBlobClient($"{folderName}/");
                using (var emptyStream = new MemoryStream())
                {
                    await folderBlobClient.UploadAsync(emptyStream, overwrite: true);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        private List<string> GetAllUserDatabases()
        {
            var databases = new List<String>();

            DataTable databasesTable;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                databasesTable = connection.GetSchema("Databases");
                connection.Close();
            }

            foreach (DataRow row in databasesTable.Rows)
            {
                string databaseName = row["database_name"].ToString();

                if (_systemDatabaseNames.Contains(databaseName))
                    continue;
                databases.Add(databaseName);
            }
            return databases;
        }

        private string BuildBackupPathWithFilename(string databaseName)
        {
            string fileName = string.Format("{0}-{1}.bak", databaseName, DateTime.Now.ToString("yyyy-MM-dd"));
            return fileName;
        }
    }
}
