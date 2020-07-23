using BarRaider.SdTools;
using ChatPager.Wrappers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    internal enum SendMethod
    {
        GET,
        POST,
        PUT,
        POST_QUERY_PARAMS
    }

    public class TwitchComm : IDisposable
    {
        #region Private Members

        private const string TWITCH_KRAKEN_URI_PREFIX = "https://api.twitch.tv/kraken";
        private const string TWITCH_HELIX_URI_PREFIX = "https://api.twitch.tv/helix";
        private const string TWITCH_ACCEPT_HEADER = "application/vnd.twitchtv.v5+json";
        private const string TWITCH_URI_MY_STREAM_INFO = "/streams/{0}";
        private const string TWITCH_URI_CHANNEL_INFO = "/streams";
        private const string TWITCH_URL_USER_INFO = "/users";
        private const string TWITCH_URL_GAME_INFO = "/games";
        private const string TWITCH_URL_ACTIVE_STREAMERS = "/streams/followed";
        private const string TWITCH_CREATE_CLIP_URI = "/clips?broadcaster_id=";
        private const string TWITCH_URI_MODIFY_CHANNEL_STATUS = "/channels/{0}";
        private const string TWITCH_CHANNEL_VIEWERS = "https://tmi.twitch.tv/group/user/{0}/chatters";
        private const string TWITCH_URL_MODIFY_TAGS = "/streams/tags?broadcaster_id={0}";

        private TwitchToken token;

        #endregion

        #region Public Methods

        public TwitchComm()
        {
            TwitchTokenManager.Instance.TokensChanged += Instance_TokensChanged;
            token = TwitchTokenManager.Instance.GetToken();
        }

        public void Dispose()
        {
            TwitchTokenManager.Instance.TokensChanged -= Instance_TokensChanged;
        }
       
        public async Task<TwitchChannelInfo> GetChannelInfo(string channelName)
        {
            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("user_login", channelName)
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URI_CHANNEL_INFO, SendMethod.GET, kvp, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    if (json["data"].HasValues)
                    {
                        return json["data"][0].ToObject<TwitchChannelInfo>();
                    }                    
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetChannelInfo Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetChannelInfo Fetch Failed. Response: {response.StatusCode} Reason: {response.ReasonPhrase}");
            }
            return null;
        }

        public async Task<TwitchUserInfo> GetUserInfo(string userLogin)
        {
            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("login", userLogin)
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URL_USER_INFO, SendMethod.GET, kvp, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    if (json["data"].HasValues)
                    {
                        return json["data"][0].ToObject<TwitchUserInfo>();
                    }

                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetUserInfo Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetUserInfo Fetch Failed. Response: {response.StatusCode} Reason: {response.ReasonPhrase}");
            }
            return null;
        }
        
        public async Task<TwitchChannelViewers> GetChannelViewers(string channel)
        {
            string url = TWITCH_CHANNEL_VIEWERS.Replace("{0}", channel);
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetChannelViewers failed - StatusCode: {response.StatusCode}");
                    return null;
                }

                string body = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(body);
                var viewers = json.ToObject<TwitchChannelViewers>();
                viewers.LastUpdated = DateTime.Now;
                return viewers;
            }
        }

        public async Task<TwitchActiveStreamer[]> GetActiveStreamers()
        {
            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("limit", 100.ToString())
            };

            HttpResponseMessage response = await TwitchKrakenQuery(TWITCH_URL_ACTIVE_STREAMERS, SendMethod.GET, kvp, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    if (json["streams"].HasValues)
                    {
                        return json["streams"].ToObject<TwitchActiveStreamer[]>();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetMyStreamInfo Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "GetMyStreamInfo Fetch Failed");
            }
            return null;
        }

        public async Task<TwitchStreamInfo> GetMyStreamInfo()
        {
            string URI = String.Format(TWITCH_URI_MY_STREAM_INFO, TwitchTokenManager.Instance.User.UserId);
            HttpResponseMessage response = await TwitchKrakenQuery(URI, SendMethod.GET, null, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    return json["stream"].ToObject<TwitchStreamInfo>();

                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetMyStreamInfo Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "GetMyStreamInfo Fetch Failed");
            }
            return null;
        }

        public async Task<ClipDetails> CreateClip()
        {
            if (TwitchTokenManager.Instance.User == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Cannot create Twitch clip, User object is null");
                return null;
            }

            string uri = TWITCH_CREATE_CLIP_URI + TwitchTokenManager.Instance.User?.UserId;
            HttpResponseMessage response = await TwitchHelixQuery(uri, SendMethod.POST, null, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    return json["data"][0].ToObject<ClipDetails>();
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"CreateClip Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"CreateClip Fetch Failed. StatusCode: {response.StatusCode}");
            }
            return null;
        }

        public async Task<bool> UpdateChannelStatus(string statusMessage, string currentGame)
        {
            string uri = String.Format(TWITCH_URI_MODIFY_CHANNEL_STATUS, TwitchTokenManager.Instance.User.UserId);
            if (string.IsNullOrEmpty(statusMessage) && string.IsNullOrEmpty(currentGame))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UpdateChannelStatus called with empty status and game");
                return false;
            }

            JObject body = new JObject();
            JObject channelBody = new JObject();
            channelBody.Add("status", statusMessage);
            channelBody.Add("game", currentGame);

            body.Add("channel", channelBody);
            HttpResponseMessage response = await TwitchKrakenQuery(uri, SendMethod.PUT, null, body);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateChannelTags(string[] tagIds)
        {
            string uri = String.Format(TWITCH_URL_MODIFY_TAGS, TwitchTokenManager.Instance.User.UserId);
            if (tagIds == null || tagIds.Length < 1)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UpdateChannelTags called with empty tagIds");
                return false;
            }

            JObject body = new JObject();
            body.Add("tag_ids", JToken.FromObject(tagIds));

            HttpResponseMessage response = await TwitchHelixQuery(uri, SendMethod.PUT, null, body);
            return response.IsSuccessStatusCode;
        }

        public async Task<TwitchGameInfo> GetGameInfo(string gameId)
        {
            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("id", gameId)
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URL_GAME_INFO, SendMethod.GET, kvp, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    if (json["data"].HasValues)
                    {
                        return json["data"][0].ToObject<TwitchGameInfo>();
                    }

                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetGameInfo Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetGameInfo Fetch Failed. Response: {response.StatusCode} Reason: {response.ReasonPhrase}");
            }
            return null;
        }


        #endregion

        #region Private Methods

        internal async Task<TwitchUserDetails> GetUserDetails()
        {
            HttpResponseMessage response = await TwitchKrakenQuery(String.Empty, SendMethod.GET, null, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    TwitchUserDetails userDetails = json["token"].ToObject<TwitchUserDetails>();
                    //userDetails = new TwitchUserDetails() { UserName = "KayRaid", UserId = "86502273" };
                    return userDetails;
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetUserDetails Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "GetUserDetails Fetch Failed");
            }
            return null;
        }

        #region Twitch v5 (Kraken API)

        internal async Task<HttpResponseMessage> TwitchKrakenQuery(string uriPath, SendMethod sendMethod, List<KeyValuePair<string, string>> optionalContent, JObject body)
        {
            try
            {
                if (token == null || String.IsNullOrEmpty(token.Token))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "TwitchQuery called without a valid token");
                    return new HttpResponseMessage() { StatusCode = HttpStatusCode.Conflict };
                }

                HttpResponseMessage response = await TwitchKrakenQueryInternal(uriPath, sendMethod, optionalContent, body);
                if (response == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchQueryInternal returned null");
                    return response;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchQueryInternal  returned with StatusCode: {response.StatusCode}");
                }

                return response;

            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"TwitchQuery Exception: {ex}");
            }
            return new HttpResponseMessage() { StatusCode = HttpStatusCode.InternalServerError, ReasonPhrase = "TwitchQuery Exception" };
        }

        private async Task<HttpResponseMessage> TwitchKrakenQueryInternal(string uriPath, SendMethod sendMethod, List<KeyValuePair<string, string>> optionalContent, JObject body)
        {
            if (token == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "TwitchQueryInternal called with null token object");
            }

            HttpContent content = null;
            string queryParams = string.Empty;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-ID", TwitchSecret.CLIENT_ID);
            client.DefaultRequestHeaders.Add("Authorization", $"OAuth {token.Token}");
            client.DefaultRequestHeaders.Add("Accept", TWITCH_ACCEPT_HEADER); 
            client.Timeout = new TimeSpan(0, 0, 10);

            switch (sendMethod)
            {
                case SendMethod.POST:
                case SendMethod.POST_QUERY_PARAMS:

                    if (optionalContent != null && sendMethod == SendMethod.POST)
                    {
                        content = new FormUrlEncodedContent(optionalContent);
                    }
                    else if (optionalContent != null && sendMethod == SendMethod.POST_QUERY_PARAMS)
                    {
                        queryParams = "?" + CreateQueryString(optionalContent);
                    }
                    return await client.PostAsync($"{TWITCH_KRAKEN_URI_PREFIX}{uriPath}{queryParams}", content);
                case SendMethod.PUT:
                case SendMethod.GET:
                    if (optionalContent != null)
                    {
                        queryParams = "?" + CreateQueryString(optionalContent);
                    }

                    if (sendMethod == SendMethod.GET)
                    {
                        return await client.GetAsync($"{TWITCH_KRAKEN_URI_PREFIX}{uriPath}{queryParams}");
                    }

                    if (body != null)
                    {
                        content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                    }

                    return await client.PutAsync($"{TWITCH_KRAKEN_URI_PREFIX}{uriPath}{queryParams}", content);
            }
            return null;
        }

        #endregion

        #region Twitch New (Helix) API

        internal async Task<HttpResponseMessage> TwitchHelixQuery(string uriPath, SendMethod sendMethod, List<KeyValuePair<string, string>> optionalContent, JObject body)
        {
            try
            {
                if (token == null || String.IsNullOrEmpty(token.Token))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "TwitchHelixQuery called without a valid token");
                    return new HttpResponseMessage() { StatusCode = HttpStatusCode.Conflict };
                }

                HttpResponseMessage response = await TwitchHelixQueryInternal(uriPath, sendMethod, optionalContent, body);
                if (response == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchHelixQueryInternal returned null");
                    return response;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchHelixQueryInternal returned with StatusCode: {response.StatusCode}");
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Logger.Instance.LogMessage(TracingLevel.WARN, "TwitchHelixQueryInternal returned unauthorized, revoking tokens");
                        TwitchTokenManager.Instance.RevokeToken();
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"TwitchHelixQuery Exception: {ex}");
            }
            return new HttpResponseMessage() { StatusCode = HttpStatusCode.InternalServerError, ReasonPhrase = "TwitchHelixQuery Exception" };
        }

        private async Task<HttpResponseMessage> TwitchHelixQueryInternal(string uriPath, SendMethod sendMethod, List<KeyValuePair<string, string>> optionalContent, JObject body)
        {
            if (token == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "TwitchHelixQueryInternal called with null token object");
            }

            HttpContent content = null;
            string queryParams = string.Empty;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-ID", TwitchSecret.CLIENT_ID);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.Token}");
            client.DefaultRequestHeaders.Add("Accept", TWITCH_ACCEPT_HEADER);
            client.Timeout = new TimeSpan(0, 0, 10);

            switch (sendMethod)
            {
                case SendMethod.POST:
                case SendMethod.POST_QUERY_PARAMS:

                    if (optionalContent != null && sendMethod == SendMethod.POST)
                    {
                        content = new FormUrlEncodedContent(optionalContent);
                    }
                    else if (optionalContent != null && sendMethod == SendMethod.POST_QUERY_PARAMS)
                    {
                        queryParams = "?" + CreateQueryString(optionalContent);
                    }
                    return await client.PostAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}", content);
                case SendMethod.PUT:
                case SendMethod.GET:
                    if (optionalContent != null)
                    {
                        queryParams = "?" + CreateQueryString(optionalContent);
                    }

                    if (sendMethod == SendMethod.GET)
                    {
                        return await client.GetAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}");
                    }

                    if (body != null)
                    {
                        content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                    }

                    return await client.PutAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}", content);
            }
            return null;
        }

        #endregion

        private string CreateQueryString(List<KeyValuePair<string, string>> parameters)
        {
            List<string> paramList = new List<string>();
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    paramList.Add($"{kvp.Key}={kvp.Value}");
                }
            }
            return string.Join("&", paramList);
        }

        private void Instance_TokensChanged(object sender, TwitchTokenEventArgs e)
        {
            token = e.Token;
        }

        #endregion
    }
}
