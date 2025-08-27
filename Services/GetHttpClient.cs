using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using A1AR.SVC.Worker.Lib.Common;
using Microsoft.Extensions.Logging;

namespace Fjord1.Int.API.Services
{
    public interface IGetHttpClient
    {
        HttpClient CreateUBW(WorkerSettings _settings);
    }

    public class GetHttpClient : IGetHttpClient
    {
        public HttpClient CreateUBW(WorkerSettings _settings)
        {
            var username = _settings.UserNameUBW;
            var password = _settings.PasswordUBW;
            var baseUrl = _settings.BaseUri;

            var client = new HttpClient();
            Uri baseUri = new Uri(baseUrl);
            client.BaseAddress = baseUri;

            var authToken = Encoding.ASCII.GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
            return client;
        }
    }
}