using System;
using System.Threading.Tasks;
using Ionic.Zip;

namespace CloudStorageManager
{
    public static class ZipUtilities
    {

        public static async Task UnzipAsync(string zipFilePath, string outPutDirectoryPath)
        {
            await Task.Factory.StartNew( Unzip(zipFilePath, outPutDirectoryPath) );
        }
        public static async Task ZipAsync(string zipDirectoryPath, string zipFilePath)
        {
            await Task.Factory.StartNew( Zip(zipDirectoryPath, zipFilePath) );
        }

        private static Action Zip(string zipDirectoryPath, string zipFilePath)
        {
            return () =>
            {
                using (ZipFile zip = new ZipFile())
                {
                    // add this map file into the "images" directory in the zip archive
                    zip.AddDirectory(zipDirectoryPath);
                    zip.Save(zipFilePath);
                }
            };

        }

        private static Action Unzip(string zipFilePath, string outPutDirectoryPath)
        {
            return () => {
                ZipFile zipFile = ZipFile.Read(zipFilePath);
                zipFile.ExtractAll(outPutDirectoryPath, ExtractExistingFileAction.OverwriteSilently);
            };
        }
    }
}
