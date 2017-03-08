using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EWSTestConsole
{
    public class AzureStorageUtil
    {
        public static async Task<string> UploadFile(string fileName, byte[] fileBytes)
        {
            return await UploadFile(fileName, "emails", fileBytes);
        }
        public static async Task<string> UploadFile(string fileName, string container, byte[] fileBytes)
        {
            //get configuration data
            string url = null;
            string azureBlobProtocol = ConfigurationManager.AppSettings["abs:Protocol"];
            string azureBlobAccountName = ConfigurationManager.AppSettings["abs:AccountName"];
            string azureBlobAccountkey = ConfigurationManager.AppSettings["abs:AccountKey"];
            container = container.Replace(".", "");

            // Initialize the Azure account information
            string connString = string.Format("DefaultEndpointsProtocol={0};AccountName={1};AccountKey={2}",
                azureBlobProtocol, azureBlobAccountName, azureBlobAccountkey);
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connString);

            // Create the blob client, which provides authenticated access to the Blob service.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();

            // Get the container reference...create if it does not exist
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(container);
            if (!blobContainer.Exists())
                await blobContainer.CreateAsync();
            CloudBlockBlob blob = blobContainer.GetBlockBlobReference(fileName);

            // Set permissions on the container.
            BlobContainerPermissions containerPermissions = new BlobContainerPermissions();
            containerPermissions.PublicAccess = BlobContainerPublicAccessType.Off;
            await blobContainer.SetPermissionsAsync(containerPermissions);

            //upload the file using a memory stream
            using (MemoryStream stream = new MemoryStream(fileBytes))
            {
                stream.Write(fileBytes, 0, fileBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                stream.Flush();
                blob.UploadFromStream(stream);
                stream.Close();

                //get the url of the blob
                url = String.Format("{0}://{1}.blob.core.windows.net/{2}/{3}", azureBlobProtocol,
                    azureBlobAccountName, container, fileName);
            }

            return url;
        }
        public static async Task<bool> DeleteFile(string fileName, string container)
        {
            //get configuration data
            string azureBlobProtocol = ConfigurationManager.AppSettings["abs:Protocol"];
            string azureBlobAccountName = ConfigurationManager.AppSettings["abs:AccountName"];
            string azureBlobAccountkey = ConfigurationManager.AppSettings["abs:AccountKey"];
            container = container.Replace(".", "");

            // Initialize the Azure account information
            string connString = string.Format("DefaultEndpointsProtocol={0};AccountName={1};AccountKey={2}",
                azureBlobProtocol, azureBlobAccountName, azureBlobAccountkey);
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connString);

            // Create the blob client, which provides authenticated access to the Blob service.
            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();

            // Get the container reference...create if it does not exist
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(container);
            if (!blobContainer.Exists())
                return true;

            try
            {
                CloudBlockBlob blob = blobContainer.GetBlockBlobReference(fileName);
                await blob.DeleteAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}