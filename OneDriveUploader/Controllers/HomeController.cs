
using Microsoft.Identity.Client;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.IdentityModel.Tokens;
using Azure.Identity;
using Microsoft.Graph;
using System.IO;
using System.Web;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Script.Serialization;
using OneDriveUploader.Models;

namespace OneDriveUploader.Controllers
{
    public class HomeController : Controller
    {
        const string tentid = "10b287a3-5f0c-4092-9cca-fecab20cc57f";
        const string clietid = "590b7bc9-4674-4a8c-af69-64cccc6a27e6";
        const string clientsecret = "aEq7Q~D5KWN67MXCKB8h2LImP.Qn0eVQSMAx-";
        const string useremail = "admin@StationCar.onmicrosoft.com";
        const string username = "fahim.soft@outlook.com";
        const string password = "fahim85836";
        private string videoAddress = "~/App_Data/Videos";
          Dictionary<string, string> breadcrumb =  new Dictionary<string, string>();

        onedrivedbEntities1 db = null;
        public HomeController()
        {
            db = new onedrivedbEntities1();
        }
        public async Task<ActionResult> Index(string id = "", string name = "", bool Isbreadcrumb = false)
        {

            var graphClient = await GraphClient();
            IDriveItemChildrenCollectionPage rootdata = null;
            if (string.IsNullOrEmpty(id))
            {
                rootdata = await graphClient.Drive.Root.Children.Request().GetAsync();
                Session["breadcrumb"] = null;
            }
            else
            {
                rootdata = await graphClient.Drive.Items[id].Children.Request().GetAsync();
                var previous = (Dictionary<string, string>)Session["breadcrumb"] == null ?  null : (Dictionary<string, string>)Session["breadcrumb"];

                //-----------remove bread crumb-----------------------
                if (previous != null && Isbreadcrumb == true)
                {
                    for (int i = previous.Count -1 ; i >=0 ; i--)
                    {
                        if (previous.ElementAt(i).Key == name)
                        {
                            break;
                        }
                        previous.Remove(previous.ElementAt(i).Key);
                    }
                }

                //-----------create bread crumb-----------------------
                if (previous != null)
                {
                    foreach (var item in previous)
                    {
                        breadcrumb.Add(item.Key, item.Value);
                    }       
                }
                if (!breadcrumb.ContainsKey(name))
                {
                    breadcrumb.Add(name, id);
                }
                Session["breadcrumb"] = breadcrumb;
            }
            var dd = rootdata.CurrentPage.Select(x => new DataModel
            {
                Id = x.Id,
                Name = x.Name,
                CreatedDateTime = x.CreatedDateTime,
                DownloadURL = x.WebUrl,
                FileType = x.File == null ? "folder" : "file",
                Size = FormatSize(Convert.ToInt64(x.Size)),
                ShareAbleLink = db.OnedriveDatas.Where(y => y.DriveItem == x.Id)?.FirstOrDefault()?.WebURL
            }
              ).OrderByDescending(x => x.CreatedDateTime).OrderByDescending(x => x.FileType).ToList();
            ViewBag.DataModel = dd;
            return View();
        }
        #region for local upload file 
        [HttpPost]
        public async Task<JsonResult> MultiUpload(string id, string fileName, string folderid, bool checkexistnig = false)
        {
            try
            {
                if (checkexistnig == false)
                {
                    IDriveItemChildrenCollectionPage items = null;
                    var graphClient = await GraphClient();
                    if (string.IsNullOrEmpty(folderid))
                    {
                        items = await graphClient
                                   .Drive.Root
                                   .Children
                                   .Request()
                                   .Filter($"name eq '{fileName}'")
                                   .GetAsync();
                    }
                    else
                    {
                        items = await graphClient
                                   .Drive.Items[folderid]
                                   .Children
                                   .Request()
                                   .Filter($"name eq '{fileName}'")
                                   .GetAsync();
                    }
                    if (items.Count() > 0)
                    {
                        return Json(new { success = "exising", msg = $" The destination already  has a file named '{fileName} !'" });
                    }
                }

                var chunkNumber = id;
                var chunks = Request.InputStream;
                string path = Server.MapPath(videoAddress);
                string newpath = Path.Combine(path, fileName + chunkNumber);
                using (FileStream fs = System.IO.File.Create(newpath))
                {
                    byte[] bytes = new byte[3757000];
                    int bytesRead;
                    while ((bytesRead = Request.InputStream.Read(bytes, 0, bytes.Length)) > 0)
                    {
                        fs.Write(bytes, 0, bytesRead);
                    }
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, msg = ex.Message });
            }
            return Json(new { success = true, msg = "" });
        }
        [HttpPost]
        public async Task<string> UploadComplete(string fileName, string complete, string folderid, string existingtype)
        {
            string tempPath = Server.MapPath(videoAddress);
            try
            {
                string videoPath = Server.MapPath(videoAddress);
                string newPath = Path.Combine(tempPath, fileName);
                if (complete == "1")
                {
                    string[] filePaths = System.IO.Directory.GetFiles(tempPath).Where(p => p.Contains(fileName)).OrderBy(p => Int32.Parse(p.Replace(fileName, "$").Split('$')[1])).ToArray();
                    foreach (string filePath in filePaths)
                    {
                        MergeFiles(newPath, filePath);
                    }
                }
                var uploadresponse = await UploadFile(videoPath, fileName, folderid, existingtype);
                if (System.IO.File.Exists(Path.Combine(tempPath, fileName)))
                {
                    System.IO.File.Delete(Path.Combine(tempPath, fileName));
                }
                if (System.IO.File.Exists(Path.Combine(videoPath, fileName)))
                {
                    System.IO.File.Delete(Path.Combine(videoPath, fileName));
                }

                //System.IO.File.Move(Path.Combine(tempPath, fileName), Path.Combine(videoPath, fileName));
                return "success";
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private static void MergeFiles(string file1, string file2)
        {
            FileStream fs1 = null;
            FileStream fs2 = null;
            try
            {
                fs1 = System.IO.File.Open(file1, FileMode.Append);
                fs2 = System.IO.File.Open(file2, FileMode.Open);
                byte[] fs2Content = new byte[fs2.Length];
                fs2.Read(fs2Content, 0, (int)fs2.Length);
                fs1.Write(fs2Content, 0, (int)fs2.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " : " + ex.StackTrace);
            }
            finally
            {
                if (fs1 != null) fs1.Close();
                if (fs2 != null) fs2.Close();
                System.IO.File.Delete(file2);
            }
        }
        #endregion
        public async Task<EmptyResult> UploadFile(string filepath, string filename, string folderid, string existingtype)
        {
            try
            {
                string ppp = System.Web.HttpContext.Current.Server.MapPath(Path.Combine(videoAddress, filename));
                using (var fileStream = System.IO.File.OpenRead(ppp))
                {
                    // Use properties to specify the conflict behavior
                    // in this case, replace
                    var uploadProps = new DriveItemUploadableProperties
                    {
                        ODataType = null,
                        AdditionalData = new Dictionary<string, object>
                        {
                            //{ "@microsoft.graph.conflictBehavior", "replace" }
                            { "@microsoft.graph.conflictBehavior",
                               existingtype.ToLower() == "replace" ? "replace" : "rename"
                            }
                        }
                    };
                    var graphClient = await GraphClient();

                    UploadSession uploadSession = null;
                    // Create the upload session
                    // itemPath does not need to be a path to an existing item
                    if (string.IsNullOrEmpty(folderid))
                    {
                        uploadSession = await graphClient.Drive.Root
                           .ItemWithPath(filename)
                           .CreateUploadSession(uploadProps)
                           .Request()
                           .PostAsync();
                    }
                    else
                    {
                        uploadSession = await graphClient.Drive.Items[folderid]
                      .ItemWithPath(filename)
                      .CreateUploadSession(uploadProps)
                      .Request()
                      .PostAsync();
                    }
                    // Max slice size must be a multiple of 320 KiB
                    int maxSliceSize = 320 * 1024;
                    var fileUploadTask =
                        new LargeFileUploadTask<DriveItem>(uploadSession, fileStream, maxSliceSize);
                    // Create a callback that is invoked after each slice is uploaded
                    IProgress<long> progress = new Progress<long>(prog =>
                    {
                        Console.WriteLine($"Uploaded {prog} bytes of {fileStream.Length} bytes");
                    });

                    try
                    {
                        // Upload the file
                        var uploadResult = await fileUploadTask.UploadAsync(progress);
                        if (uploadResult.UploadSucceeded)
                        {
                            // The ItemResponse object in the result represents the
                            // created item.
                            Console.WriteLine($"Upload complete, item ID: {uploadResult.ItemResponse.Id}");
                        }
                        else
                        {
                            Console.WriteLine("Upload failed");
                        }
                    }
                    catch (ServiceException ex)
                    {
                        Console.WriteLine($"Error uploading: {ex.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            ////----------------------------small file--------------------------------
            //var files = Request.Files;
            //for (int i = 0; i < files.Count; i++)
            //{
            //    HttpPostedFileBase file = files[i];
            //    int fileSize = file.ContentLength;
            //    string fileName = file.FileName;
            //    string mimeType = file.ContentType;
            //    Stream fileContent = file.InputStream;
            //    var directoryName = System.IO.Path.GetDirectoryName(Server.MapPath("~/upload/") + fileName);
            //    if (!System.IO.Directory.Exists(directoryName))
            //    {
            //        System.IO.Directory.CreateDirectory(directoryName);
            //    }
            //    try
            //    {
            //        var scopes = new[] { "https://graph.microsoft.com/.default" };
            //        var tenantId = tentid;
            //        var options = new TokenCredentialOptions
            //        {
            //            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            //        };
            //        var clientSecretCredential = new ClientSecretCredential(
            //            tenantId, clietid, clientsecret, options);
            //        var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            //        //                var innerfolder = await graphClient
            //        //.Users[useremail].Drive.Items.ItemWithPath(fileName).Content.Request().PutAsync<DriveItem>(fileContent);
            //        //                //-------------for upload-----------------

            //        string id = ((System.Web.HttpRequestWrapper)Request).Params["id"];
            //        if (string.IsNullOrEmpty(id))
            //        {
            //            var res = await graphClient.Drive.Root.ItemWithPath(fileName).Content
            //                .Request()
            //                .PutAsync<DriveItem>(fileContent);
            //        }
            //        else
            //        {
            //            var res = await graphClient.Drive.Items[id].ItemWithPath(fileName).Content
            //                  .Request()
            //                  .PutAsync<DriveItem>(fileContent);
            //        }
            //    }
            //    catch (Exception ex)
            //    {

            //        throw;
            //    }
            //}
            return null;
            //----------------------------small file--------------------------------
        }
        [HttpPost]
        public async Task<ActionResult> ShareAbleLink(string id)
        {
            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    var type = "view";
                    //var password = "ThisIsMyPrivatePassword";
                    var scope = "anonymous";
                    var graphClient = await GraphClient();
                    var shareresponse = await graphClient.Drive.Items[id]
                         .CreateLink(type, scope, null, null, null)
                         .Request()
                         .PostAsync();
                    //----------insert in table ----------------
                    var model = new OnedriveData
                    {
                        WebURL = shareresponse.Link.WebUrl,
                        DriveItem = id
                    };
                   var res =  db.OnedriveDatas.Add(model);
                    db.SaveChanges();

                    return Json(new { success = true, sharelink = shareresponse.Link.WebUrl });
                }
                else
                {
                    return Json(new { success = false, msg = "Try Again" });
                }
            }
            catch (Exception ex)
            {

                return Json(new { success = false, msg = ex.Message });
            }
        }
        [HttpPost]
        public async Task<ActionResult> DeleteFile(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {

                var graphClient = await GraphClient();

                await graphClient.Drive.Items[id]
        .Request()
        .DeleteAsync();
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<ActionResult> CreateFolder(string id, string name)
        {
            ////------------------create new folder---------------------
            try
            {
                string msg = string.Empty;
                var graphClient = await GraphClient();
                var driveItem = new DriveItem
                {
                    Name = string.IsNullOrEmpty(name) ? "Empity Folder" : name,
                    Folder = new Folder
                    {
                    },
                    AdditionalData = new Dictionary<string, object>()
                 {
                 {"@microsoft.graph.conflictBehavior", "rename"}
                 }
                };
                if (string.IsNullOrEmpty(id))
                {
                    var responsefolder = await graphClient.Drive.Root.Children.Request().AddAsync(driveItem);
                }
                else
                {
                    var responsefolder = await graphClient.Drive.Items[id].Children.Request().AddAsync(driveItem);
                }
                msg = string.Format("{0} Created Successfully", name);
                return Json(new { sucdess = true, msg = msg }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { sucdess = false, msg = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            ////------------------create new folder---------------------
        }
        public async Task<ActionResult> DownloadFile(string id, string name)
        {
            var graphclient = await GraphClient();
            Stream stream = await graphclient.Drive.Items[id].Content.Request().GetAsync();
            Int32 length = stream.Length > Int32.MaxValue ? Int32.MaxValue : Convert.ToInt32(stream.Length);
            Byte[] buffer = new Byte[length];
            stream.Read(buffer, 0, length);

            return File(buffer, System.Net.Mime.MediaTypeNames.Application.Octet, name);
        }
        public async Task<GraphServiceClient> GraphClient()
        {

            string token = string.Empty;
            if (Session["accesstoken"] != null)
            {
                token = Session["accesstoken"].ToString();
            }
            else
            {
                token = await GetTokenAsync(tentid, clietid, clientsecret);
                Session["accesstoken"] = token;
            }
            //-=-------------for access token---------------------

            var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("Bearer", token);

                return Task.CompletedTask;
            }));
            //-=-------------for access token---------------------

            //var scopes = new[] { "https://graph.microsoft.com/.default" };
            //var tenantId = tentid;
            //var options = new TokenCredentialOptions
            //{
            //    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            //};
            //var clientSecretCredential = new ClientSecretCredential(
            //    tenantId, clietid, clientsecret, options);
            //var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            return graphClient;
        }
        public async Task GetTokenAsync(string tenant, string clientId, string clientSecret, string username, string password)
        {
            HttpResponseMessage resp;
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                var req = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenant}/oauth2/token/");
                req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
         {
            {"grant_type", "password"},
            {"client_id", clientId},
            {"client_secret", clientSecret},
            {"resource", "https://graph.microsoft.com"},
            {"username", username},
            {"password", password}
         });

                resp = await httpClient.SendAsync(req);
                string content = await resp.Content.ReadAsStringAsync();
                var jsonObj = new JavaScriptSerializer().Deserialize<dynamic>(content);
                string token = jsonObj["access_token"];
                Console.WriteLine(token);
            }
        }
        public async Task<string> GetTokenAsync(string tenant, string clientId, string clientSecret)
        {

            try
            {
                //string authority = "https://login.microsoftonline.com/10b287a3-5f0c-4092-9cca-fecab20cc57f/oauth2/v2.0/token";
                var scopes = new List<string>();
                scopes.Add("User.Read");
                scopes.Add("Files.ReadWrite");
                scopes.Add("Files.ReadWrite.All");
                HttpResponseMessage resp;
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                    var req = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenant}/oauth2/token/");
                    req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
         {
            {"grant_type", "client_credentials"},
            {"client_id", clientId},
            {"client_secret", clientSecret},
            {"resource", "https://graph.microsoft.com"},
            {"scope", "Files.ReadWrite.All"},
            //{"scope", "https://graph.microsoft.com/.default"},
            //{"username", username},
            //{"password", password}
         });

                    resp = await httpClient.SendAsync(req);
                    string content = await resp.Content.ReadAsStringAsync();
                    var jsonObj = new JavaScriptSerializer().Deserialize<dynamic>(content);
                    string token = jsonObj["access_token"];
                    Console.WriteLine(token);
                    return token;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        // Load all suffixes in an array  
        static readonly string[] suffixes =
        { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public string FormatSize(Int64 bytes)
        {
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return string.Format("{0:n1}{1}", number, suffixes[counter]);
        }
    
    }

    public class DataModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public DateTimeOffset? CreatedDateTime { get; internal set; }
        public string DownloadURL { get; internal set; }
        public string FileType { get; internal set; }
        public string Size { get; internal set; }
        public string ShareAbleLink { get; internal set; }
    }




}


