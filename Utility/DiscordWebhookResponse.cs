using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    internal class DiscordWebhookResponse {
        /*
         JSON for this class:
        {
    "id": "1148920614422323301",
    "type": 0,
    "content": "test",
    "channel_id": "1009928962257997925",
    "author": {
        "id": "1035343625829236786",
        "username": "Webhook-Test",
        "avatar": null,
        "discriminator": "0000",
        "public_flags": 0,
        "flags": 0,
        "bot": true
    },
    "attachments": [],
    "embeds": [],
    "mentions": [],
    "mention_roles": [],
    "pinned": false,
    "mention_everyone": false,
    "tts": false,
    "timestamp": "2023-09-06T10:00:20.391000+00:00",
    "edited_timestamp": null,
    "flags": 0,
    "components": [],
    "webhook_id": "1035343625829236786"
}
         */

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }

        [JsonProperty("author")]
        public WebhookAuthor Author { get; set; }

        [JsonProperty("attachments")]
        public List<object> Attachments { get; set; }

        [JsonProperty("embeds")]
        public List<DiscordWebhookRequest.Embed> Embeds { get; set; }

        [JsonProperty("mentions")]
        public List<object> Mentions { get; set; }

        [JsonProperty("mention_roles")]
        public List<object> MentionRoles { get; set; }

        [JsonProperty("pinned")]
        public bool Pinned { get; set; }

        [JsonProperty("mention_everyone")]
        public bool MentionEveryone { get; set; }

        [JsonProperty("tts")]
        public bool Tts { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("edited_timestamp")]
        public object EditedTimestamp { get; set; }

        [JsonProperty("flags")]
        public int Flags { get; set; }

        [JsonProperty("components")]
        public List<object> Components { get; set; }

        [JsonProperty("webhook_id")]
        public string WebhookId { get; set; }


        public class WebhookAuthor {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("avatar")]
            public object Avatar { get; set; }

            [JsonProperty("discriminator")]
            public string Discriminator { get; set; }

            [JsonProperty("public_flags")]
            public int PublicFlags { get; set; }

            [JsonProperty("flags")]
            public int Flags { get; set; }

            [JsonProperty("bot")]
            public bool Bot { get; set; }
        }
    }
}
