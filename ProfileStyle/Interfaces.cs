using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProfileStyle;

internal sealed record ProfileStyleConfig {
    [JsonInclude]
    public CategoryConfig Avatars { get; set; } = new();

    [JsonInclude]
    public CategoryConfig AvatarFrames { get; set; } = new();

    [JsonInclude]
    public CategoryConfig MiniBackgrounds { get; set; } = new();

    [JsonInclude]
    public CategoryBackgroundConfig Backgrounds { get; set; } = new();

    [JsonInclude]
    public CategoryConfig SpecialProfiles { get; set; } = new();

    internal record CategoryConfig {
        [JsonInclude]
        public bool Enable { get; set; }

        [JsonInclude]
        public List<ulong> Items { get; set; } = [];

        [JsonInclude]
        public uint Timeout { get; set; } = 60;

        [JsonConstructor]
        public CategoryConfig() { }
    }

    internal sealed record CategoryBackgroundConfig : CategoryConfig {
        [JsonInclude]
        public List<ulong> Showcases { get; set; } = [];

        [JsonConstructor]
        public CategoryBackgroundConfig() { }
    }

    [JsonConstructor]
    public ProfileStyleConfig() { }
}

internal sealed record GetCommunityInventoryResponse {
    [JsonPropertyName("response")]
    public ResponseData? Response { get; set; }

    internal sealed record ResponseData {
        [JsonPropertyName("items")]
        public List<Item>? Items { get; set; }

        internal sealed record Item {
            [JsonPropertyName("communityitemid")]
            public ulong CommunityItemId { get; set; }

            [JsonPropertyName("appid")]
            public uint AppId { get; set; }
        }
    }
}

internal sealed record GetProfileItemsOwnedResponse {
    [JsonPropertyName("response")]
    public ResponseData? Response { get; set; }

    internal sealed record ResponseData {
        [JsonPropertyName("animated_avatars")]
        public List<ItemData>? Avatars { get; set; }

        [JsonPropertyName("avatar_frames")]
        public List<ItemData>? AvatarFrames { get; set; }

        [JsonPropertyName("mini_profile_backgrounds")]
        public List<ItemData>? MiniBackgrounds { get; set; }

        [JsonPropertyName("profile_backgrounds")]
        public List<ItemData>? Backgrounds { get; set; }

        [JsonPropertyName("profile_modifiers")]
        public List<ItemData>? SpecialProfiles { get; set; }

        public sealed record ItemData {
            [JsonPropertyName("communityitemid")]
            public string? CommunityItemId { get; set; }

            [JsonPropertyName("item_title")]
            public string? ItemTitle { get; set; }
        }
    }
}

internal sealed record GetProfileCustomizationResponse {
    [JsonPropertyName("response")]
    public ResponseData? Response { get; set; }

    internal sealed record ResponseData {
        [JsonPropertyName("customizations")]
        public List<Customization>? Customizations { get; set; }

        internal sealed record Customization {
            [JsonPropertyName("customization_type")]
            public uint CustomizationType { get; set; }

            [JsonPropertyName("purchaseid")]
            public string? PurchaseId { get; set; }

            [JsonPropertyName("customization_style")]
            public uint CustomizationStyle { get; set; }

            [JsonPropertyName("slots")]
            public List<SlotData>? Slots { get; set; }

            internal sealed record SlotData {
                [JsonPropertyName("slot")]
                public uint Slot { get; set; }

                [JsonExtensionData]
                public Dictionary<string, JsonElement>? Data { get; set; }
            }
        }
    }
}

internal sealed record ChangeShowcaseResponse {
    [JsonPropertyName("success")]
    public uint Success { get; set; }

    [JsonPropertyName("errmsg")]
    public string? ErrMsg { get; set; }
}
