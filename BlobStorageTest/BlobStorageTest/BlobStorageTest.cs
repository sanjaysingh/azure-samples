﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Reflection;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Collections.Generic;

namespace BlobStorageTest
{
    [TestClass]
    public class BlobStorageTest
    {
        #region fields
        static CloudStorageAccount storageAccount;
        static CloudBlobClient blobClient;
        static CloudBlobContainer container;
        
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
        [TestInitialize]
        public void TestInitialize()
        {
           
        }

        [TestCleanup]
        public void TestCleanup()
        {
            
        }

        #endregion

        #region tests

        [TestMethod]
        public void Upload_Three_Blocks_Undo_One_Verify_Content()
        {
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetUniqueBlobName());
            
            string block1Id = GetBase64String("block1");
            var block1Data = Encoding.UTF8.GetBytes("First block of data");
            blockBlob.PutBlock(block1Id, new MemoryStream(block1Data), GetMD5Hash(block1Data));

            string block2Id = GetBase64String("block2");
            var block2Data = Encoding.UTF8.GetBytes("Second block of data");
            blockBlob.PutBlock(block2Id, new MemoryStream(block2Data), GetMD5Hash(block2Data));

            string block3Id = GetBase64String("block3");
            var block3Data = Encoding.UTF8.GetBytes("Third block of data");
            blockBlob.PutBlock(block3Id, new MemoryStream(block3Data), GetMD5Hash(block3Data));
            blockBlob.PutBlockList(new string[] {block1Id, block2Id });

            string readContent = blockBlob.DownloadText(Encoding.UTF8);
            
            Assert.AreEqual(readContent, Encoding.UTF8.GetString(block1Data) + Encoding.UTF8.GetString(block2Data), "The data downloaded from block blob does not correctly match expected one when one out of three was undone.");
        }

        [TestMethod]
        public void Upload_Multiple_Blobs_VerifyCount()
        {
            int prevBlobsCount = container.ListBlobs(useFlatBlobListing: true).Count();
            int uploadBlobsCount = 5;
            for (int i = 0; i < uploadBlobsCount; i++)
            {
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetUniqueBlobName());
                blockBlob.UploadText(i.ToString());

            }

            int newBlobCountInContainer = container.ListBlobs(null, true).Count()- prevBlobsCount;

