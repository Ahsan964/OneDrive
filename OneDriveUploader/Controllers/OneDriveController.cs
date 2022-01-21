using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace OneDriveUploader.Controllers
{
    public class OneDriveController : Controller
    {

        //[HttpPost]
        //public async Task UploadFileOnedrive()
        //{
        //    try
        //    {
        //        var token = await OneDriveController.GetTokenAsync(tentid, clietid, clientsecret);
        //        var files = Request.Files;
        //        for (int i = 0; i < files.Count; i++)
        //        {
        //            HttpPostedFileBase file = files[i];
        //            int fileSize = file.ContentLength;
        //            string fileName = file.FileName;
        //            string mimeType = file.ContentType;
        //            Stream fileContent = file.InputStream;
        //            var directoryName = System.IO.Path.GetDirectoryName(Server.MapPath("~/upload/") + fileName);
        //            if (!System.IO.Directory.Exists(directoryName))
        //            {
        //                System.IO.Directory.CreateDirectory(directoryName);
        //            }
        //            using (var client = new HttpClient())
        //            {

        //                var url = "https://graph.microsoft.com/v1.0/me/drive/root:" + $"/{fileName}:/content";
        //                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

        //                MemoryStream ms = new MemoryStream();
        //                fileContent.CopyTo(ms);
        //                byte[] sContents = ms.ToArray();
        //                var content = new ByteArrayContent(sContents);

        //                var response = client.PutAsync(url, content).Result.Content.ReadAsStringAsync().Result;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}
        //public async Task<ActionResult> Index()
        //{
        //    const string url = @"https://login.live.com/oauth20_authorize.srf?client_id=fd60e65e-750d-492c-a82b-569d70beb5bf&scope=Files.ReadWrite.All&response_type=token&redirect_uri=https://localhost:44304/Home/About";
        //    const string tentid = "ae2a7bce-9281-4f63-bb2a-422a7568daf1";
        //    const string clietid = "3c92577c-ee11-4108-88b3-0daef90aed4f";
        //    const string clientsecret = "BSs7Q~3magBVlLDTTppuUwy~VJHGH~iyRvj6v";
        //    //const string username = "fahim.soft@outlook.com";
        //    //const string password = "fahim85836";
        //    await GetTokenAsync(tentid, clietid, clientsecret);

        //    WebRequest request = WebRequest.Create(url);
        //    request.Method = "GET";
        //    using (WebResponse response = request.GetResponse())
        //    {
        //        using (Stream stream = response.GetResponseStream())
        //        {
        //            StreamReader reader = new StreamReader(stream);
        //            string text = reader.ReadToEnd();
        //        }
        //    }

        //    return View();
        //}
        //public static async Task <string> GetTokenAsync(string tenant, string clientId, string clientSecret)
        //{

        //    try
        //    {
        //        //string authority = "https://login.microsoftonline.com/10b287a3-5f0c-4092-9cca-fecab20cc57f/oauth2/v2.0/token";
        //        var scopes = new List<string>();
        //        scopes.Add("User.Read");
        //        scopes.Add("Files.ReadWrite");
        //        scopes.Add("Files.ReadWrite.All");
        //        HttpResponseMessage resp;
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
        //            var req = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenant}/oauth2/token/");
        //            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        // {
        //    {"grant_type", "client_credentials"},
        //    {"client_id", clientId},
        //    {"client_secret", clientSecret},
        //    {"resource", "https://graph.microsoft.com"},
        //    {"scope", "Files.ReadWrite.All"},
        //    //{"scope", "https://graph.microsoft.com/.default"},
        //    //{"username", username},
        //    //{"password", password}
        // });

        //            resp = await httpClient.SendAsync(req);
        //            string content = await resp.Content.ReadAsStringAsync();
        //            var jsonObj = new JavaScriptSerializer().Deserialize<dynamic>(content);
        //            string token = jsonObj["access_token"];
        //            Console.WriteLine(token);
        //            return token;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}

        //public static async Task<string> CreateFileOneDrive(string token, string FileName, string FolderId)
        //{
        //    try
        //    {
        //        using (var client = new HttpClient())
        //        {
        //            string url = FolderId.ToLower() == "root" ?
        //                "https://graph.microsoft.com/v1.0/me/drive/root:/" + FileName + ":/createUploadSession" :
        //                "https://graph.microsoft.com/v1.0/me/drive/items/" + FolderId + ":/" + FileName + ":/createUploadSession";
        //            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        //            var sessionResponse = await client.PostAsync(url, null).Result.Content.ReadAsStringAsync();
        //            var uploadSession = JsonConvert.DeserializeObject<dynamic>(sessionResponse);
        //            return uploadSession.uploadUrl;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        throw;
        //    }
        //}
    }

    public class ChunkedUploadProvider
    {
        private const int DefaultMaxChunkSize = 5 * 1024 * 1024;
        private const int RequiredChunkSizeIncrement = 320 * 1024;

        /// <summary>
        /// The UploadSession object
        /// </summary>
        public UploadSession Session { get; private set; }
        private IBaseClient client;
        private Stream uploadStream;
        private readonly int maxChunkSize;
        private List<Tuple<long, long>> rangesRemaining;
        private long totalUploadLength => uploadStream.Length;

        /// <summary>
        /// Helps with resumable uploads. Generates chunk requests based on <paramref name="session"/>
        /// information, and can control uploading of requests using <paramref name="client"/>
        /// </summary>
        /// <param name="session">Session information.</param>
        /// <param name="client">Client used to upload chunks.</param>
        /// <param name="uploadStream">Readable, seekable stream to be uploaded. Length of session is determined via uploadStream.Length</param>
        /// <param name="maxChunkSize">Max size of each chunk to be uploaded. Multiple of 320 KiB (320 * 1024) is required.
        /// If less than 0, default value of 5 MiB is used. .</param>
        public ChunkedUploadProvider(UploadSession session, IBaseClient client, Stream uploadStream, int maxChunkSize = -1)
        {
            if (!uploadStream.CanRead || !uploadStream.CanSeek)
            {
                throw new ArgumentException("Must provide stream that can read and seek");
            }

            this.Session = session;
            this.client = client;
            this.uploadStream = uploadStream;
            this.rangesRemaining = this.GetRangesRemaining(session);
            this.maxChunkSize = maxChunkSize < 0 ? DefaultMaxChunkSize : maxChunkSize;
            if (this.maxChunkSize % RequiredChunkSizeIncrement != 0)
            {
                throw new ArgumentException("Max chunk size must be a multiple of 320 KiB", nameof(maxChunkSize));
            }
        }

        /// <summary>
        /// Get the series of requests needed to complete the upload session. Call <see cref="UpdateSessionStatusAsync"/>
        /// first to update the internal session information.
        /// </summary>
        /// <param name="options">Options to be applied to each request.</param>
        /// <returns>All requests currently needed to complete the upload session.</returns>
        public virtual IEnumerable<UploadChunkRequest> GetUploadChunkRequests(IEnumerable<Option> options = null)
        {
            foreach (var range in this.rangesRemaining)
            {
                var currentRangeBegins = range.Item1;

                while (currentRangeBegins <= range.Item2)
                {
                    var nextChunkSize = NextChunkSize(currentRangeBegins, range.Item2);
                    var uploadRequest = new UploadChunkRequest(
                        this.Session.UploadUrl,
                        this.client,
                        options,
                        currentRangeBegins,
                        currentRangeBegins + nextChunkSize - 1,
                        this.totalUploadLength);

                    yield return uploadRequest;

                    currentRangeBegins += nextChunkSize;
                }
            }
        }

        /// <summary>
        /// Get the status of the session. Stores returned session internally.
        /// Updates internal list of ranges remaining to be uploaded (according to the server).
        /// </summary>
        /// <returns>UploadSession returned by the server.</returns>
        public virtual async Task<UploadSession> UpdateSessionStatusAsync()
        {
            var request = new UploadSessionRequest(this.Session, this.client, null);
            var newSession = await request.GetAsync().ConfigureAwait(false);

            var newRangesRemaining = this.GetRangesRemaining(newSession);

            this.rangesRemaining = newRangesRemaining;
            newSession.UploadUrl = this.Session.UploadUrl; // Sometimes the UploadUrl is not returned
            this.Session = newSession;
            return newSession;
        }

        /// <summary>
        /// Delete the session.
        /// </summary>
        /// <returns>Once returned task is complete, the session has been deleted.</returns>
        public async Task<UploadSession> DeleteSession()
        {
            var request = new UploadSessionRequest(this.Session, this.client, null);
            return await request.DeleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Upload the whole session.
        /// </summary>
        /// <param name="maxTries">Number of times to retry entire session before giving up.</param>
        /// <param name="options">Query and header option name value pairs for the request.</param>
        /// <returns>Item information returned by server.</returns>
        public async Task<DriveItem> UploadAsync(int maxTries = 3, IEnumerable<Option> options = null)
        {
            var uploadTries = 0;
            var readBuffer = new byte[this.maxChunkSize];
            var trackedExceptions = new List<Exception>();

            while (uploadTries < maxTries)
            {
                var chunkRequests = this.GetUploadChunkRequests(options);

                foreach (var request in chunkRequests)
                {
                    var result = await this.GetChunkRequestResponseAsync(request, readBuffer, trackedExceptions).ConfigureAwait(false);

                    if (result.UploadSucceeded)
                    {
                        return result.ItemResponse;
                    }
                }

                await this.UpdateSessionStatusAsync().ConfigureAwait(false);
                uploadTries += 1;
                if (uploadTries < maxTries)
                {
                    // Exponential backoff in case of failures.
                    await System.Threading.Tasks.Task.Delay(2000 * uploadTries * uploadTries).ConfigureAwait(false);
                }
            }

            throw new TaskCanceledException("Upload failed too many times. See InnerException for list of exceptions that occured.", new AggregateException(trackedExceptions.ToArray()));
        }

        /// <summary>
        /// Write a chunk of data using the UploadChunkRequest.
        /// </summary>
        /// <param name="request">The UploadChunkRequest to make the request with.</param>
        /// <param name="readBuffer">The byte[] content to read from.</param>
        /// <param name="exceptionTrackingList">A list of exceptions to use to track progress. ChunkedUpload may retry.</param>
        /// <returns></returns>
        public virtual async Task<UploadChunkResult> GetChunkRequestResponseAsync(UploadChunkRequest request, byte[] readBuffer, ICollection<Exception> exceptionTrackingList)
        {
            var firstAttempt = true;
            this.uploadStream.Seek(request.RangeBegin, SeekOrigin.Begin);
            await this.uploadStream.ReadAsync(readBuffer, 0, request.RangeLength).ConfigureAwait(false);

            while (true)
            {
                using (var requestBodyStream = new MemoryStream(request.RangeLength))
                {
                    await requestBodyStream.WriteAsync(readBuffer, 0, request.RangeLength).ConfigureAwait(false);
                    requestBodyStream.Seek(0, SeekOrigin.Begin);

                    try
                    {
                        return await request.PutAsync(requestBodyStream).ConfigureAwait(false);
                    }
                    catch (ServiceException exception)
                    {
                        if (exception.IsMatch("generalException") || exception.IsMatch("timeout"))
                        {
                            if (firstAttempt)
                            {
                                firstAttempt = false;
                                exceptionTrackingList.Add(exception);
                            }
                            else
                            {
                                throw;
                            }
                        }
                        else if (exception.IsMatch("invalidRange"))
                        {
                            // Succeeded previously, but nothing to return right now
                            return new UploadChunkResult();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }

        internal List<Tuple<long, long>> GetRangesRemaining(UploadSession session)
        {
            // nextExpectedRanges: https://dev.onedrive.com/items/upload_large_files.htm
            // Sample: ["12345-55232","77829-99375"]
            // Also, second number in range can be blank, which means 'until the end'
            var newRangesRemaining = new List<Tuple<long, long>>();
            foreach (var range in session.NextExpectedRanges)
            {
                var rangeSpecifiers = range.Split('-');
                newRangesRemaining.Add(new Tuple<long, long>(long.Parse(rangeSpecifiers[0]),
                    string.IsNullOrEmpty(rangeSpecifiers[1]) ? this.totalUploadLength - 1 : long.Parse(rangeSpecifiers[1])));
            }

            return newRangesRemaining;
        }

        private long NextChunkSize(long rangeBegin, long rangeEnd)
        {
            var sizeBasedOnRange = (long)(rangeEnd - rangeBegin) + 1;
            return sizeBasedOnRange > this.maxChunkSize
                ? this.maxChunkSize
                : sizeBasedOnRange;
        }
    }

}