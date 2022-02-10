using BarRaider.SdTools;
using ChatPager.Backend;
using ChatPager.Wrappers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    internal enum SendMethod
    {
        GET = 0,
        POST = 1,
        PUT = 2,
        DELETE = 3,
        POST_QUERY_PARAMS = 4,
        PATCH = 5
    }

    public class TwitchComm : IDisposable
    {
        #region Private Members

        private const string TWITCH_HELIX_URI_PREFIX = "https://api.twitch.tv/helix";
        private const string TWITCH_ACCEPT_HEADER = "application/vnd.twitchtv.v5+json";
        private const string TWITCH_URI_CHANNEL_INFO = "/streams";
        private const string TWITCH_URL_USER_INFO = "/users";
        private const string TWITCH_URL_GAME_INFO = "/games";
        private const string TWITCH_URL_ACTIVE_STREAMERS = "/streams/followed";
        private const string TWITCH_CREATE_CLIP_URI = "/clips?broadcaster_id=";
        private const string TWITCH_CREATE_MARKER_URI = "/streams/markers";
        private const string TWITCH_URI_MODIFY_CHANNEL_STATUS = "/channels?broadcaster_id={0}";
        private const string TWITCH_URI_RUN_COMMERCIAL = "/channels/commercial";
        private const string TWITCH_CHANNEL_VIEWERS = "https://tmi.twitch.tv/group/user/{0}/chatters";
        private const string TWITCH_URL_MODIFY_TAGS = "/streams/tags?broadcaster_id={0}";

        private readonly int[] VALID_AD_LENGTHS = new int[] { 30, 60, 90, 120, 150, 180 };

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
            if (string.IsNullOrEmpty(channel))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} GetChannelViewers called but channel is null!");
                return null;
            }

            string url = TWITCH_CHANNEL_VIEWERS.Replace("{0}", channel);
            using (var client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetChannelViewers failed - StatusCode: {response.StatusCode} for Channel {channel}");
                    if (channel != channel.Trim().ToLowerInvariant())
                    {
                        return await GetChannelViewers(channel.Trim().ToLowerInvariant());
                    }
                    return null;
                }

                string body = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(body);
                var viewers = json.ToObject<TwitchChannelViewers>();
                viewers.LastUpdated = DateTime.Now;
                return viewers;
            }
        }

        public async Task<TwitchChannelInfo[]> GetActiveStreamers()
        {
            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("user_id", TwitchTokenManager.Instance.User.UserId)
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URL_ACTIVE_STREAMERS, SendMethod.GET, kvp, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    if (json["data"].HasValues)
                    {
                        return json["data"].ToObject<TwitchChannelInfo[]>();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetActiveStreamers Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetActiveStreamers Fetch Failed. Status: {response.StatusCode} Reason: {response.ReasonPhrase}");
            }
            return null;
        }

        public async Task<TwitchChannelInfo> GetMyStreamInfo()
        {
            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("user_id", TwitchTokenManager.Instance.User.UserId)
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
                        return json["data"].ToObject<TwitchChannelInfo[]>().FirstOrDefault();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetMyStreamInfo Exception: {ex}");
                }
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetMyStreamInfo Fetch Failed. Status: {response.StatusCode} Reason: {response.ReasonPhrase}");
            }
            return null;
        }

        public async Task<ClipDetails> CreateClip(string userName)
        {
            if (TwitchTokenManager.Instance.User == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Cannot create Twitch clip, User object is null");
                return null;
            }

            string userId = TwitchTokenManager.Instance.User?.UserId;
            if (userName != TwitchTokenManager.Instance.User.UserName)
            {
                // Different user, get the UserId
                var userInfo = await GetUserInfo(userName);
                if (userInfo == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Cannot create Twitch clip, Could not retreive info on {userName}");
                    return null;
                }
                userId = userInfo.UserId;
            }

            string uri = TWITCH_CREATE_CLIP_URI + userId;
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

        public async Task<bool> UpdateChannelStatus(string statusMessage, int? gameId, string language)
        {
            string uri = String.Format(TWITCH_URI_MODIFY_CHANNEL_STATUS, TwitchTokenManager.Instance.User.UserId);
            if (string.IsNullOrEmpty(statusMessage) && string.IsNullOrEmpty(language) && !gameId.HasValue)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UpdateChannelStatus called with empty status, game and language");
                return false;
            }

            JObject body = new JObject();
            if (!String.IsNullOrEmpty(statusMessage))
            {
                body.Add("title", statusMessage);
            }

            if (gameId.HasValue)
            {
                body.Add("game_id", gameId.Value);
            }

            if (!String.IsNullOrWhiteSpace(language))
            {
                body.Add("broadcaster_language", language);
            }

            HttpResponseMessage response = await TwitchHelixQuery(uri, SendMethod.PATCH, null, body);
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

            JObject body = new JObject
            {
                { "tag_ids", JToken.FromObject(tagIds) }
            };

            HttpResponseMessage response = await TwitchHelixQuery(uri, SendMethod.PUT, null, body);

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} UpdateChannelTags return Forbidden - Check if an invalid tag (such as 'Language' tag) was used");
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<TwitchGameInfo> GetGameInfo(string gameName)
        {
            if (Int32.TryParse(gameName, out int gameId)) // Actually a GameId
            {
                return await GetGameInfo(gameId);
            }
            else
            {
                var kvp = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("name", gameName)
                };

                return await InternalGetGameInfo(kvp);
            }

        }

        public async Task<TwitchGameInfo> GetGameInfo(int gameId)
        {
            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("id", gameId.ToString())
            };

            return await InternalGetGameInfo(kvp);
        }

        private async Task<TwitchGameInfo> InternalGetGameInfo(List<KeyValuePair<string, string>> kvp)
        {
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
                string res = await response.Content.ReadAsStringAsync();
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetGameInfo Fetch Failed. Response: {response.StatusCode} Reason: {response.ReasonPhrase} Error: {res}");
            }
            return null;
        }

        public async Task<AdDetails> RunAd(int adLength)
        {
            if (!VALID_AD_LENGTHS.Contains(adLength))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"RunAd called with invalid Ad Length: {adLength}");
                return null;
            }

            string userId = TwitchTokenManager.Instance.User?.UserId;
            JObject req = new JObject()
            {
                { "broadcaster_id", userId },
                { "length", adLength }
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URI_RUN_COMMERCIAL, SendMethod.POST, null, req);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    if (json != null && json["data"].HasValues)
                    {
                        AdDetails details = json["data"][0].ToObject<AdDetails>();
                        if (details != null)
                        {
                            if (!String.IsNullOrEmpty(details.ErrorMessage))
                            {
                                Logger.Instance.LogMessage(TracingLevel.WARN, $"RunAd returned message: {details.ErrorMessage}");
                            }
                            details.CalculateAdInfo();
                        }
                        return details;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"RunAd Exception: {ex}");
                }
            }
            else
            {
                string res = await response.Content.ReadAsStringAsync();
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"RunAd Failed. StatusCode: {response.StatusCode} Error: {res}");
            }
            return null;
        }

        public async Task<bool> CreateMarker(string channel)
        {
            if (TwitchTokenManager.Instance.User == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "Cannot create marker, User object is null");
                return false;
            }

            string userId = TwitchTokenManager.Instance.User?.UserId;
            if (channel != TwitchTokenManager.Instance.User.UserName)
            {
                // Different user, get the UserId
                var userInfo = await GetUserInfo(channel);
                if (userInfo == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Cannot create marker, Could not retreive info on {channel}");
                    return false;
                }
                userId = userInfo.UserId;
            }

            JObject req = new JObject
            {
                { "user_id", userId },
                { "description", "Marker created from BarRaider's Twitch Tools"}
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_CREATE_MARKER_URI, SendMethod.POST, null, req);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Private Methods

        internal async Task<TwitchUserDetails> GetUserDetails()
        {
            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URL_USER_INFO, SendMethod.GET, null, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    if (json["data"].HasValues)
                    {
                        return json["data"][0].ToObject<TwitchUserDetails>();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetUserDetails Exception: {ex}");
                }
            }
            else
            {
                string res = await response.Content.ReadAsStringAsync();
                Logger.Instance.LogMessage(TracingLevel.WARN, $"GetUserDetails Fetch Failed. Error {res}");
            }
            return null;
        }

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
                    string res = await response.Content.ReadAsStringAsync();
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"TwitchHelixQueryInternal returned with StatusCode: {response.StatusCode} Error: {res}");
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
                    else if (body != null)
                    {
                        content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                    }
                    return await client.PostAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}", content);
                case SendMethod.PUT:
                case SendMethod.GET:
                case SendMethod.PATCH:
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

                    if (sendMethod == SendMethod.PATCH)
                    {
                        return await client.PatchAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}", content);
                    }
                    else
                    {
                        return await client.PutAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}", content);
                    }
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
