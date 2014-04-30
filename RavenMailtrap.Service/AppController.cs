using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;

namespace RavenMailtrap
{
    [RoutePrefix("")]
    public class AppController : ApiController
    {
        [HttpGet]
        [Route("")]
        public HttpResponseMessage Get()
        {
            return IndexPage();
        }

        private HttpResponseMessage IndexPage()
        {
            try
            {
                using (FileStream stream = File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.html")))
                {
                    var sha = MD5.Create();
                    byte[] hash = sha.ComputeHash(stream);

                    var etag =
                        new EntityTagHeaderValue("\"" + BitConverter.ToString(hash).Replace("-", String.Empty) + "\"");

                    HttpHeaderValueCollection<EntityTagHeaderValue> headers = Request.Headers.IfNoneMatch;
                    if (headers.Count > 0 && headers.Any(x => x.Tag == etag.Tag))
                    {
                        return Request.CreateResponse(HttpStatusCode.NotModified);
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                    using (var sr = new StreamReader(stream))
                    {
                        var resp = new HttpResponseMessage()
                        {
                            Content = new StringContent(sr.ReadToEnd(), Encoding.UTF8, "text/html")
                        };
                        resp.Headers.ETag = etag;
                        resp.StatusCode = HttpStatusCode.OK;
                        return resp;
                    }
                }
            }
            catch (FileNotFoundException)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }
    }
}