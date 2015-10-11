using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.File;
using System.IO;

namespace FileShareStorageTest
{
    
    [TestClass]
    public class FileStoraeTest
    {
        private static CloudStorageAccount storageAccount;
        private static CloudFileClient fileClient;
        private static CloudFileShare share;
        private static CloudFileDirectory rootDirectory;

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            storageAccount = CloudStorageAccount.Parse(AppSetting.StorageConnectionString);
            fileClient = storageAccount.CreateCloudFileClient();
            share = fileClient.GetShareReference(Guid.NewGuid().ToString());
            share.CreateIfNotExists();
            rootDirectory = share.GetRootDirectoryReference();
        }
        [ClassCleanup]
        public static void ClassCleanup()
        {
            share.Delete();
        }

        [TestMethod]
        public void Create_Directory_Check_Exist()
        {
            var testDir = rootDirectory.GetDirectoryReference("TestDirectory");
            testDir.CreateIfNotExists();

            Assert.IsTrue(rootDirectory.GetDirectoryReference("TestDirectory").Exists(), "Directory was not created in the Share");
        }

        [TestMethod]
        public void Create_File_Check_Exist()
        {
            var testDir = rootDirectory.GetDirectoryReference("TestDirectory");
            testDir.CreateIfNotExists();

            var myFile = testDir.GetFileReference("Myfile.txt");
         
            myFile.UploadText("THIS IS SAMPLE FILE");
            Assert.AreEqual(testDir.GetFileReference("Myfile.txt").DownloadText(), "THIS IS SAMPLE FILE", "Uploaded file content does not match");
        }

        [TestMethod]
        public void Create_File_Access_AS_SMB_Verify_Content()
        {
            var testDir = rootDirectory.GetDirectoryReference("TestDirectory");
            testDir.CreateIfNotExists();

            var myFile = testDir.GetFileReference("Myfile.txt");
            var content = "THIS IS SAMPLE FILE";
            myFile.UploadText(content);

            string fileNetworkShareName = $@"\\{AppSetting.StorageAccountFullName}\{share.Name}\{testDir.Name}\{myFile.Name}";
            Assert.AreEqual(File.ReadAllText(fileNetworkShareName), content, "Uploaded file content does not match when read as SMB share");
        }

        [TestMethod]
        public void Put_File_AS_SMB_To_Share_Verify_Uploaded()
        {
            string rootDirShareName = $@"\\{AppSetting.StorageAccountFullName}\{share.Name}\{rootDirectory.Name}";

            var tempFile = Path.GetTempFileName();
            var content = "THIS IS SAMPLE FILE";
            File.WriteAllText(tempFile, content);
            string fileName = "testfile.txt";
            File.Copy(tempFile, $"{rootDirShareName}\\{fileName}");

            var cloudCopiedFile = rootDirectory.GetFileReference(fileName);

            Assert.AreEqual(cloudCopiedFile.DownloadText(), content, "Uploaded file content does not match when written by copyng to SMB share");
        }
    }
}
