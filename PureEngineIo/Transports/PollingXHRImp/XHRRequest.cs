using PureEngineIo.EmitterImp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PureEngineIo.Transports.PollingXHRImp
{
    public class XHRRequest : Emitter
    {
        private readonly string _method;
        private readonly string _uri;
        private readonly byte[] _data;
        private readonly string _cookieHeaderValue;
        private readonly Dictionary<string, string> _extraHeaders;

        public XHRRequest(RequestOptions options)
        {
            _method = options.Method ?? "GET";
            _uri = options.Uri;
            _data = options.Data;
            _cookieHeaderValue = options.CookieHeaderValue;
            _extraHeaders = options.ExtraHeaders;
        }

        public void Create()
        {
            var httpMethod = _method == "POST" ? HttpMethod.Post : HttpMethod.Get;
            var dataToSend = _data ?? Encoding.UTF8.GetBytes("");

            Task.Run(async () =>
            {
                try
                {
                    using (var httpClientHandler = new HttpClientHandler())
                    {
                        //if (ServerCertificate.Ignore)
                        //{
                        //    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                        //}

                        using (var client = new HttpClient(httpClientHandler))
                        {
                            using (var httpContent = new ByteArrayContent(dataToSend))
                            {
                                if (_method == "POST")
                                {
                                    httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                                }

                                var request = new HttpRequestMessage(httpMethod, _uri)
                                {
                                    Content = httpContent
                                };

                                if (!string.IsNullOrEmpty(_cookieHeaderValue))
                                {
                                    httpContent.Headers.Add(@"Cookie", _cookieHeaderValue);
                                }
                                if (_extraHeaders != null)
                                {
                                    foreach (var header in _extraHeaders)
                                    {
                                        httpContent.Headers.Add(header.Key, header.Value);
                                    }
                                }

                                if (_method == "GET")
                                {
                                    using (var response = await client.GetAsync(request.RequestUri))
                                    {
                                        var responseContent = await response.Content.ReadAsStringAsync();
                                        OnData(responseContent);
                                    }
                                }
                                else
                                {
                                    using (var response = await client.SendAsync(request))
                                    {
                                        response.EnsureSuccessStatusCode();
                                        var contentType = response.Content.Headers.GetValues("Content-Type").Aggregate("", (acc, x) => acc + x).Trim();

                                        if (contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var responseContent = await response.Content.ReadAsByteArrayAsync();
                                            OnData(responseContent);
                                        }
                                        else
                                        {
                                            var responseContent = await response.Content.ReadAsStringAsync();
                                            OnData(responseContent);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    OnError(e);
                }
            }).Wait();
        }

        private void OnSuccess() => Emit(Transport.EVENT_SUCCESS);

        private void OnData(string data)
        {
            //var log = LogManager.GetLogger(Global.CallerName());
            //log.Info("OnData string = " + data);
            Emit(Transport.EVENT_DATA, data);
            OnSuccess();
        }

        private void OnData(byte[] data)
        {
            //var log = LogManager.GetLogger(Global.CallerName());
            //log.Info(string.Format("OnData byte[] ={0}", System.Text.Encoding.UTF8.GetString(data, 0, data.Length)));
            Emit(Transport.EVENT_DATA, data);
            OnSuccess();
        }

        private void OnError(Exception err) => Emit(Transport.EVENT_ERROR, err);

        private void OnRequestHeaders(Dictionary<string, string> headers) => Emit(Transport.EVENT_REQUEST_HEADERS, headers);

        private void OnResponseHeaders(Dictionary<string, string> headers) => Emit(Transport.EVENT_RESPONSE_HEADERS, headers);
    }
}
