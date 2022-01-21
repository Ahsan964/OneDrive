using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OneDriveUploader.Controllers
{
    public class UploadLArgeFilesController : Controller
    {
        private string videoAddress = "~/App_Data/Videos";
        public async Task<ActionResult> Index(string id)
        {
            return View();
        }
        [HttpPost]
        public string MultiUpload(string id, string fileName)
        {
            try
            {
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

                throw;
            }
            return "done";
        }
        [HttpPost]
        public string UploadComplete(string fileName, string complete)
        {
            try
            {
                string tempPath = Server.MapPath(videoAddress);
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
                System.IO.File.Move(Path.Combine(tempPath, fileName), Path.Combine(videoPath, fileName));
            }
            catch (Exception ex)
            {

                throw;
            }
            return "success";
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
    }
}