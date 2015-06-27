using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Reflection;
using System.IO;
using System.Text;

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
        const string ContainerName = "mycontainer";
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
        public void Upload_Text_ToBlockBlob_VerifyUploadedContent()
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

            Assert.IsTrue(readContent == writtenContent, "Read blob text is not same as what was written as text.");
        }

        [TestMethod]
        public void Upload_Stream_ToBlockBlob_VerifyUploadedContent()
        {
            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);

            string writtenContent = string.Empty;
            using (var fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName))
            {
                blockBlob.UploadFromStream(fileStream);
                
            }
            using (var fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName))
            {
                using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    writtenContent = reader.ReadToEnd();
                }
            }
            // This does not work and returned string has extra BOM character at the beginning
            // http://stackoverflow.com/questions/11231147/cloudblob-downloadtext-method-inserts-additional-character
            //var readContent = blockBlob.DownloadText(Encoding.UTF8);

            string readContent;
            using (var memoryStream = new MemoryStream())
            {
                blockBlob.DownloadToStream(memoryStream);
                memoryStream.Position = 0;
                using (var reader = new StreamReader(memoryStream, Encoding.UTF8))
                {
                    readContent = reader.ReadToEnd();
                }
            }


            Assert.IsTrue(readContent == writtenContent, "Read blob text is not same as what was written using stream.");
        }


        #endregion
    }
}
