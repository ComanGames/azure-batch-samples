using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Azure.Batch;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudStorageManager
{
    public static class TransferUtilities
    {
        private static CloudStorageAccount _storageAccount;
        private static CloudBlobClient _storageClient;
        private static ConcurrentDictionary<string, CloudBlobContainer> _storageContainers;
        /// <summary>
        /// Gets a CloudStorageAccount
        /// </summary>
        public static CloudStorageAccount StorageAccount
        {
            get
            {
                if (_storageAccount == null)
                {
                    string strAccount = "coman";
                    string strKey = "h4L5PobVYAyt0hkzYDpy2fvm9pwznVIwxNWyZwj2YU77HM4V5kKraTRIdiZ3UMVDctUbprCcBK28ZUXEnbNbJQ==";

                    StorageCredentials credential = new StorageCredentials(strAccount, strKey);
                    _storageAccount = new CloudStorageAccount(credential, true);
                }
                return _storageAccount;
            }
        }

        public static CloudBlobClient StorageClient 
        {
            get
            {
                if (_storageClient == null)
                    _storageClient = StorageAccount.CreateCloudBlobClient();
                return _storageClient;
            }
        }
        public static CloudBlobContainer GetBlobConatiner(string blobName)
        {
            if (_storageContainers == null)
               _storageContainers = new ConcurrentDictionary<string, CloudBlobContainer>();
            if (!_storageContainers.ContainsKey(blobName))
            {
                CloudBlobClient blobClient = StorageClient;
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(blobName);

                try
                {
                    if (blobContainer.CreateIfNotExists())
                    {
                        DebugInfo.Log($" We create blob conatiner {blobName}");
                    }
                    else
                    {
                        DebugInfo.Log($" Blob conatiner {blobName} already exist");
                    }
                }
                catch (Exception e)
                {
                    DebugInfo.Log(e.Message);
                }
                blobContainer.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                if (blobName != null)
                    _storageContainers.TryAdd(blobName, blobContainer);
                return blobContainer;
            }

            CloudBlobContainer result;
            _storageContainers.TryGetValue(blobName, out result);
            return result;

        }

        public static async Task UploadFileToBlobAsync(string filePath, string strContainerName,string blobName)
        {

                DebugInfo.Log($"Uploading File{filePath} to blob {blobName}");
             await Task.Factory.StartNew(UploadFileBlobAction(filePath, strContainerName, blobName) );
                DebugInfo.Log($"Uploading File{filePath} to blob {blobName}");
        }

        public static async Task<ResourceFile> GetResourceFileFromBlobAsync(string strContainerName, string blobName,SharedAccessBlobPermissions permissions= SharedAccessBlobPermissions.Read,int time=2)
        {
            DebugInfo.Log($"Start getting permissiong Continer={strContainerName} Blob={blobName} permission={permissions} time ={time}");
            ResourceFile result = await  Task<ResourceFile>.Factory.StartNew(() =>
            {
                SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
                {
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(time),
                    Permissions = permissions
                };

                // Construct the SAS URL for blob
                CloudBlockBlob blobData = GetCloudBlockBlob(strContainerName, blobName);
                string sasBlobToken = blobData.GetSharedAccessSignature(sasConstraints);
                string blobSasUri = String.Format("{0}{1}", blobData.Uri, sasBlobToken);

                return new ResourceFile(blobSasUri, blobName);

            });
            DebugInfo.Log($"Done getting permissiong Continer={strContainerName} Blob={blobName} permission={permissions} time ={time}");
            return result;

        }
        public static DirectoryInfo rootDir;
        public static async Task UploadFolderAsync(string di, string strContainerName)
        {
           DebugInfo.Log($"Start Uploding Folder {di}");
            DirectoryInfo dirInfo = new DirectoryInfo(di);
            rootDir = new DirectoryInfo(di);
            await ListAllFilesAsync(dirInfo, strContainerName);
            DebugInfo.Log($"Done Uploding Folder {di}") ;

        }
        private static async Task ListAllFilesAsync(DirectoryInfo dirInfo, string strContainerName)
        {
            try
            {
                DebugInfo.Log($"Start Uploding Files to the {strContainerName} blob ");
                foreach (var file in dirInfo.GetFileSystemInfos())
                {
                    if (file is FileInfo)
                    {
                        FileInfo fileInfo = (FileInfo)file;
                        await Task.Factory.StartNew(()=>UploadFileToBlobAsync(fileInfo.FullName, strContainerName,"some"));

                    }
                    else
                    {
                        DirectoryInfo newinfo = (DirectoryInfo)file;
                        await ListAllFilesAsync(newinfo, strContainerName);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static Action UploadFileBlobAction(string filePath, string strContainerName, string blobName)
        {
            return () =>
            {
                try
                {
                    //Generates  a blobName
                    FileInfo fileInfo = new FileInfo(filePath);
                    string strPath = fileInfo.FullName;
                    if (!string.IsNullOrEmpty(blobName))
                    {

                        CloudBlockBlob blob = GetCloudBlockBlob(strContainerName, blobName);
                        //upload files
                        DebugInfo.Log("Uplod to server just started");
                        Timer timer = new Timer(10000);
                        timer.Elapsed += (sender, args) => {DebugInfo.Log($"Loading... {args.SignalTime}"); };
                        timer.Start();
                        using (FileStream stream = fileInfo.OpenRead())
                        {
                            blob.UploadFromStream(stream);
                            // Upload the file
                        }
                        timer.Stop();
                        DebugInfo.Log($"Uploaded File{fileInfo.FullName} to blob {blobName}");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            };
        }

        private static CloudBlockBlob GetCloudBlockBlob(string strContainerName, string blobName)
        {
            CloudBlockBlob blob = GetBlobConatiner(strContainerName).GetBlockBlobReference(blobName);
            return blob;
        }
    }
}
