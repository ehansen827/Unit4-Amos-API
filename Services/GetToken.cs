using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Fjord1.Int.API.Services
{
    public class GetToken
    {
        public static string ReturnToken()
        {
            var tokenUrl = "https://s-no-ids1.unit4cloud.com/identity/connect/token";
            var clientId = "u4erp-api-u4erx_kje_prod-m2m-3";
            var clientSecret = "59a4486a-52a0-4f25-ae2c-187bd3276199";

            var tokenClient = new HttpClient();
            var tokencontent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret},
                { "grant_type", "client_credentials" },
                { "scope", "u4erp" },
            });

            var httpTokenRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(tokenUrl))
            {
                Content = tokencontent
            };

            var response = tokenClient.SendAsync(httpTokenRequestMessage).Result;

            var responseStream = response.Content.ReadAsStringAsync();
            var authheaders = JsonConvert.DeserializeObject<TokenObject>(responseStream.Result);

            if (authheaders == null || string.IsNullOrEmpty(authheaders.access_token))
            {
                throw new InvalidOperationException("Failed to retrieve access token from response.");
            }

            return authheaders.access_token;
        }
    }

    public class TokenObject
    {
        public string access_token { get; set; }
        public string? scope { get; set; }
        public int expires_in { get; set; }
        public string? token_type { get; set; }
        public TokenObject(string access_token, string scope, int expires_in, string token_type)
        {
            this.access_token = access_token;
            this.scope = scope;
            this.expires_in = expires_in;
            this.token_type = token_type;
        }
    }
}
