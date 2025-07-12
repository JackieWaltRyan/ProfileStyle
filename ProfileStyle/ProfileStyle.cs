using System;
using System.Collections.Generic;
using System.Linq;
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

        if (args[0].Equals("ChangeShowcase", StringComparison.OrdinalIgnoreCase) && (access >= EAccess.FamilySharing)) {
            await ChangeShowcase(bot, 0).ConfigureAwait(false);
        }

        return null;
    }

    [GeneratedRegex("""OnScreenshotClicked\(\s*(?<showcaseID>\d+)\s*\);""", RegexOptions.CultureInvariant)]
    private static partial Regex ShowcasesIDRegex();

    [GeneratedRegex("""<q class="ellipsis">(?<showcaseName>.+)</q>""", RegexOptions.CultureInvariant)]
    private static partial Regex ShowcasesNameRegex();

    public static async Task<Dictionary<ulong, string>> LoadingShowcases(Bot bot, uint page = 1) {
        try {
            Dictionary<ulong, string> showcasesDict = new();

            if (!bot.IsConnectedAndLoggedOn) {
                return showcasesDict;
            }

            bot.ArchiLogger.LogGenericInfo($"Checking existing showcases: Page {page}");

            HtmlDocumentResponse? rawResponse = await bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(
                new Uri($"{ArchiWebHandler.SteamCommunityURL}/profiles/{bot.SteamID}/images/screenshots"), data: new Dictionary<string, string>(7) {
                    { "appid", "0" },
                    { "p", $"{page}" },
                    { "privacy", "30" },
                    { "content", "1" },
                    { "browsefilter", "myfiles" },
                    { "sort", "newestfirst" },
                    { "view", "imagewall" }
                }, referer: new Uri($"{ArchiWebHandler.SteamCommunityURL}/profiles/{bot.SteamID}/images/")
            ).ConfigureAwait(false);

            string? response = rawResponse?.Content?.Source?.Text;

            if (response != null) {
                MatchCollection showcasesIDMatches = ShowcasesIDRegex().Matches(response);
                MatchCollection showcasesNameMatches = ShowcasesNameRegex().Matches(response);

                if ((showcasesIDMatches.Count > 0) && (showcasesNameMatches.Count > 0) && (showcasesIDMatches.Count == showcasesNameMatches.Count)) {
                    int index = 0;

                    foreach (Match match in showcasesIDMatches) {
                        if (ulong.TryParse(match.Groups["showcaseID"].Value, out ulong showcaseID)) {
                            showcasesDict[showcaseID] = showcasesNameMatches[index].Groups["showcaseName"].Value;
                        }

                        index += 1;
                    }

                    if (showcasesIDMatches.Count == 12) {
                        Dictionary<ulong, string> newShowcasesDict = await LoadingShowcases(bot, page + 1).ConfigureAwait(false);

                        showcasesDict = showcasesDict.Concat(newShowcasesDict).ToDictionary(static x => x.Key, static x => x.Value);
                    }
                }
            } else {
                await Task.Delay(3000).ConfigureAwait(false);

                await LoadingShowcases(bot, page).ConfigureAwait(false);
            }

            return showcasesDict;
        } catch {
            await Task.Delay(3000).ConfigureAwait(false);

            return await LoadingShowcases(bot, page).ConfigureAwait(false);
        }
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
                    result += $"Avatars ({response.Avatars.Count}):\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.Avatars) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }

                    result += "\n";
                }

                if ((response.AvatarFrames != null) && bytes.Contains("AvatarFrames")) {
                    result += $"AvatarFrames ({response.AvatarFrames.Count}):\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.AvatarFrames) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }

                    result += "\n";
                }

                if ((response.MiniBackgrounds != null) && bytes.Contains("MiniBackgrounds")) {
                    result += $"MiniBackgrounds ({response.MiniBackgrounds.Count}):\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.MiniBackgrounds) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }

                    result += "\n";
                }

                if ((response.Backgrounds != null) && bytes.Contains("Backgrounds")) {
                    result += $"Backgrounds ({response.Backgrounds.Count}):\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.Backgrounds) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }

                    result += "\n";

                    Dictionary<ulong, string> showcasesDict = await LoadingShowcases(bot).ConfigureAwait(false);

                    bot.ArchiLogger.LogGenericInfo($"Existing showcases found: {showcasesDict.Count}");

                    if (showcasesDict.Count > 0) {
                        result += $"    Showcases ({showcasesDict.Count}):\n";

                        foreach (KeyValuePair<ulong, string> item in showcasesDict) {
                            result += $"        {item.Key}: {item.Value}\n";
                        }

                        result += "\n";
                    }
                }

                if ((response.SpecialProfiles != null) && bytes.Contains("SpecialProfiles")) {
                    result += $"SpecialProfiles ({response.SpecialProfiles.Count}):\n";

                    foreach (GetProfileItemsOwnedResponse.ResponseData.ItemData item in response.SpecialProfiles) {
                        result += $"    {item.CommunityItemId}: {item.ItemTitle}\n";
                    }

                    result += "\n";
                }

                return result;
            }

            return bot.Commands.FormatBotResponse("CommunityInventoryIsEmpty or Error");
        }

        return bot.Commands.FormatBotResponse("BotNotConnected");
    }

    public async Task ChangeAvatar(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            ulong communityitemid = ProfileStyleConfig[bot.BotName].Avatars.Items[RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].Avatars.Items.Count)];

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
                bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeAvatar"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }

    public async Task ChangeAvatarFrame(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            ulong communityitemid = ProfileStyleConfig[bot.BotName].AvatarFrames.Items[RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].AvatarFrames.Items.Count)];

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
                bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeAvatarFrame"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }

    public async Task ChangeMiniBackground(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            ulong communityitemid = ProfileStyleConfig[bot.BotName].MiniBackgrounds.Items[RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].MiniBackgrounds.Items.Count)];

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
                bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeMiniBackground"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }

    public static async Task ChangeShowcase(Bot bot, int index) {
        if (bot.IsConnectedAndLoggedOn) {
            ObjectResponse<GetProfileCustomizationResponse>? rawResponse = await bot.ArchiWebHandler.UrlGetToJsonObjectWithSession<GetProfileCustomizationResponse>(new Uri($"https://api.steampowered.com/IPlayerService/GetProfileCustomization/v1/?access_token={bot.AccessToken}&steamid={bot.SteamID}&include_purchased_customizations=true")).ConfigureAwait(false);

            List<GetProfileCustomizationResponse.ResponseData.Customization>? items = rawResponse?.Content?.Response?.Customizations;

            bot.ArchiLogger.LogGenericInfo(items.ToJsonText());

            if (items != null) {
                Dictionary<string, string> data = new() {
                    { "type", "showcases" },
                    { "sessionID", bot.ArchiWebHandler.WebBrowser.CookieContainer.GetCookieValue(ArchiWebHandler.SteamCommunityURL, "sessionid") ?? string.Empty },
                    { "json", "1" }
                };

                foreach (GetProfileCustomizationResponse.ResponseData.Customization item in items) {
                    data["profile_showcase[]"] = $"{item.CustomizationType}";
                    data["profile_showcase_purchaseid[]"] = $"{item.PurchaseId}";
                    data[$"profile_showcase_style_{item.CustomizationType}_{item.PurchaseId}"] = $"{item.CustomizationStyle}";

                    bot.ArchiLogger.LogGenericInfo(item.Slots.ToJsonText());

                    if (item.Slots != null) {
                        foreach (GetProfileCustomizationResponse.ResponseData.Customization.SlotData slot in item.Slots) {
                            if (slot.Data != null) {
                                foreach (KeyValuePair<string, JsonElement> slotData in slot.Data) {
                                    bot.ArchiLogger.LogGenericInfo($"{slotData.Key} = {slotData.Value}");

                                    data[$"rgShowcaseConfig[{item.CustomizationType}_{item.PurchaseId}][{slot.Slot}][{slotData.Key}]"] = $"{slotData.Value}";
                                }
                            }
                        }
                    }
                }

                bot.ArchiLogger.LogGenericInfo(data.ToJsonText());

                // ulong communityitemid = ProfileStyleConfig[bot.BotName].Backgrounds.Showcases[index];
                //
                // ObjectResponse<ChangeShowcaseResponse>? rawCsResponse = await bot.ArchiWebHandler.UrlPostToJsonObjectWithSession<ChangeShowcaseResponse>(new Uri($"{ArchiWebHandler.SteamCommunityURL}/profiles/{bot.SteamID}/edit/"), data: data).ConfigureAwait(false);
                //
                // ChangeShowcaseResponse? response = rawCsResponse?.Content;
                //
                // if (response != null) {
                //     bot.ArchiLogger.LogGenericInfo(response.Success == 1 ? $"ID: {communityitemid} | Status: OK" : $"ID: {communityitemid} | Status: {response.ErrMsg}");
                // } else {
                //     bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: Error");
                //
                //     await Task.Delay(3000).ConfigureAwait(false);
                //
                //     await ChangeShowcase(bot, index).ConfigureAwait(false);
                // }
            } else {
                bot.ArchiLogger.LogGenericInfo("Status: Error");

                await Task.Delay(3000).ConfigureAwait(false);

                await ChangeShowcase(bot, index).ConfigureAwait(false);
            }
        } else {
            bot.ArchiLogger.LogGenericInfo("Status: BotNotConnected");

            await Task.Delay(3000).ConfigureAwait(false);

            await ChangeShowcase(bot, index).ConfigureAwait(false);
        }
    }

    public async Task ChangeBackground(Bot bot) {
        uint timeout = 1;

        if (bot.IsConnectedAndLoggedOn) {
            int random = RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].Backgrounds.Items.Count);

            ulong communityitemid = ProfileStyleConfig[bot.BotName].Backgrounds.Items[random];

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
                bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
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
                ulong communityitemid = ProfileStyleConfig[bot.BotName].SpecialProfiles.Items[RandomNumberGenerator.GetInt32(ProfileStyleConfig[bot.BotName].SpecialProfiles.Items.Count)];

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
                        bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: Error | Next run: {DateTime.Now.AddMinutes(timeout):T}");
                    }
                } else {
                    bot.ArchiLogger.LogGenericInfo($"ID: {communityitemid} | Status: ItemNotOwned | Next run: {DateTime.Now.AddMinutes(timeout):T}");
                }
            } else {
                bot.ArchiLogger.LogGenericInfo($"Status: CommunityInventoryIsEmpty | Next run: {DateTime.Now.AddMinutes(timeout):T}");
            }
        } else {
            bot.ArchiLogger.LogGenericInfo($"Status: BotNotConnected | Next run: {DateTime.Now.AddMinutes(timeout):T}");
        }

        ProfileStyleTimers[bot.BotName]["ChangeSpecialProfile"].Change(TimeSpan.FromMinutes(timeout), TimeSpan.FromMilliseconds(-1));
    }
}
