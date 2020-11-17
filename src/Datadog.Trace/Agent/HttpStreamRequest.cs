using System;
using System.IO;
using System.Threading.Tasks;
using Datadog.Trace.Agent.MessagePack;

namespace Datadog.Trace.Agent
{
    internal class HttpStreamRequest : IApiRequest
    {
        private readonly Uri _uri;
        private readonly HttpOverStream.HttpHeaders _headers = new HttpOverStream.HttpHeaders();
        private readonly Stream _requestStream;
        private readonly Stream _responseStream;

        public HttpStreamRequest(Uri uri, Stream requestStream, Stream responseStream)
        {
            _uri = uri;
            _requestStream = requestStream;
            _responseStream = responseStream;
        }

        public void AddHeader(string name, string value)
        {
            _headers.Add(name, value);
        }

        public async Task<IApiResponse> PostAsync(Span[][] traces, FormatterResolverWrapper formatterResolver)
        {
            // buffer the entire contents for now
            var requestContentStream = new MemoryStream();
            await CachedSerializer.Instance.SerializeAsync(requestContentStream, traces, formatterResolver).ConfigureAwait(false);
            requestContentStream.Position = 0;

            /*
            // save messagepack to file
            if (traces.Length > 0)
            {
                using (var file = File.Create("C:\\temp\\traces.msgpack"))
                {
                    requestContentStream.CopyTo(file);
                    file.Flush();
                    requestContentStream.Position = 0;
                }
            }
            */

            _headers.Add("Content-Type", "application/msgpack");

            var content = new HttpOverStream.StreamContent(requestContentStream, requestContentStream.Length);
            var request = new HttpOverStream.HttpRequest("POST", _uri.Host, _uri.PathAndQuery, _headers, content);

            // send request, get response
            var client = new HttpOverStream.HttpClient();
            var response = client.Send(request, _requestStream, _responseStream);

            // buffer the entire contents for now
            var responseContentStream = new MemoryStream();
            response.Content.CopyTo(responseContentStream);
            responseContentStream.Position = 0;

            if (response.ContentLength != null && response.ContentLength != responseContentStream.Length)
            {
                throw new Exception("Content length from http headers does not match content's actual length.");
            }

            return new HttpStreamResponse(response.StatusCode, responseContentStream.Length, response.GetContentEncoding(), responseContentStream);
        }
    }
}