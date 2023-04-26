using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class DiscordWebhookRequest {

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("embeds")]
        public List<Embed> Embeds { get; set; }

        [JsonProperty("allowed_mentions")]
        public AllowedMention AllowedMentions { get; set; } = new AllowedMention();

        public class Embed {
            [JsonProperty("author")]
            public Author Author { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("color")]
            public int Color { get; set; }

            [JsonProperty("fields")]
            public List<Field> Fields { get; set; }

            [JsonProperty("thumbnail")]
            public Thumbnail Thumbnail { get; set; }

            [JsonProperty("image")]
            public Image Image { get; set; }

            [JsonProperty("footer")]
            public Footer Footer { get; set; }
        }

        public class Author {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("icon_url")]
            public string IconUrl { get; set; }
        }

        public class Field {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("inline")]
            public bool Inline { get; set; }
        }

        public class Footer {
            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("icon_url")]
            public string IconUrl { get; set; }
        }

        public class Image {
            [JsonProperty("url")]
            public string Url { get; set; }
        }

        public class Thumbnail {
            [JsonProperty("url")]
            public string Url { get; set; }
        }

        public class AllowedMention {
            [JsonProperty("parse")]
            public List<string> Parse { get; set; } = new List<string>() { "roles", "users", "everyone" };
        }
    }
}
