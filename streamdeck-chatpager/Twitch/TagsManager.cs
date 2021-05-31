using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Twitch
{

    public class TagsManager
    {
        #region Private Members
        private const string TAGS_LIST_URL = "https://barraider.com/resources/twitch_tags.csv";

        private static TagsManager instance = null;
        private static readonly object objLock = new object();

        private Dictionary<string, string> dicTags;

        #endregion

        #region Constructors

        public static TagsManager Instance
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
                        instance = new TagsManager();
                    }
                    return instance;
                }
            }
        }

        private TagsManager()
        {
        }

        #endregion

        #region Public Methods

        public async Task<string[]> GetTagIdsFromTagNames(string[] tagNames)
        {
            if (dicTags == null)
            {
                if (!await PopulateTwitchTags())
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} Failed to populate twitch tags!");
                    return null;
                }
            }

            List<string> tagIds = new List<string>();
            foreach (string tagName in tagNames)
            {
                if (!dicTags.ContainsKey(tagName.ToLowerInvariant()))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Tag not found: {tagName}");
                    continue;
                }
                tagIds.Add(dicTags[tagName.ToLowerInvariant()]);
            }

            return tagIds.ToArray();
        }

        #endregion

        #region Private Methods

        private async Task<bool> PopulateTwitchTags()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(TAGS_LIST_URL);
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"PopulateTwitchTags failed - StatusCode: {response.StatusCode}");
                        return false;
                    }

                    string body = await response.Content.ReadAsStringAsync();
                    dicTags = new Dictionary<string, string>();

                    string[] lines = body.Split('\n');

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        var tag = line.Split(',');
                        if (tag.Length == 2)
                        {
                            dicTags[tag[0].ToLowerInvariant()] = tag[1];
                        }
                        else
                        {
                            Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid Tag Line: {line}");
                        }
                    }

                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} PopulateTwitchTags: Received {dicTags.Keys.Count} tags");
                }
                return true;
            }
            catch (Exception ex)
            {

                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} PopulateTwitchTags Exception {ex}");
            }
            return false;
        }

            #endregion
        }

}
