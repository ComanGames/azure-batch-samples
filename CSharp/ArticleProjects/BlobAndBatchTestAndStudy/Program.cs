using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudStorageManager;
using Ionic.Zip;
using Microsoft.Azure.Batch.Protocol.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ResourceFile = Microsoft.Azure.Batch.ResourceFile;

namespace BlobAndBatchTestAndStudy
{
    class Program
    {
        private const string BatchAccountName = "coman";
        private const string BatchAccountKey = "mpgIXIkoJdBUoHlqVAKs+QbXJzN9rZTPhfDG6kuoRaVyDZouYDE5WRZjmFUQdHir6fAhxezLz5OfWcUNR40PoA==";
        private const string BatchAccountUrl = "https://coman.westeurope.batch.azure.com";

        // Storage account credentials
        private const string StorageAccountName = "coman";
        private const string StorageAccountKey = "h4L5PobVYAyt0hkzYDpy2fvm9pwznVIwxNWyZwj2YU77HM4V5kKraTRIdiZ3UMVDctUbprCcBK28ZUXEnbNbJQ==";
        private const string TempFolder = @"C:\tmp"; 
        private const string ZipFile = @"C:\Users\coman\Desktop\Blender.zip"; 

        static  void Main(string[] args)
        {
//            UplodingFolderToBlob();
//            ZipUtilitesBasics();
            DebugInfo.ListenLog(Console.WriteLine);
//            TransferUtilities.UploadFileToBlobAsync(@"C:\Users\coman\Desktop\Blender.zip", "temp", "Blender.zip").Wait();
//            ResourceFile rf = TransferUtilities.GetResourceFileFromBlobAsync("temp", "Blender.zip").Result;
//            BatchUtilities.CreateResourcePoolAsync(new List<ResourceFile>(new[] {rf})).Wait();
            BatchUtilities.CreateResourceJobAsync().Wait();
            Console.ReadKey();
        }

        private static void ZipUtilitesBasics()
        {
            DirectoryInfo di = Directory.CreateDirectory(TempFolder + $@"\{Path.GetFileNameWithoutExtension(ZipFile)}_temp");
            Console.WriteLine($"Folder created {di.FullName}");
            ZipFile zip = Ionic.Zip.ZipFile.Read(ZipFile);
            Console.WriteLine($"Start Executing");
            zip.SaveProgress += ProgressOfZip;
            zip.StatusMessageTextWriter = System.Console.Out;
            zip.ExtractAll(di.FullName, ExtractExistingFileAction.OverwriteSilently);
            Console.WriteLine($"Start Executing");
        }

        private static void ProgressOfZip(object sender, SaveProgressEventArgs e)
        {

            Console.Clear();
            Console.WriteLine($"Done {(int) ((e.BytesTransferred*100)/e.TotalBytesToTransfer)} %");
        }

        private static void UplodingFolderToBlob()
        {
            CloudBlobClient client = CreateBlobClient();
            CloudBlobContainer container = client.GetContainerReference("test");
            container.CreateIfNotExists();
            Console.WriteLine("We created continer test");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(@"Blender\BlenderFile");
            Console.WriteLine("Start uploding file");
            using (var fileStream = System.IO.File.OpenRead(@"C:\tmp\blender-2.73-windows64\Blender.exe"))
            {
                blockBlob.UploadFromStream(fileStream);
            }
            Console.WriteLine("Done Uploading file");
        }

        private static async Task CreatingClient()
        {
        }

        private static CloudBlobClient CreateBlobClient()
        {
            string storageConnectionString = $"DefaultEndpointsProtocol=https;AccountName={StorageAccountName};AccountKey={StorageAccountKey}";
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            return blobClient;
        }

    }
}
