using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Azure.Batch.Protocol.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using AzureUtilites;

namespace BlenderRenderer
{
    public static class BackEndFunctionality
    {
        private const string BatchAccountName = "coman";
        private const string BatchAccountKey = "mpgIXIkoJdBUoHlqVAKs+QbXJzN9rZTPhfDG6kuoRaVyDZouYDE5WRZjmFUQdHir6fAhxezLz5OfWcUNR40PoA==";
        private const string BatchAccountUrl = "https://coman.westeurope.batch.azure.com";

        // Storage account credentials
        private const string StorageAccountName = "coman";
        private const string StorageAccountKey = "h4L5PobVYAyt0hkzYDpy2fvm9pwznVIwxNWyZwj2YU77HM4V5kKraTRIdiZ3UMVDctUbprCcBK28ZUXEnbNbJQ==";

        private const string PoolId = "DotNetTutorialPool";
        private const string JobId = "DotNetTutorialJob";

        const string appContainerName = "application";
        const string inputContainerName = "input";
        const string outputContainerName = "output";

        public static bool IsAllNullOrEmpty(params string[] strings)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                if (String.IsNullOrEmpty(strings[i]))
                {
                    return true;
                }
                
            }

            return false;
        }
        public static async void CreatingBlobs()
        {
            Form1.FormIns.UpdateStatus("Creating Blobs...");
             await CreateBlobs();
            Form1.FormIns.UpdateStatus("non connected...");
        }
        public static void Connect()
        {
            if (IsAllNullOrEmpty(BatchAccountName, BatchAccountKey, BatchAccountUrl,StorageAccountName,StorageAccountKey))
            {
                MessageBox.Show("One ore more account credential strings have not been populated. Please ensure that your Batch and Storage account credentials have been specified.");
                return;
            }
            TryToConnect().Wait();
        }
        private static async Task TryToConnect()
        {
            await CreateBlobs();
        }

        private static async Task CreateBlobs()
        {
            var blobClient = CreateBlobClient();

            await CreateContainerIfNotExistAsync(blobClient, appContainerName);
            await CreateContainerIfNotExistAsync(blobClient, inputContainerName);
            await CreateContainerIfNotExistAsync(blobClient, outputContainerName);
        }

        private static CloudBlobClient CreateBlobClient()
        {
            string storageConnectionString = $"DefaultEndpointsProtocol=https;AccountName={StorageAccountName};AccountKey={StorageAccountKey}";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            return blobClient;
        }

        private static async Task CreateContainerIfNotExistAsync(CloudBlobClient blobClient, string containerName)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            await container.CreateIfNotExistsAsync();
        }

        public static async void UploadApp(string fileName)
        {
            Form1.FormIns.UpdateStatus("Start Uploading");
            CloudUtilitesBlob unZip = new CloudUtilitesBlob();
            Form1.FormIns.UpdateStatus("Unziping Up");
            bool result = await unZip.UnZipFilesAsync(fileName, appContainerName,Debug.Log);
            Form1.FormIns.UpdateStatus("Uploding Done");
        }
        public static async void UploadFolder(string folderName)
        {
            Form1.FormIns.UpdateStatus("Start Uploading");
            CloudUtilitesBlob unZip = new CloudUtilitesBlob();
            Form1.FormIns.UpdateStatus("Unziping Up");
              await unZip.UploadFolderAsync(folderName, appContainerName, Debug.Log);
            Form1.FormIns.UpdateStatus("Uploding Done");
        }


        private static async Task<List<ResourceFile>> UploadFilesToContainerAsync(CloudBlobClient blobClient, string inputContainerName, List<string> filePaths)
        {
            List<ResourceFile> resourceFiles = new List<ResourceFile>();

            foreach (string filePath in filePaths)
            {
                resourceFiles.Add(await UploadFileToContainerAsync(blobClient, inputContainerName, filePath));
            }

            return resourceFiles;
        }
        private static async Task<ResourceFile> UploadFileToContainerAsync(CloudBlobClient blobClient, string containerName, string filePath)
        {
            Console.WriteLine("Uploading file {0} to container [{1}]...", filePath, containerName);

            string blobName = Path.GetFileName(filePath);

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blobData = container.GetBlockBlobReference(blobName);
            await blobData.UploadFromFileAsync(filePath, FileMode.Open);

            // Set the expiry time and permissions for the blob shared access signature. In this case, no start time is specified,
            // so the shared access signature becomes valid immediately
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Read
            };

            // Construct the SAS URL for blob
            string sasBlobToken = blobData.GetSharedAccessSignature(sasConstraints);
            string blobSasUri = String.Format("{0}{1}", blobData.Uri, sasBlobToken);

            return new ResourceFile(blobSasUri, blobName);
        }


    }
}