using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Reflection;
using System.IO;

namespace BlobStorageTest
{
    [TestClass]
    public class BlobStorageTest
    {
        #region fields
        static CloudStorageAccount storageAccount;
        static CloudBlobClient blobClient;
        static CloudBlobContainer container;
        const string BlobName = "myblob";
        const string ContainerName = "MyContainer";
        const string ResourceFileName = "BlobStorageTest.SampleDataFile.txt";

        #endregion

        #region initializers

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            storageAccount = CloudStorageAccount.Parse(AppSetting.StorageConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();

            container = blobClient.GetContainerReference(ContainerName);

            container.CreateIfNotExists();
        }
        [ClassCleanup]
        public static void ClassCleanup()
        {
            if (container != null)
            {
                container.Delete();
            }
        }

        #endregion

        #region tests

        [TestMethod]
        public void Upload_File_ToBlockBlob_VerifyUploadedContent()
        {
            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);

            string writtenContent = string.Empty;
            using (var fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    writtenContent = reader.ReadToEnd();
                }
            }

            blockBlob.UploadText(writtenContent);
            
            var readContent = blockBlob.DownloadText();

            Assert.IsTrue(readContent == writtenContent, "Read blob text is not same as what was written.");
        }

        #endregion
    }
}