            Assert.IsTrue(newBlobCountInContainer == uploadBlobsCount, "Number of blobs uploaded does not match count of blobs in the container.");
        }

        [TestMethod]
        public void Upload_Multiple_Blobs_Select_By_Name_Ensure_Count()
        {
            int uploadBlobsCount = 5;
            string blobName = string.Empty;
            for (int i = 0; i < uploadBlobsCount; i++)
            {
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetUniqueBlobName());
                blockBlob.UploadText(i.ToString());
                if (string.IsNullOrEmpty(blobName))
                {
                    blobName = blockBlob.Name;
                }
                
            }

            int blobCountWithGivenUniqueName = container.ListBlobs(blobName, true).Count();

            Assert.IsTrue(blobCountWithGivenUniqueName == 1, "Blob with given unique name was not found.");
        }
        [TestMethod]
        public void Upload_Blobs_Nested_Directory_Verify_Count()
        {
            int prevOuterEntries = container.ListBlobs().Count();
            int prevBlobsCount = container.ListBlobs(useFlatBlobListing: true).Count();

            var outerBlob = container.GetBlockBlobReference(GetUniqueBlobName());
            outerBlob.UploadText("This is a sample text blob");
           
            /// Directory is a vitual thing in the container. There is no corresponding thing in the Resource APIs.
            var firstDirectory = container.GetDirectoryReference(GetUniqueBlobName());
            var firstInnerBlob = firstDirectory.GetBlockBlobReference("Blob1");
            var secondInnerBlob = firstDirectory.GetBlockBlobReference("Blob2");
            firstInnerBlob.UploadText("This is a first sample text blob in a directory");
            secondInnerBlob.UploadText("This is a second sample text blob in a directory");
            
            var newEntriesCount = container.ListBlobs().Count() - prevOuterEntries;
            var newBlobsCount = container.ListBlobs(useFlatBlobListing: true).Count()- prevBlobsCount;

            Assert.IsTrue(newEntriesCount == 2 && newBlobsCount == 3, "Directory structure in the blob container is not correctly build when directory was built specifically.");
        }

        [TestMethod]
        public void Upload_Blobs_Nested_Directory_In_Name_Verify_Count()
        {
            int prevOuterEntries = container.ListBlobs().Count();
            int prevBlobsCount = container.ListBlobs(useFlatBlobListing: true).Count();

            var outerBlob = container.GetBlockBlobReference(GetUniqueBlobName());
            outerBlob.UploadText("This is a sample text blob");

            string secondBlob = GetUniqueBlobName();
            var firstInnerBlob = container.GetBlockBlobReference(secondBlob + "/blob1");
            var secondInnerBlob = container.GetBlockBlobReference(secondBlob+ "/blob2");
            firstInnerBlob.UploadText("This is a first sample text blob in a directory");
            secondInnerBlob.UploadText("This is a second sample text blob in a directory");

            var newEntriesCount = container.ListBlobs().Count()-prevOuterEntries;
            var newBlobsCount = container.ListBlobs(useFlatBlobListing: true).Count()-prevBlobsCount;
            
            Assert.IsTrue(newEntriesCount == 2 && newBlobsCount == 3, "Directory structure in the blob container is not correctly build when blob is uploaded with directory structure in name.");
        }

        [TestMethod]
        public void Upload_Multiple_Blobs_Select_Blobs_In_Pages_Verify_Count()
        {
            int uploadBlobsCount = 50;
            string blobName = string.Empty;
            for (int i = 0; i < uploadBlobsCount; i++)
            {
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetUniqueBlobName());
                blockBlob.UploadText(i.ToString());
                if (string.IsNullOrEmpty(blobName))
                {
                    blobName = blockBlob.Name;
                }

            }
            int totalBlobsCount = container.ListBlobs(useFlatBlobListing: true).Count();
            BlobContinuationToken continuationToken = null;
            BlobResultSegment resultSegment = null;
            int selectCount = 0;
            do
            {
                //This overload allows control of the page size. You can return all remaining results by passing null for the maxResults parameter,
                //or by calling a different overload.
                resultSegment = container.ListBlobsSegmented("", true, BlobListingDetails.All, 10, continuationToken, null, null);
                selectCount += resultSegment.Results.Count<IListBlobItem>();

                //Get the continuation token.
                continuationToken = resultSegment.ContinuationToken;
            }
            while (continuationToken != null);
            Assert.IsTrue(selectCount == totalBlobsCount, "Segemented listing of blobs did not return all blobs.");
        } 

        [TestMethod]
        public void Upload_LargeFileStream_ToBlockBlob_VerifySize()
        {
            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetUniqueBlobName());
            
            string writtenContent = string.Empty;
            string fileToUpload = CreateLargeFile(5);
            
            BlobRequestOptions bro = new BlobRequestOptions()
            {
                SingleBlobUploadThresholdInBytes = 1024 * 1024, //1MB, the minimum
                ParallelOperationThreadCount = 2,
                RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 1)
            };
            blobClient.DefaultRequestOptions = bro;
            blockBlob.UploadFromFile(fileToUpload, FileMode.Open, options: bro);

            long uploadedFileLength = (new FileInfo(fileToUpload)).Length;
            blockBlob.FetchAttributes();

            File.Delete(fileToUpload);

            Assert.IsTrue(uploadedFileLength == blockBlob.Properties.Length, "Blob length is different from what was uploaded.");

        }

        [TestMethod]
        public void Upload_Text_ToBlockBlob_VerifyUploadedContent()
        {
            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetUniqueBlobName());
            
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
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetUniqueBlobName());
            
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
            // blockBlob.DownloadText does not work and returned string has extra BOM character at the beginning
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

        [TestMethod]
        public void Upload_Page_Blob_Verify_Content()
        {
            CloudPageBlob pageBlob = container.GetPageBlobReference(GetUniqueBlobName());
            
            int totalBytes = 2 * 512; // 2 pages, data size has to be in multiple of 512 for it to work with page blobs
            byte[] data = new byte[totalBytes];
            for(int i = 0; i < totalBytes; i++)
            {
                data[i] = 5;
            }
            pageBlob.UploadFromByteArray(data, 0, totalBytes);
            byte[] readdata = new byte[totalBytes];
            pageBlob.DownloadToByteArray(readdata, 0);
            
            Assert.IsTrue(Enumerable.SequenceEqual(data,readdata), "Page blob downloaded content does not match what was uploaded.");
        }

        #endregion

        #region private methods

        private string GetUniqueBlobName()
        {
            return Guid.NewGuid().ToString();
        }
        private string GetBase64String(byte[] data)
        {
            return Convert.ToBase64String(data);
        }
        private string GetBase64String(string data)
        {
            return GetBase64String(Encoding.UTF8.GetBytes(data));
        }

        private string GetMD5Hash(byte[] data)
        {
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        private string GetMD5Hash(string data)
        {
            return GetMD5Hash(Encoding.UTF8.GetBytes(data));
        }
        private string CreateLargeFile(int appendCount)
        {
            string fileName = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                using (var fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName))
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        string content = reader.ReadToEnd();
                        for (int i = 1; i <= appendCount; i++)
                            writer.Write(content);
                    }
                }
            }
            return fileName;
        }

        private string GetResourceFileContent()
        {
            using (var fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceFileName))
            {
                using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        #endregion
    }
}
