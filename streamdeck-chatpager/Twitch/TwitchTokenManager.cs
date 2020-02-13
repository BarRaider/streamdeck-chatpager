﻿using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{
    public class TwitchTokenManager
    {
        #region Private Members
        private const string TOKEN_FILE = "twitch.dat";

        private static TwitchTokenManager instance = null;
        private static readonly object objLock = new object();

        private TwitchToken token;
        private TwitchUserDetails userDetails;
        private object lockObj = new object();
        private TwitchGlobalSettings global = null;

        #endregion

        #region Constructors

        public static TwitchTokenManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new TwitchTokenManager();
                    }
                    return instance;
                }
            }
        }

        private TwitchTokenManager()
        {
            GlobalSettingsManager.Instance.OnReceivedGlobalSettings += Instance_OnReceivedGlobalSettings;
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        #endregion

        #region Public Members

        internal event EventHandler<TwitchTokenEventArgs> TokensChanged;
        public event EventHandler TokenStatusChanged;

        public TwitchUserDetails User
        {
            get
            {
                if (userDetails == null && token != null)
                {
                    lock (lockObj)
                    {
                        if (userDetails == null && token != null)
                        {
                            LoadUserDetails();
                        }
                    }
                    
                }
                return userDetails;
            }
        }

        public bool TokenExists
        {
            get
            {
                return token != null && !(String.IsNullOrWhiteSpace(token.Token));
            }
        }


        #endregion

        #region Public Methods

        public void SetToken(TwitchToken token)
        {
            if (token != null && (this.token == null || token.TokenLastRefresh > this.token.TokenLastRefresh))
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"New token set Token Size: {token?.Token?.Length}");
                this.token = token;
                if (ValidateToken())
                {
                    SaveToken();
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Could not validate token with twitch");
                    this.token = null;
                }
            }
            RaiseTokenChanged();
        }

        internal TwitchToken GetToken()
        {
            if (token != null)
            {
                return new TwitchToken() { Token = token.Token, TokenLastRefresh = token.TokenLastRefresh };
            }
            return null;
        }

        public void RevokeToken()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "RevokeToken Called");
            this.token = null;
            SaveToken();
            RaiseTokenChanged();
        }

        #endregion

        #region Private Methods

        private void LoadToken(TwitchToken globalToken)
        {
            try
            {
                if (globalToken == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to load tokens, deserialized globalToken is null");
                    return;
                }

                if (token != null && token.Token == globalToken.Token && token.TokenLastRefresh == globalToken.TokenLastRefresh)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"LoadToken called for EXISTING token. Token Size: {token?.Token?.Length}");
                    return;
                }

                token = new TwitchToken()
                {
                    Token = globalToken.Token,
                    TokenLastRefresh = globalToken.TokenLastRefresh
                };

                Logger.Instance.LogMessage(TracingLevel.INFO, $"Token initialized. Last refresh date was: {token.TokenLastRefresh} Token Size: {token?.Token?.Length}");
                if (String.IsNullOrWhiteSpace(token.Token))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "Existing token in Global Settings is empty!");
                }
                RaiseTokenChanged();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Exception loading tokens: {ex}");
            }
        }

        private void SaveToken()
        {
            try
            {
                if (global == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "Global Settings is null, creating new instance");
                    global = new TwitchGlobalSettings();
                }

                // Set token in Global Settings
                if (token == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "Saving NULL token to Global Settings");
                    global.Token = null;
                }
                else
                {
                    global.Token = new TwitchToken()
                    {
                        Token = token.Token,
                        TokenLastRefresh = token.TokenLastRefresh
                    };
                    Logger.Instance.LogMessage(TracingLevel.INFO, "TwitchTokenManager saving token to global");
                }

                GlobalSettingsManager.Instance.SetGlobalSettings(JObject.FromObject(global));
                Logger.Instance.LogMessage(TracingLevel.INFO, $"New token saved. Last refresh date was: {token?.TokenLastRefresh} Token Size: {token?.Token?.Length}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Exception saving tokens: {ex}");
            }
        }

        private void LoadUserDetails()
        {
            TwitchComm comm = new TwitchComm();
            userDetails = Task.Run(() => comm.GetUserDetails()).GetAwaiter().GetResult();
        }

        private void RaiseTokenChanged()
        {

            TokenStatusChanged?.Invoke(this, EventArgs.Empty);
            if (token != null)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"TwitchTokenManager raising new token change. Token Size: {token?.Token?.Length}");
                TokensChanged?.Invoke(this, new TwitchTokenEventArgs(new TwitchToken() { Token = token.Token, TokenLastRefresh = token.TokenLastRefresh }));
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, "TwitchTokenManager raising EMPTY token change");
                TokensChanged?.Invoke(this, new TwitchTokenEventArgs(null));
            }
        }

        private bool ValidateToken()
        {
            LoadUserDetails();
            return userDetails != null && !String.IsNullOrWhiteSpace(userDetails.UserName);
        }

        private void Instance_OnReceivedGlobalSettings(object sender, ReceivedGlobalSettingsPayload payload)
        {
            if (payload?.Settings != null && payload.Settings.Count > 0)
            {
                global = payload.Settings.ToObject<TwitchGlobalSettings>();
                LoadToken(global.Token);
            }
        }

        #endregion
    }
}
