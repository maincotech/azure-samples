using System;
using System.IO;
using System.Net;
using System.Text.Encodings.Web;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

namespace FunctionSamples
{
    public static class Functions
    {

        [FunctionName("SendMailWhenBlobUpdated")]
        public static void Run([BlobTrigger("training/{name}", Connection = "")] CloudBlockBlob myBlob, string name, ILogger log,
            [SendGrid()] out SendGridMessage message)
        {
            SharedAccessBlobPermissions permissions = SharedAccessBlobPermissions.Read;

            TimeSpan clockSkew = TimeSpan.FromMinutes(15d);
            TimeSpan accessDuration = TimeSpan.FromHours(24d);

            var blobSAS = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTime.UtcNow.Subtract(clockSkew),
                SharedAccessExpiryTime = DateTime.UtcNow.Add(accessDuration) + clockSkew,
                Permissions = permissions
            };

            //generate sas token
            var sas = myBlob.GetSharedAccessSignature(blobSAS);
            var url = myBlob.Uri.ToString() + sas;
            
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Sas: {url}");
           // var endcoder = UrlEncoder.Create();
            //send mail to users
            message = new SendGridMessage();
            message.AddTo("minghl@maincotech.com");
            message.AddContent("text/html", $"<a href='{url}'>{name}</a>");
            message.SetFrom("noreply@maincotech.com");
            message.SetSubject("Today's ppt");
        }


    }
}
