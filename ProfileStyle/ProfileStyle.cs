﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Web.Responses;

namespace ProfileStyle;

internal sealed partial class ProfileStyle : IGitHubPluginUpdates, IBotModules, IBotCommand2 {
    public string Name => nameof(ProfileStyle);
    public string RepositoryName => "JackieWaltRyan/ProfileStyle";
    public Version Version => typeof(ProfileStyle).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

    public Dictionary<string, ProfileStyleConfig> ProfileStyleConfig = new();
    public Dictionary<string, Dictionary<string, Timer>> ProfileStyleTimers = new();

    public Task OnLoaded() => Task.CompletedTask;

    public async Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
        if (additionalConfigProperties != null) {
            if (ProfileStyleTimers.TryGetValue(bot.BotName, out Dictionary<string, Timer>? dict)) {
                foreach (KeyValuePair<string, Timer> timers in dict) {
                    switch (timers.Key) {
                        case "ChangeAvatar": {
                            await timers.Value.DisposeAsync().ConfigureAwait(false);

                            bot.ArchiLogger.LogGenericInfo("ChangeAvatar Dispose.");

                            break;
                        }

                        case "ChangeAvatarFrame": {
                            await timers.Value.DisposeAsync().ConfigureAwait(false);

                            bot.ArchiLogger.LogGenericInfo("ChangeAvatarFrame Dispose.");

                            break;
                        }

                        case "ChangeMiniBackground": {
                            await timers.Value.DisposeAsync().ConfigureAwait(false);

                            bot.ArchiLogger.LogGenericInfo("ChangeMiniBackground Dispose.");

                            break;
                        }

                        case "ChangeBackground": {
                            await timers.Value.DisposeAsync().ConfigureAwait(false);

                            bot.ArchiLogger.LogGenericInfo("ChangeBackground Dispose.");

                            break;
                        }

                        case "ChangeSpecialProfile": {
                            await timers.Value.DisposeAsync().ConfigureAwait(false);

                            bot.ArchiLogger.LogGenericInfo("ChangeSpecialProfile Dispose.");

                            break;
                        }
                    }
                }
            }

            ProfileStyleTimers[bot.BotName] = new Dictionary<string, Timer> {
                { "ChangeAvatar", new Timer(async e => await ChangeAvatar(bot).ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite) },
                { "ChangeAvatarFrame", new Timer(async e => await ChangeAvatarFrame(bot).ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite) },
                { "ChangeMiniBackground", new Timer(async e => await ChangeMiniBackground(bot).ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite) },
                { "ChangeBackground", new Timer(async e => await ChangeBackground(bot).ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite) },
                { "ChangeSpecialProfile", new Timer(async e => await ChangeSpecialProfile(bot).ConfigureAwait(false), null, Timeout.Infinite, Timeout.Infinite) }
            };

            ProfileStyleConfig[bot.BotName] = new ProfileStyleConfig();

            foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
                switch (configProperty.Key) {
                    case "ProfileStyleConfig": {
                        ProfileStyleConfig? config = configProperty.Value.ToJsonObject<ProfileStyleConfig>();

                        if (config != null) {
                            ProfileStyleConfig[bot.BotName] = config;
                        }

                        break;
                    }
                }
            }

            ProfileStyleConfig psc = ProfileStyleConfig[bot.BotName];

