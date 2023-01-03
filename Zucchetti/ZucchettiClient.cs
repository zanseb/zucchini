using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Zucchetti
{
    public class ZucchettiClient
    {
        private const string ssoRoute = "servlet/ushp_btrustsite";
        private const string sqlDataProviderRoute = "servlet/SQLDataProviderServer";
        private const string stampRoute = "servlet/ushp_ftimbrus";
        private const string mCIDRoute = "jsp/ushp_one_column_model.jsp";
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.81 Safari/537.36 Edg/104.0.1293.47";

        private readonly string userName;
        private readonly string password;

        private readonly HttpClient httpClient;

        public ZucchettiClient(string baseURL, string userName, string password)
        {
            this.userName = userName;
            this.password = password;

            var cookies = new CookieContainer();
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookies;
            handler.UseCookies = true;
            httpClient = new(handler) { BaseAddress = new Uri(baseURL) };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);
        }

        public async Task LoginAsync()
        {
            var ssoPageResponse = await httpClient.GetAsync(QueryHelpers.AddQueryString(ssoRoute, "idSSO", "1"));
            ssoPageResponse.EnsureSuccessStatusCode();

            var ssoPagePayload = await ssoPageResponse.Content.ReadAsStringAsync();
            var authRoute = HtmlContentExtractor.ExtractAuthRoute(ssoPagePayload);

            var ssoLoginParams = new Dictionary<string, string>()
            {
                ["username"] = userName,
                ["password"] = password,
            };

            var ssoLoginResponse = await httpClient.PostAsync(authRoute, new FormUrlEncodedContent(ssoLoginParams));
            ssoLoginResponse.EnsureSuccessStatusCode();

            var ssoLoginPayload = await ssoLoginResponse.Content.ReadAsStringAsync();
            var SAMLResult = HtmlContentExtractor.ExtractSAML(ssoLoginPayload);

            var forwardSAMLParams = new Dictionary<string, string>()
            {
                ["SAMLResponse"] = SAMLResult,
            };
            var forwardSAMLResponse = await httpClient.PostAsync(ssoRoute, new FormUrlEncodedContent(forwardSAMLParams));
            forwardSAMLResponse.EnsureSuccessStatusCode();
        }

        public async Task<IEnumerable<Stamp>> RetrieveStampsAsync(DateOnly day)
        {
            var loadStampsParams = new Dictionary<string, string>()
            {
                ["rows"] = "25",
                ["startrow"] = "0",
                ["sqlcmd"] = "rows:ushp_fgettimbrus",
                ["pDATE"] = day.ToString("yyyy-MM-dd"),
            };

            var response = await httpClient.PostAsync(sqlDataProviderRoute, new FormUrlEncodedContent(loadStampsParams));
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<StampsResult>(payload);
            if (result == null)
            {
                return Array.Empty<Stamp>();
            }

            var stamps = new List<Stamp>();
            foreach (var stamp in result.Data)
            {
                if (stamp is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    var stampProperties = element.Deserialize<List<string>>();
                    var datePart = stampProperties[0];
                    var timePart = stampProperties[1];
                    var direction = stampProperties[2] == "E" ? StampDirection.In : StampDirection.Out;

                    var stampeDateTime = DateTime.ParseExact($"{datePart} {timePart}", "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture);
                    stamps.Add(new Stamp(stampeDateTime, direction));
                }
            }

            return stamps;
        }

        public Task ClockInAsync()
        {
            return CreateStampAsync(StampDirection.In);
        }

        public Task ClockOutAsync()
        {
            return CreateStampAsync(StampDirection.Out);
        }

        private async Task CreateStampAsync(StampDirection direction)
        {
            var mCID = await RetrieveMCIDAsync();

            var createStampParams = new Dictionary<string, string>()
            {
                ["verso"] = direction == StampDirection.In ? "E" : "U",
                ["causale"] = string.Empty,
                ["m_cID"] = mCID,
            };

            var response = await httpClient.PostAsync(stampRoute, new FormUrlEncodedContent(createStampParams));
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();
            if (!payload.Contains("Ok routine eseguita.") && !payload.Contains("Ok Routine wurde ausgeführt"))
            {
                throw new Exception("Failed to stamp ");
            }
        }

        public async Task<string> RetrieveMCIDAsync()
        {
            var findMCID = new Dictionary<string, string>()
            {
                ["containerCode"] = "MYDESK",
                ["currentPageCode"] = "209",
            };

            var response = await httpClient.PostAsync(mCIDRoute, new FormUrlEncodedContent(findMCID));
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync();
            var candidate = Regex.Match(payload, "this.splinker10.m_cID='(.+?)';");

            if (!candidate.Success)
            {
                throw new Exception();
            }

            return candidate.Groups[1].Value;
        }
    }
};