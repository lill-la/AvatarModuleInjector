using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using ResoniteModLoader;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elements.Assets;

namespace AvatarModuleInjector;

public class AvatarModuleInjector : ResoniteMod
{
    internal const string VERSION_CONSTANT = "0.8.0";
    public override string Name => "AvatarModuleInjector";
    public override string Author => "lill";
    public override string Version => VERSION_CONSTANT;
    public override string Link => "https://github.com/lill-la/AvatarModuleInjector/";

    [AutoRegisterConfigKey] private static readonly ModConfigurationKey<string> ModuleJson = new ModConfigurationKey<string>("Module Json", "The path to a JSON file containing an array of modules to inject into avatars.", () => "Modules.json");

    private static ModConfiguration _config;

    public override void OnEngineInit()
    {
        _config = GetConfiguration();

        Harmony harmony = new Harmony("la.lill.AvatarModuleInjector");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ComponentBase<Component>), "OnStart")]
    private class ComponentBaseOnStartPatch
    {
        private static readonly Dictionary<RefID, Slot> Avatars = new Dictionary<RefID, Slot>();
        private static readonly List<Module> Modules = new List<Module>();
        private static readonly Dictionary<Slot, Slot> AvatarModuleList = new Dictionary<Slot, Slot>();

        private const string ProcessingMarkerTag = "__AMI_PROCESSING_MARKER";
        private const string ContainerTag = "__AMI_CONTAINER";

        private static void Postfix(Component __instance)
        {
            if (__instance is not AvatarObjectSlot avatarObjSlot) return;
            if (avatarObjSlot == null || avatarObjSlot.Slot?.ActiveUser != avatarObjSlot.LocalUser || avatarObjSlot.World.IsUserspace() || !avatarObjSlot.Slot.Name.StartsWith("User")) return;

            avatarObjSlot.Equipped.OnTargetChange += Equipped_OnTargetChange;
            avatarObjSlot.Disposing += Worker_Disposing;
            Msg($"Found AvatarObjectSlot. RefID: {avatarObjSlot.ReferenceID}");
        }

        private static void Worker_Disposing(Worker obj)
        {
            Avatars.Remove(obj.ReferenceID);
            Msg($"Dispose AvatarObjectSlot. RefID: {obj.ReferenceID}");
        }

        private static void Equipped_OnTargetChange(SyncRef<IAvatarObject> avatarObj)
        {
            if (avatarObj?.Target?.Node != BodyNode.Root) return;

            if (Avatars.TryGetValue(avatarObj.Worker.ReferenceID, out Slot oldAvatar))
            {
                if (oldAvatar != null)
                {
                    Msg($"Avatar DeEquip : {oldAvatar.Name}");
                    if (AvatarModuleList.TryGetValue(oldAvatar, out Slot slot))
                    {
                        oldAvatar.RunSynchronously(() => slot?.Destroy());
                    }

                    AvatarModuleList.Remove(oldAvatar);
                    Avatars[avatarObj.Worker.ReferenceID] = null;
                }
            }

            if (avatarObj.State != ReferenceState.Available) return;

            Msg($"Avatar Equip : {avatarObj.Target.Slot.Name}");
            
            // Check if the avatar is allowed to load cloud avatars and has the necessary permissions
            if (avatarObj.World.RootSlot.GetComponentInChildren<CommonAvatarBuilder>()?.LoadCloudAvatars.Value is not true
                || avatarObj.World.Permissions.Check(avatarObj.Target, (AvatarObjectPermissions p) => p.CanEquip(avatarObj.Target, avatarObj.World.LocalUser))) return;

            avatarObj.Target.Slot.RunInUpdates(1, () => InjectModules(avatarObj.Target.Slot));
            Avatars[avatarObj.Worker.ReferenceID] = avatarObj.Target.Slot;
        }

        private static void InjectModules(Slot avatar)
        {
            string moduleJsonString = null;

            if (File.Exists(_config.GetValue(ModuleJson)))
            {
                moduleJsonString = File.ReadAllText(_config.GetValue(ModuleJson));
            }
            else
            {
                const string defaultModule = "[\n  {\n    \"Name\": \"\",\n    \"URI\": \"\",\n    \"ExcludeIfExists\": false,\n    \"ScaleToUser\": false,\n    \"IsNameBadge\": false\n  },\n  {\n    \"Name\": \"\",\n    \"URI\": \"\",\n    \"ExcludeIfExists\": false,\n    \"ScaleToUser\": false,\n    \"IsNameBadge\": false\n  }\n]";

                File.WriteAllText(_config.GetValue(ModuleJson), defaultModule);
                Msg($"No modules file found at {_config.GetValue(ModuleJson)}. Created a new one with default template.");
            }

            if (string.IsNullOrEmpty(moduleJsonString)) return;

            Modules.Clear();
            Modules.AddRange(JsonSerializer.Deserialize<List<Module>>(moduleJsonString, new JsonSerializerOptions { AllowTrailingCommas = true }));
            if (Modules.Count == 0) return;

            List<Slot> oldMarker = avatar.GetChildrenWithTag(ProcessingMarkerTag);
            if (oldMarker.Count != 0) return;

            Slot rootContainer = avatar.AddSlot(ContainerTag, false);
            rootContainer.Tag = ContainerTag;
            rootContainer.OrderOffset = long.MaxValue;
            AvatarModuleList[avatar] = rootContainer;

            Slot processingMarker = avatar.AddSlot(ProcessingMarkerTag, false);
            processingMarker.Tag = ProcessingMarkerTag;

            AvatarManager avatarManager = avatar.LocalUser.Root.GetRegisteredComponent<AvatarManager>();
            bool avatarHasCustomNameBadge = avatar.GetComponentInChildren((AvatarNameTagAssigner a) => a.Slot != avatarManager.AutomaticNameBadge && !IsUnderView(a.Slot)) != null
                                            || avatar.GetComponentInChildren((AvatarBadgeManager a) => a.Slot != avatarManager.AutomaticIconBadge && !IsUnderView(a.Slot)) != null
                                            || avatar.GetComponentInChildren((AvatarLiveIndicator a) => a.Slot != avatarManager.AutomaticLiveBadge && !IsUnderView(a.Slot)) != null;

            AvatarObjectSlot avatarObjectSlot = avatar.LocalUser.Root.GetRegisteredComponent<AvatarObjectSlot>();
            for (int i = 0; i < Modules.Count; i++)
            {
                Module module = Modules[i];

                string name = string.IsNullOrEmpty(module.Name) ? $"__AMI_MODULE_{i}" : module.Name;
                Uri uri = string.IsNullOrEmpty(module.Uri) ? null : new Uri(module.Uri);
                bool excludeIfExists = module.ExcludeIfExists;
                bool scaleToUser = module.ScaleToUser;
                bool isNameBadge = module.IsNameBadge;

                if (uri == null) continue;
                if (avatarHasCustomNameBadge && isNameBadge) continue;

                if (excludeIfExists)
                {
                    bool found = false;

                    avatar.ForeachChild(c =>
                    {
                        if (c.Name.GetRawString().Equals(module.Name.GetRawString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            found = true;
                        }
                    });

                    if (found) continue;
                }

                Slot moduleContainer = rootContainer.AddSlot(name, false);

                moduleContainer.StartTask(async delegate
                {
                    Slot moduleRoot = moduleContainer.AddSlot("TempSlot", false);
                    await moduleRoot.LoadObjectAsync(uri);
                    moduleRoot.GetComponent<InventoryItem>()?.Unpack();

                    foreach (Slot child in moduleContainer.Children)
                    {
                        child.SetIdentityTransform();
                        if (scaleToUser) child.ScaleToUser(avatar.LocalUser);
                    }

                    AvatarObjectSlot.ForeachObjectComponent(moduleContainer, avatarObjectComponent =>
                    {
                        try
                        {
                            avatarObjectComponent.OnPreEquip(avatarObjectSlot);
                        }
                        catch (Exception e)
                        {
                            Msg($"Exception in OnPreEquip on {moduleContainer.Name}\n" + e.Message);
                        }
                    });

                    AvatarObjectSlot.ForeachObjectComponent(moduleContainer, avatarObjectComponent =>
                    {
                        try
                        {
                            avatarObjectComponent.OnEquip(avatarObjectSlot);
                        }
                        catch (Exception e)
                        {
                            Msg($"Exception in OnEquip on {moduleContainer.Name}\n" + e.Message);
                        }
                    });

                    Msg($"Module '{name}' Injected to {avatar.Name.GetRawString()}");
                });
            }

            avatar.RunInUpdates(2, delegate
            {
                bool hasCustomNameTag = rootContainer.GetComponentInChildren((AvatarNameTagAssigner a) => a.Slot != avatarManager.AutomaticNameBadge && !IsUnderView(a.Slot)) != null;
                bool hasCustomIconBadge = rootContainer.GetComponentInChildren((AvatarBadgeManager a) => a.Slot != avatarManager.AutomaticIconBadge && !IsUnderView(a.Slot)) != null;
                bool hasCustomLiveBadge = rootContainer.GetComponentInChildren((AvatarLiveIndicator a) => a.Slot != avatarManager.AutomaticLiveBadge && !IsUnderView(a.Slot)) != null;

                if (hasCustomNameTag) avatarManager.AutomaticNameBadge?.Destroy();
                if (hasCustomIconBadge) avatarManager.AutomaticIconBadge?.Destroy();
                if (hasCustomLiveBadge) avatarManager.AutomaticLiveBadge?.Destroy();

                List<Slot> markers = avatar.GetChildrenWithTag(ProcessingMarkerTag);
                foreach (Slot marker in markers)
                {
                    marker.Destroy();
                }
            });
        }

        private static bool IsUnderView(Slot slot)
            => slot.GetComponentInParents((AvatarPoseNode p) => p.Node.Value == BodyNode.View) != null;
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private struct Module
    {
        [JsonPropertyName("Name")] public string Name { get; set; }
        [JsonPropertyName("URI")] public string Uri { get; set; }
        [JsonPropertyName("ExcludeIfExists")] public bool ExcludeIfExists { get; set; }
        [JsonPropertyName("ScaleToUser")] public bool ScaleToUser { get; set; }
        [JsonPropertyName("IsNameBadge")] public bool IsNameBadge { get; set; }
    }
}

public static class Helpers
{
    public static string GetRawString(this string text) => string.IsNullOrEmpty(text) ? "Null" : new StringRenderTree(text).GetRawString();
}