            if (psc.Avatars.Enable || psc.AvatarFrames.Enable || psc.MiniBackgrounds.Enable || psc.Backgrounds.Enable || psc.SpecialProfiles.Enable) {
                bot.ArchiLogger.LogGenericInfo($"ProfileStyleConfig: {psc.ToJsonText()}");

                if (psc.Avatars.Enable && (psc.Avatars.Items.Count > 0)) {
                    ProfileStyleTimers[bot.BotName]["ChangeAvatar"].Change(1, -1);
                }

                if (psc.AvatarFrames.Enable && (psc.AvatarFrames.Items.Count > 0)) {
                    ProfileStyleTimers[bot.BotName]["ChangeAvatarFrame"].Change(1, -1);
                }

                if (psc.MiniBackgrounds.Enable && (psc.MiniBackgrounds.Items.Count > 0)) {
                    ProfileStyleTimers[bot.BotName]["ChangeMiniBackground"].Change(1, -1);
                }

                if (psc.Backgrounds.Enable && (psc.Backgrounds.Items.Count > 0)) {
                    ProfileStyleTimers[bot.BotName]["ChangeBackground"].Change(1, -1);
                }

                if (psc.SpecialProfiles.Enable && (psc.SpecialProfiles.Items.Count > 0)) {
                    ProfileStyleTimers[bot.BotName]["ChangeSpecialProfile"].Change(1, -1);
                }
            }
        }
    }

    public async Task<string?> OnBotCommand(Bot bot, EAccess access, string message, string[] args, ulong steamID = 0) {
        if (args[0].Equals("GetMyItems", StringComparison.OrdinalIgnoreCase) && (access >= EAccess.FamilySharing)) {
            switch (args.Length) {
                case 1:
                    return await GetMyItems(bot).ConfigureAwait(false);

                case 2:
                    return await GetMyItems(bot, args[1]).ConfigureAwait(false) ?? await GetMyItems(bot, null, args[1]).ConfigureAwait(false);

                case 3:
                    return await GetMyItems(bot, args[1], args[2]).ConfigureAwait(false);
            }
        }

        return null;
    }

    [GeneratedRegex("""g_strCurrentLanguage = "(?<languageID>\w+)";""", RegexOptions.CultureInvariant)]
    private static partial Regex GetLanguageRegex();

    public static async Task<string?> GetMyItems(Bot defaultBot, string? botName = null, string? type = null) {
        Bot bot = defaultBot;

        if (botName != null) {
            Bot? newBot = Bot.GetBot(botName);

            if (newBot != null) {
                bot = newBot;
            } else {
                return null;
            }
        }

        if (bot.IsConnectedAndLoggedOn) {
            string language;

            try {
                HtmlDocumentResponse? rawLanguageResponse = await bot.ArchiWebHandler.UrlGetToHtmlDocumentWithSession(new Uri($"{ArchiWebHandler.SteamStoreURL}/account/languagepreferences")).ConfigureAwait(false);

                string? languageResponse = rawLanguageResponse?.Content?.Source.Text;

                if (languageResponse != null) {
                    MatchCollection languageMatches = GetLanguageRegex().Matches(languageResponse);

                    if (languageMatches.Count > 0) {
                        language = languageMatches[0].Groups["languageID"].Value;
                    } else {
                        language = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(ArchiWebHandler.SteamStoreURL, "Steam_Language") ?? "english";
                    }
                } else {
                    language = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(ArchiWebHandler.SteamStoreURL, "Steam_Language") ?? "english";
                }
            } catch {
                language = bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(ArchiWebHandler.SteamStoreURL, "Steam_Language") ?? "english";
            }

            ObjectResponse<GetProfileItemsOwnedResponse>? rawResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<GetProfileItemsOwnedResponse>(new Uri($"https://api.steampowered.com/IPlayerService/GetProfileItemsOwned/v1/?access_token={bot.AccessToken}&language={language}")).ConfigureAwait(false);

            GetProfileItemsOwnedResponse.ResponseData? response = rawResponse?.Content?.Response;

            if (response != null) {
                string result = "";

                List<string> bytes = ["Avatars", "AvatarFrames", "MiniBackgrounds", "Backgrounds", "SpecialProfiles"];

                if ((type != null) && bytes.Contains(type)) {
                    bytes = [type];
                }

                if ((response.Avatars != null) && bytes.Contains("Avatars")) {
                    result += "Avatars:\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.Avatars) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }
                }

                if ((response.AvatarFrames != null) && bytes.Contains("AvatarFrames")) {
                    result += "AvatarFrames:\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.AvatarFrames) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }
                }

                if ((response.MiniBackgrounds != null) && bytes.Contains("MiniBackgrounds")) {
                    result += "MiniBackgrounds:\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.MiniBackgrounds) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }
                }

                if ((response.Backgrounds != null) && bytes.Contains("Backgrounds")) {
                    result += "Backgrounds:\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.Backgrounds) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }
                }

                if ((response.SpecialProfiles != null) && bytes.Contains("SpecialProfiles")) {
                    result += "SpecialProfiles:\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.SpecialProfiles) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }
                }

                return result;
            }
        } else {
            return bot.Commands.FormatBotResponse("BotNotConnected");
        }

        return null;
    }

    public async Task ChangeAvatar(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            int communityitemid = ProfileStyleConfig[bot.BotName].Avatars.Items[RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].Avatars.Items.Count)];

            bool response = await bot.ArchiWebHandler.UrlPostWithSession(
                new Uri("https://api.steampowered.com/IPlayerService/SetAnimatedAvatar/v1/"), data: new Dictionary<string, string>(2) {
                    { "access_token", bot.AccessToken ?? string.Empty },
                    { "communityitemid", $"{communityitemid}" }
                }, session: ArchiWebHandler.ESession.None
            ).ConfigureAwait(false);

            if (response) {
                timeout = ProfileStyleConfig[bot.BotName].Avatars.Timeout;

                bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: OK | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            } else {
                bot.ArchiLogger.LogGenericInfo($"Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeAvatar"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }

    public async Task ChangeAvatarFrame(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            int communityitemid = ProfileStyleConfig[bot.BotName].AvatarFrames.Items[RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].AvatarFrames.Items.Count)];

            bool response = await bot.ArchiWebHandler.UrlPostWithSession(
                new Uri("https://api.steampowered.com/IPlayerService/SetAvatarFrame/v1/"), data: new Dictionary<string, string>(2) {
                    { "access_token", bot.AccessToken ?? string.Empty },
                    { "communityitemid", $"{communityitemid}" }
                }, session: ArchiWebHandler.ESession.None
            ).ConfigureAwait(false);

            if (response) {
                timeout = ProfileStyleConfig[bot.BotName].AvatarFrames.Timeout;

                bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: OK | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            } else {
                bot.ArchiLogger.LogGenericInfo($"Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeAvatarFrame"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }

    public async Task ChangeMiniBackground(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            int communityitemid = ProfileStyleConfig[bot.BotName].MiniBackgrounds.Items[RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].MiniBackgrounds.Items.Count)];

            bool response = await bot.ArchiWebHandler.UrlPostWithSession(
                new Uri("https://api.steampowered.com/IPlayerService/SetMiniProfileBackground/v1/"), data: new Dictionary<string, string>(2) {
                    { "access_token", bot.AccessToken ?? string.Empty },
                    { "communityitemid", $"{communityitemid}" }
                }, session: ArchiWebHandler.ESession.None
            ).ConfigureAwait(false);

            if (response) {
                timeout = ProfileStyleConfig[bot.BotName].MiniBackgrounds.Timeout;

                bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: OK | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            } else {
                bot.ArchiLogger.LogGenericInfo($"Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeMiniBackground"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }

    public async Task ChangeShowcase(Bot bot, int index) {
        if (bot.IsConnectedAndLoggedOn) {
            int communityitemid = ProfileStyleConfig[bot.BotName].Backgrounds.Showcases[index];

            bool response = await bot.ArchiWebHandler.UrlPostWithSession(
                new Uri("https://api.steampowered.com/IPlayerService/SetProfileBackground/v1/"), data: new Dictionary<string, string>(2) {
                    { "access_token", bot.AccessToken ?? string.Empty },
                    { "communityitemid", $"{communityitemid}" }
                }, session: ArchiWebHandler.ESession.None
            ).ConfigureAwait(false);

            bot.ArchiLogger.LogGenericInfo(response ? $"ID: {communityitemid} | Status: OK" : "Status: Error");
        } else {
            bot.ArchiLogger.LogGenericInfo("Status: BotNotConnected");
        }
    }

    public async Task ChangeBackground(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            int random = RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].Backgrounds.Items.Count);

            int communityitemid = ProfileStyleConfig[bot.BotName].Backgrounds.Items[random];

            bool response = await bot.ArchiWebHandler.UrlPostWithSession(
                new Uri("https://api.steampowered.com/IPlayerService/SetProfileBackground/v1/"), data: new Dictionary<string, string>(2) {
                    { "access_token", bot.AccessToken ?? string.Empty },
                    { "communityitemid", $"{communityitemid}" }
                }, session: ArchiWebHandler.ESession.None
            ).ConfigureAwait(false);

            if (response) {
                timeout = ProfileStyleConfig[bot.BotName].Backgrounds.Timeout;

                bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: OK | Next run: {DateTime.Now.AddMinutes(timeout):T}");

                if (ProfileStyleConfig[bot.BotName].Backgrounds.Showcases.Count >= random + 1) {
                    await ChangeShowcase(bot, random).ConfigureAwait(false);
                }
            } else {
                bot.ArchiLogger.LogGenericInfo($"Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeBackground"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }

    public async Task ChangeSpecialProfile(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            ObjectResponse<GetCommunityInventoryResponse>? rawResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<GetCommunityInventoryResponse>(new Uri($"https://api.steampowered.com/IQuestService/GetCommunityInventory/v1/?access_token={bot.AccessToken}")).ConfigureAwait(false);

            List<GetCommunityInventoryResponse.ResponseData.Item>? items = rawResponse?.Content?.Response?.Items;

            if (items != null) {
                int communityitemid = ProfileStyleConfig[bot.BotName].SpecialProfiles.Items[RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].SpecialProfiles.Items.Count)];

                GetCommunityInventoryResponse.ResponseData.Item? item = items.Find(x => x.CommunityItemId == communityitemid);

                if (item != null) {
                    bool response = await bot.ArchiWebHandler.UrlPostWithSession(
                        new Uri("https://api.steampowered.com/IQuestService/ActivateProfileModifierItem/v1/"), data: new Dictionary<string, string>(4) {
                            { "access_token", bot.AccessToken ?? string.Empty },
                            { "appid", $"{item.AppId}" },
                            { "communityitemid", $"{communityitemid}" },
                            { "activate", "true" }
                        }, session: ArchiWebHandler.ESession.None
                    ).ConfigureAwait(false);

                    if (response) {
                        timeout = ProfileStyleConfig[bot.BotName].SpecialProfiles.Timeout;

                        bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: OK | Next run: {DateTime.Now.AddMinutes(timeout):T}");

                        ProfileStyleConfig psc = ProfileStyleConfig[bot.BotName];

                        if (psc.Avatars.Enable && (psc.Avatars.Items.Count > 0)) {
                            ProfileStyleTimers[bot.BotName]["ChangeAvatar"].Change(1, -1);
                        }

                        if (psc.AvatarFrames.Enable && (psc.AvatarFrames.Items.Count > 0)) {
                            ProfileStyleTimers[bot.BotName]["ChangeAvatarFrame"].Change(1, -1);
                        }

                        if (psc.MiniBackgrounds.Enable && (psc.MiniBackgrounds.Items.Count > 0)) {
                            ProfileStyleTimers[bot.BotName]["ChangeMiniBackground"].Change(1, -1);
                        }

                        if (psc.Backgrounds.Enable && (psc.Backgrounds.Items.Count > 0)) {
                            ProfileStyleTimers[bot.BotName]["ChangeBackground"].Change(1, -1);
                        }
                    } else {
                        bot.ArchiLogger.LogGenericInfo($"Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
                    }
                } else {
                    bot.ArchiLogger.LogGenericInfo($"Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
                }
            } else {
                bot.ArchiLogger.LogGenericInfo($"Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeSpecialProfile"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }
}
