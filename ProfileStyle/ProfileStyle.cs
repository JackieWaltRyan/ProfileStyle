using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArchiSteamFarm.Helpers.Json;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;

namespace ProfileStyle;

internal sealed class ProfileStyle : IGitHubPluginUpdates, IBotModules {
    public string Name => nameof(ProfileStyle);
    public string RepositoryName => "JackieWaltRyan/ProfileStyle";
    public Version Version => typeof(ProfileStyle).Assembly.GetName().Version ?? throw new InvalidOperationException(nameof(Version));

    public Timer UserDataRefreshTimer;

    public Task OnLoaded() => Task.CompletedTask;

    public Task OnBotInitModules(Bot bot, IReadOnlyDictionary<string, JsonElement>? additionalConfigProperties = null) {
        if (additionalConfigProperties == null) {
            return Task.CompletedTask;
        }

        bool isEnabled = false;
        uint timeout = 60;

        foreach (KeyValuePair<string, JsonElement> configProperty in additionalConfigProperties) {
            switch (configProperty.Key) {
                case "EnableProfileStyle" when configProperty.Value.ValueKind is JsonValueKind.True or JsonValueKind.False: {
                    isEnabled = configProperty.Value.GetBoolean();

                    bot.ArchiLogger.LogGenericInfo($"Enable Profile Style: {isEnabled}");

                    break;
                }

                case "ProfileStyleTimeout" when configProperty.Value.ValueKind is JsonValueKind.Number: {
                    timeout = configProperty.Value.ToJsonObject<uint>();

                    bot.ArchiLogger.LogGenericInfo($"Profile Style Timeout: {timeout}");

                    break;
                }

                case "ProfileStyleSettings": {
                    FilterConfig? filter = configProperty.Value.ToJsonObject<FilterConfig>();

                    if (filter != null) {
                        if (filter.Types.Contains("Demo") && filter.IgnoredTypes.Contains("Demo")) {
                            filter.IgnoredTypes.Remove("Demo");
                        }

                        if (filter.MinDaysOld > 0 && filter.MaxDaysOld == 0) {
                            filter.MaxDaysOld = filter.MinDaysOld;
                        }

                        bot.ArchiLogger.LogGenericInfo("Profile Style Settings: " + filter.ToJsonText());

                        filterConfigs.Add(filter);
                    }

                    break;
                }
            }
        }

        if (isEnabled) {
            timeout;
        }

        return Task.CompletedTask;
    }
}
