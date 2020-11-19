using System;
using System.Threading;
using Datadog.Trace.HttpOverStreams;

namespace Datadog.Trace.Agent.Transports
{
    internal class HttpStreamRequestFactory : IApiRequestFactory
    {
        private readonly IStreamFactory _streamFactory;
        private readonly DatadogHttpClient _httpClient;

        public HttpStreamRequestFactory(IStreamFactory streamFactory, DatadogHttpClient httpClient)
        {
            _streamFactory = streamFactory;
            _httpClient = httpClient;
        }

        public IApiRequest Create(Uri endpoint)
        {
            var bidirectionalStream = _streamFactory.GetBidirectionalStream();
            return new HttpStreamRequest(_httpClient, endpoint, bidirectionalStream, bidirectionalStream);
        }
    }
}
