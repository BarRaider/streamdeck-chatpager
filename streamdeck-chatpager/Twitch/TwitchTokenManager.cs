using BarRaider.SdTools;
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
            LoadToken();
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
                Logger.Instance.LogMessage(TracingLevel.INFO, "New token set");
                this.token = token;
                if (ValidateToken())
                {
                    SaveToken();
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "Could not validate token with twitch");
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
            RaiseTokenChanged();
        }

        #endregion

        #region Private Methods

        private void LoadToken()
        {
            try
            {
                string fileName = Path.Combine(System.AppContext.BaseDirectory, TOKEN_FILE);
                if (File.Exists(fileName))
                {
                    using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        var formatter = new BinaryFormatter();
                        token = (TwitchToken)formatter.Deserialize(stream);
                        if (token == null)
                        {
                            Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to load tokens, deserialized token is null");
                            return;
                        }
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Token initialized. Last refresh date was: {token.TokenLastRefresh}");
                    }
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Failed to load tokens, token file does not exist: {fileName}");
                }
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
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(Path.Combine(System.AppContext.BaseDirectory, TOKEN_FILE), FileMode.Create, FileAccess.Write))
                {

                    formatter.Serialize(stream, token);
                    stream.Close();
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"New token saved. Last refresh date was: {token.TokenLastRefresh}");
                }
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
                TokensChanged?.Invoke(this, new TwitchTokenEventArgs(new TwitchToken() { Token = token.Token, TokenLastRefresh = token.TokenLastRefresh }));
            }
            else
            {
                TokensChanged?.Invoke(this, new TwitchTokenEventArgs(null));
            }
        }

        private bool ValidateToken()
        {
            LoadUserDetails();
            return userDetails != null && !String.IsNullOrWhiteSpace(userDetails.UserName);
        }

        #endregion
    }
}
