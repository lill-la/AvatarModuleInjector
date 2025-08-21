# AvatarModuleInjector

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that inject object to the avatar when equip.

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [AvatarModuleInjector.dll](https://github.com/lill-la/AvatarModuleInjector/releases/latest/download/AvatarModuleInjector.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create this folder for you.
3. Create a JSON file with the contents of [rml_config/AvatarModuleInjector_Modules.json](https://github.com/lill-la/AvatarModuleInjector/blob/master/AvatarModuleInjector/Modules.json) or start the game once to create it in the Resonite Folder, then copy the path to that file and paste it into the specified config option.

Here's the [AvatarModuleInjector_Modules.json](https://github.com/lill-la/AvatarModuleInjector/blob/master/AvatarModuleInjector/Modules.json)
```
[
  {
    "Name": "",
    "URI": "",
    "ExcludeIfExists": true,
    "ScaleToUser": false,
    "IsNameBadge": false,
    "IsIconBadge": false,
    "IsLiveBadge": false
  },
  {
    "Name": "",
    "URI": "",
    "ExcludeIfExists": true,
    "ScaleToUser": false,
    "IsNameBadge": false,
    "IsIconBadge": false,
    "IsLiveBadge": false
  },
  {
    "Name": "",
    "URI": "",
    "ExcludeIfExists": true,
    "ScaleToUser": false,
    "IsNameBadge": false,
    "IsIconBadge": false,
    "IsLiveBadge": false
  }
]
```
5. Start the game. If you want to verify that the mod is working you can check your Resonite logs.
