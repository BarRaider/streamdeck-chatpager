using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatPager.Backend
{
    public static class HelperFunctions
    {
        private const int PREVIEW_IMAGE_HEIGHT_PIXELS = 144;
        private const int PREVIEW_IMAGE_WIDTH_PIXELS = 144;
        private const string PREVIEW_IMAGE_WIDTH_TOKEN = "{width}";
        private const string PREVIEW_IMAGE_HEIGHT_TOKEN = "{height}";

        public static Task<Bitmap> FetchImage(string imageUrl)
        {
            return Task.Run(() =>
            {


                try
                {
                    if (String.IsNullOrEmpty(imageUrl))
                    {
                        return null;
                    }

                    using (WebClient client = new WebClient())
                    {
                        using (Stream stream = client.OpenRead(imageUrl))
                        {
                            Bitmap image = new Bitmap(stream);
                            return image;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to fetch image: {imageUrl} {ex}");
                }
                return null;
            });
        }

        public static string GenerateUrlFromGenericImageUrl(string genericImageUrl)
        {
            if (string.IsNullOrEmpty(genericImageUrl))
            {
                return null;
            }

            return genericImageUrl.Replace(PREVIEW_IMAGE_WIDTH_TOKEN, PREVIEW_IMAGE_WIDTH_PIXELS.ToString()).Replace(PREVIEW_IMAGE_HEIGHT_TOKEN, PREVIEW_IMAGE_HEIGHT_PIXELS.ToString());
        }
    }
}
