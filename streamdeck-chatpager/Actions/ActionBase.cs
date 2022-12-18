using BarRaider.SdTools;
using ChatPager.Twitch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatPager.Actions
{
    public abstract class ActionBase : KeypadBase
    {
        protected class PluginSettingsBase
        {
            [JsonProperty(PropertyName = "tokenExists")]
            public bool TokenExists { get; set; }
        }

        #region Protected Members

        protected PluginSettingsBase settings;
        protected bool baseHandledOnTick = false;

        #endregion

        public ActionBase(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Connection.OnSendToPlugin += Connection_OnSendToPlugin;
            TwitchTokenManager.Instance.TokenStatusChanged += Instance_TokenStatusChanged;
        }

        #region Public Methods

        public override void Dispose()
        {
            TwitchTokenManager.Instance.TokenStatusChanged -= Instance_TokenStatusChanged;
            Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Base Destructor called");
        }

        public async override void OnTick()
        {
            if (!settings.TokenExists)
            {
                baseHandledOnTick = true;
                await Connection.SetImageAsync(Properties.Settings.Default.TwitchNoToken).ConfigureAwait(false);
                return;
            }
        }

        protected virtual Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        #endregion

        #region Private Methods


        // Used to register and revoke token
        private async void Connection_OnSendToPlugin(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.SendToPlugin> e)
        {
            var payload = e.Event.Payload;

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLowerInvariant())
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
                    case "ping":
                        await SendPongToPI();
                        break;
                }
            }
        }

        private async void Instance_TokenStatusChanged(object sender, EventArgs e)
        {
            settings.TokenExists = TwitchTokenManager.Instance.TokenExists;
            if (settings.TokenExists)
            {
                await Connection.SetImageAsync((String)null);
            }
            await SaveSettings();
        }
        private async Task SendPongToPI()
        {
            JObject obj = new JObject()
                {
                    new JProperty("PONG", new JObject() {
                                                    new JProperty("datetime", DateTime.Now)
                    })
                };
            await Connection.SendToPropertyInspectorAsync(obj);
        }

        #endregion
    }
}
