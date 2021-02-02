using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServingPartialContent
{
    [ApiController]
    public class PartialContentController : ControllerBase
    {
        [Route("getcontent")]
        public async Task<IActionResult> GetContent()
        {
            await Test2();
            return Ok();
        }

        #region Test1
        private async Task Test1()
        {
            var message1 = GetMessage("message 1");
            var message2 = GetMessage("message 2");
            var message3 = GetMessage("message 3");
            var content = message1.Concat(message2).Concat(message3).ToArray();

            await WriteMessage(content, 0, message1.Length);
            Thread.Sleep(5000);
            await WriteMessage(content, message1.Length, message2.Length);
            Thread.Sleep(5000);
            await WriteMessage(content, message1.Length + message2.Length, message3.Length);
            Thread.Sleep(5000);
        }
        private async Task WriteMessage(byte[] content, int offset, int length)
        {
            await Response.Body.WriteAsync(content, offset, length);
        }
        private byte[] GetMessage(string message)
        {
            JObject json = new JObject();
            json.Add("message", message);
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(json));
        }
        #endregion

        #region Test2
        private async Task Test2()
        {
            Response.ContentType = "application/json";
            Response.Headers[HeaderNames.CacheControl] = "no-cache";

            await WriteWithBodyWriterAsync(new { message = "message 1" }, HttpContext.RequestAborted);
            Thread.Sleep(5000);
            await WriteWithBodyWriterAsync(new { message = "message 2" }, HttpContext.RequestAborted);
            Thread.Sleep(5000);
            await WriteWithBodyWriterAsync(new { message = "message 3" }, HttpContext.RequestAborted);
            Thread.Sleep(5000);
        }
        private async Task WriteWithBodyWriterAsync(object obj, CancellationToken cancellationToken)
        {
            //This code is based on this post: https://www.stevejgordon.co.uk/using-the-bodyreader-and-bodywriter-in-asp-net-core-3-0
            if (cancellationToken.IsCancellationRequested)
                throw new InvalidOperationException("Cannot write message after request is complete.");
            if (obj is null)
                return;

            var bytesWritten = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj), Response.BodyWriter.GetMemory().Span);
            Response.BodyWriter.Advance(bytesWritten);
            await Response.BodyWriter.FlushAsync(cancellationToken);
        }
        #endregion
    }
}
