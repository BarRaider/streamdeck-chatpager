using BarRaider.SdTools;
using ChatPager.Twitch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPager.Actions
{
    public abstract class ActionBase : PluginBase
    {
        protected class PluginSettingsBase
        {
            [JsonProperty(PropertyName = "tokenExists")]
            public bool TokenExists { get; set; }
        }

        #region Protected Members

        protected PluginSettingsBase settings;

        #endregion

        public ActionBase(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Connection.StreamDeckConnection.OnSendToPlugin += StreamDeckConnection_OnSendToPlugin;
            TwitchTokenManager.Instance.TokenStatusChanged += Instance_TokenStatusChanged;
        }

        #region Public Methods

        public override void Dispose()
        {
            TwitchTokenManager.Instance.TokenStatusChanged -= Instance_TokenStatusChanged;
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Base Destructor called");
        }


        protected virtual Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion

        #region Private Methods


        // Used to register and revoke token
        private async void StreamDeckConnection_OnSendToPlugin(object sender, streamdeck_client_csharp.StreamDeckEventReceivedEventArgs<streamdeck_client_csharp.Events.SendToPluginEvent> e)
        {
            var payload = e.Event.Payload;
            if (Connection.ContextId != e.Event.Context)
            {
                return;
            }

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLower())
                {
                    case "updateapproval":
                        string approvalCode = (string)payload["approvalCode"];
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Requesting approval with code: {approvalCode}");
                        TwitchTokenManager.Instance.SetToken(new TwitchToken() { Token = approvalCode, TokenLastRefresh = DateTime.Now });
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"RefreshToken completed. Token Exists: {TwitchTokenManager.Instance.TokenExists}");
                        break;
                    case "resetplugin":
                        Logger.Instance.LogMessage(TracingLevel.WARN, $"ResetPlugin called. Tokens are cleared");
                        TwitchTokenManager.Instance.RevokeToken();
                        await SaveSettings();
                        break;
                }
            }
        }

        private async void Instance_TokenStatusChanged(object sender, EventArgs e)
        {
            settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            await SaveSettings();
        }

        #endregion
    }
}
