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
        private const string TWITCH_URI_SHIELD_MODE = "/moderation/shield_mode";
        private const string TWITCH_URI_RAIDS = "/raids";
        private const string TWITCH_URI_BANS = "/moderation/bans";

        private readonly int[] VALID_AD_LENGTHS = new int[] { 30, 60, 90, 120, 150, 180 };

        private TwitchToken token;

        #endregion

        #region Constructor/Destructor

        public TwitchComm()
        {
            TwitchTokenManager.Instance.TokensChanged += Instance_TokensChanged;
            token = TwitchTokenManager.Instance.GetToken();
        }

        public void Dispose()
        {
            TwitchTokenManager.Instance.TokensChanged -= Instance_TokensChanged;
        }

        #endregion

        #region User/Channel Info

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


        #endregion

        #region Game Info

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

        #endregion

        #region Active Viewers/Streamers

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

        #endregion

        #region Create Clip/Marker

        public async Task<ClipDetails> CreateClip(string userName)
        {
            (string broadcasterId, string moderatorId) = await GetBroadcasterAndModeratorIds(userName);

            if (string.IsNullOrEmpty(broadcasterId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"CreateClip failed tp get broadcaster id");
                return null;
            }

            string uri = TWITCH_CREATE_CLIP_URI + broadcasterId;
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

        public async Task<bool> CreateMarker(string channel, string description)
        {
            (string broadcasterId, string moderatorId) = await GetBroadcasterAndModeratorIds(channel);

            if (string.IsNullOrEmpty(broadcasterId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"CreateMarker failed tp get broadcaster ids");
                return false;
            }

            if (String.IsNullOrEmpty(description))
            {
                description = "Created by BarRaider's Twitch Tools";
            }

            JObject req = new JObject
            {
                { "user_id", broadcasterId },
                { "description", description}
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_CREATE_MARKER_URI, SendMethod.POST, null, req);
            return response.IsSuccessStatusCode;
        }


        #endregion

        #region Ads

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

        #endregion

        #region Twitch Shield

        public async Task<bool?> IsShieldEnabled(string channel)
        {
            (string broadcasterId, string moderatorId) = await GetBroadcasterAndModeratorIds(channel);

            if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(moderatorId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"IsShieldEnabled failed tp get broadcaster/moderator ids");
                return null;
            }

            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("broadcaster_id", broadcasterId),
                new KeyValuePair<string, string>("moderator_id", moderatorId),
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URI_SHIELD_MODE, SendMethod.GET, kvp, null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    string body = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(body);
                    if (json["data"].HasValues)
                    {
                        return json["data"][0]["is_active"].Value<bool>();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"IsShieldEnabled Exception: {ex}");
                }
            }
            else
            {
                string res = await response.Content.ReadAsStringAsync();
                Logger.Instance.LogMessage(TracingLevel.WARN, $"IsShieldEnabled Fetch Failed. Error {res}");
            }
            return null;
        }

        public async Task<bool> SetShieldStatus(string channel, bool isEnabled)
        {
            (string broadcasterId, string moderatorId) = await GetBroadcasterAndModeratorIds(channel);

            if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(moderatorId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"SetShieldStatus failed tp get broadcaster/moderator ids");
                return false;
            }

            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("broadcaster_id", broadcasterId),
                new KeyValuePair<string, string>("moderator_id", moderatorId),
            };

            JObject req = new JObject
            {
                { "is_active", isEnabled },
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URI_SHIELD_MODE, SendMethod.PUT, kvp, req);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            string res = await response.Content.ReadAsStringAsync();
            Logger.Instance.LogMessage(TracingLevel.WARN, $"SetShieldStatus Failed. Error {res}");
            return false;
        }

        #endregion

        #region Ban/Timeout

        public async Task<bool> BanUser(string channel, string banUserId, int timeoutLength = -1)
        {
            (string broadcasterId, string moderatorId) = await GetBroadcasterAndModeratorIds(channel);

            if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(moderatorId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"BanUser failed to get broadcaster/moderator ids");
                return false;
            }

            if (!Int32.TryParse(banUserId, out int bannedId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"BanUser invalid ban user id: {banUserId}");
                return false;
            }

            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("broadcaster_id", broadcasterId),
                new KeyValuePair<string, string>("moderator_id", moderatorId),
            };

            JObject req = new JObject
            {
                { "user_id", banUserId },
            };

            if (timeoutLength > 0)
            {
                req.Add("duration", timeoutLength);
            }

            JObject data = new JObject
            {
                {"data", req}
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URI_BANS, SendMethod.POST_QUERY_PARAMS, kvp, data);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnbanUser(string channel, string unbanUserId)
        {
            (string broadcasterId, string moderatorId) = await GetBroadcasterAndModeratorIds(channel);

            if (string.IsNullOrEmpty(broadcasterId) || string.IsNullOrEmpty(moderatorId))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UnbanUser failed to get broadcaster/moderator ids");
                return false;
            }

            if (!Int32.TryParse(unbanUserId, out _))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"UnbanUser invalid ban user id: {unbanUserId}");
                return false;
            }

            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("broadcaster_id", broadcasterId),
                new KeyValuePair<string, string>("moderator_id", moderatorId),
                new KeyValuePair<string, string>("user_id", unbanUserId),
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URI_BANS, SendMethod.DELETE, kvp, null);
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Raids

        public async Task<bool> RaidChannel(string channelToRaid)
        {
            if (TwitchTokenManager.Instance.User == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "RaidChannel failed - User object is null");
                return false;
            }

            string broadcasterId = TwitchTokenManager.Instance.User?.UserId;
            var raidChannel = await GetChannelInfo(channelToRaid);
            if (raidChannel == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"RaidChannel failed - Couldn't get channel info for {channelToRaid}");
                return false;
            }

            var kvp = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("from_broadcaster_id", broadcasterId),
                new KeyValuePair<string, string>("to_broadcaster_id", raidChannel.UserId),
            };

            HttpResponseMessage response = await TwitchHelixQuery(TWITCH_URI_RAIDS, SendMethod.POST_QUERY_PARAMS, kvp, null);
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

        internal async Task<(string broadcasterId, string moderatorId)> GetBroadcasterAndModeratorIds(string channel)
        {
            if (TwitchTokenManager.Instance.User == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "GetBroadcasterAndModeratorIds failed - User object is null");
                return (null, null);
            }

            string broadcasterId = TwitchTokenManager.Instance.User?.UserId;
            if (!String.IsNullOrEmpty(channel) && channel != TwitchTokenManager.Instance.User.UserName)
            {
                // Different user, get the UserId
                var userInfo = await GetUserInfo(channel);
                if (userInfo == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"GetBroadcasterAndModeratorIds failed - Could not retreive info on {channel}");
                    return (null, null);
                }
                broadcasterId = userInfo.UserId;
            }

            return (broadcasterId, TwitchTokenManager.Instance.User?.UserId);
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

                    if (body != null)
                    {
                        content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                    }

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
                case SendMethod.PATCH:
                case SendMethod.DELETE:
                    if (optionalContent != null)
                    {
                        queryParams = "?" + CreateQueryString(optionalContent);
                    }

                    if (sendMethod == SendMethod.GET)
                    {
                        return await client.GetAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}");
                    }
                    else if (sendMethod == SendMethod.DELETE)
                    {
                        return await client.DeleteAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}");
                    }

                    if (body != null)
                    {
                        content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");
                    }

                    if (sendMethod == SendMethod.PATCH)
                    {
                        return await client.PatchAsync($"{TWITCH_HELIX_URI_PREFIX}{uriPath}{queryParams}", content);
                    }
                    else //(sendMethod == SendMethod.PUT)
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
