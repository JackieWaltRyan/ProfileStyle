# Profile Style Plugin for ArchiSteamFarm

ASF plugin for automatic change of various profile design items.

## Installation

1. Download the .zip file from
   the [![GitHub Release](https://img.shields.io/github/v/release/JackieWaltRyan/ProfileStyle?display_name=tag&logo=github&label=latest%20release)](https://github.com/JackieWaltRyan/ProfileStyle/releases/latest).<br><br>
2. Locate the `plugins` folder inside your ASF folder. Create a new folder here and unpack the downloaded .zip file to
   that folder.<br><br>
3. (Re)start ASF, you should get a message indicating that the plugin loaded successfully.

## Usage

Default configuration. To change this feature, add the following parameter to your bot's config file:

```json
{
  "ProfileStyleConfig": {
    "Avatars": {
      "Enable": false,
      "Items": [],
      "Timeout": 60
    },
    "AvatarFrames": {
      "Enable": false,
      "Items": [],
      "Timeout": 60
    },
    "MiniBackgrounds": {
      "Enable": false,
      "Items": [],
      "Timeout": 60
    },
    "Backgrounds": {
      "Enable": false,
      "Items": [],
      "Showcases": [],
      "Timeout": 60
    },
    "SpecialProfiles": {
      "Enable": false,
      "Items": [],
      "Timeout": 60
    }
  }
}
```

- `Enable` - `bool` type with default value of `false`. Enable or disable item rotation for a specific category.<br><br>
- `Items` - `List<ulong>` type with default value of `[]`. List of item IDs that will be changed randomly. To get a list
  of all IDs that can be added to this list, use the `GetMyItems` command. The order of the items in this list does not
  matter, as the plugin selects them randomly. There must be 1 or more IDs in the list to work, an empty list in any
  category is equal to the `"Enable": false` parameter in that category.<br><br>
- `Showcases` (only for category Backgrounds) - `List<ulong>` type with default value of `[]`. List of illustration IDs
  that will be applied to the `Featured Artwork Showcase` along with the background change. See below for details on
  this parameter.<br><br>
- `Timeout` - `uint` type with default value of `60`. Wait time in minutes between repeated item changes in a category.

### Backgrounds -> Showcases Config:

If you have a special linked illustration for some profile backgrounds that is used in the Featured Artwork Showcase,
then when changing the background, the plugin can automatically apply the desired illustration. To do this, simply
specify the illustration IDs in this list, following some rules:

- The number of IDs in the illustration list can be any and does not necessarily have to match the number of
  backgrounds. If there are more illustrations in the list than backgrounds, the extra illustrations will be ignored. If
  there are fewer illustrations in the list than backgrounds, some of the last backgrounds will not receive their
  illustrations.<br><br>
- The illustration position number in the list must exactly match the background position number in its list. Simply
  put, the first illustration in the list will be applied when the first background in the background list is set for
  the profile. The same for all the others. The third illustration in the list will be associated with the third
  background in the list. The fifth illustration = the fifth background, and so on.<br><br>
- If you don't need to set an illustration for a background that is somewhere in the middle of the list, then you need
  to specify `0` as the illustration ID. Illustrations with 0 instead of ID are the same as illustrations not existing.

For example, in the following example there are 5 backgrounds and 3 illustrations. The second and third illustrations
have a value of 0, which means that for backgrounds 2222222222 and 3333333333 no illustrations will be applied (to be
more precise, the profile will have the illustration that was there before). For background 1111111111, illustration
6666666666 will be set, and for 4444444444 - 7777777777 and so on accordingly.

```json
{
  "Backgrounds": {
    "Enable": true,
    "Items": [
      1111111111,
      2222222222,
      3333333333,
      4444444444,
      5555555555
    ],
    "Showcases": [
      6666666666,
      0,
      0,
      7777777777,
      8888888888
    ]
  }
}
```

## Commands

| Command                                                                                           | Access           | Description                                                                                  | Example                                                                                             |
|---------------------------------------------------------------------------------------------------|------------------|----------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| `GetMyItems`<br/>`GetMyItems <Category>`<br/>`GetMyItems [Bot]`<br/>`GetMyItems [Bot] <Category>` | `Family Sharing` | Prints the ID and name of all items on the account that can be used to decorate the profile. | `GetMyItems`<br/>`GetMyItems Avatars`<br/>`GetMyItems MyBotName`<br/>`GetMyItems MyBotName Avatars` |
