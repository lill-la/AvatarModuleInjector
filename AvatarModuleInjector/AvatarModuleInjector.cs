using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using ResoniteModLoader;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elements.Assets;
using Renderite.Shared;

namespace AvatarModuleInjector;

public class AvatarModuleInjector : ResoniteMod
{
    internal const string VERSION_CONSTANT = "0.11.2";
    public override string Name => "AvatarModuleInjector";
    public override string Author => "lill, NepuShiro";
    public override string Version => VERSION_CONSTANT;
    public override string Link => "https://github.com/lill-la/AvatarModuleInjector/";
    
    [AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> Enabled = new ModConfigurationKey<bool>("Enabled", "Enables/Disables Injecting modules into the Avatar.", () => true);

    [AutoRegisterConfigKey] private static readonly ModConfigurationKey<string> ModuleJson = new ModConfigurationKey<string>("Module Json", "The path to a JSON file containing an array of modules to inject into avatars.", () => "rml_config/AvatarModuleInjector_Modules.json");

    private static ModConfiguration _config;

    public override void OnEngineInit()
    {
        _config = GetConfiguration();

        Harmony harmony = new Harmony("la.lill.AvatarModuleInjector");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(AvatarObjectSlot), "OnAwake")]
    private class AvatarObjectSlotOnAwakePatch
    {
        private static readonly Dictionary<RefID, Slot> Avatars = new Dictionary<RefID, Slot>();
        private static readonly List<Module> Modules = new List<Module>();
        private static readonly Dictionary<Slot, Slot> AvatarModuleList = new Dictionary<Slot, Slot>();

        private const string ProcessingMarkerTag = "__AMI_PROCESSING_MARKER";
        private const string ContainerTag = "__AMI_CONTAINER";

        private static void Postfix(AvatarObjectSlot __instance)
        {
            if (__instance.Slot?.ActiveUser != __instance.LocalUser || __instance.World.IsUserspace() || !__instance.Slot.Name.StartsWith("User")) return;

            __instance.Equipped.OnTargetChange += Equipped_OnTargetChange;
            __instance.Disposing += Worker_Disposing;
            Debug($"Found AvatarObjectSlot. RefID: {__instance.ReferenceID}");
        }

        private static void Worker_Disposing(Worker obj)
        {
            Avatars.Remove(obj.ReferenceID);
            Debug($"Dispose AvatarObjectSlot. RefID: {obj.ReferenceID}");
        }

        private static void Equipped_OnTargetChange(SyncRef<IAvatarObject> avatarObj)
        {
            if (avatarObj.Target?.Node != BodyNode.Root) return;

            if (Avatars.TryGetValue(avatarObj.Worker.ReferenceID, out Slot oldAvatar))
            {
                if (oldAvatar != null)
                {
                    Debug($"Avatar DeEquip : {oldAvatar.Name}");
                    if (AvatarModuleList.TryGetValue(oldAvatar, out Slot slot))
                    {
                        oldAvatar.RunSynchronously(() => slot?.Destroy());
                    }

                    AvatarModuleList.Remove(oldAvatar);
                    Avatars[avatarObj.Worker.ReferenceID] = null;
                }
            }

            if (avatarObj.State != ReferenceState.Available) return;
            
            Debug($"Avatar Equip : {avatarObj.Target.Slot.Name.GetRawString()}");
            avatarObj.Target.Slot.RunInUpdates(1, () => InjectModules(avatarObj.Target.Slot));
            Avatars[avatarObj.Worker.ReferenceID] = avatarObj.Target.Slot;
        }

        private static void InjectModules(Slot avatar)
        {
            if (!_config.GetValue(Enabled)) return;
            
            if (!avatar.World.CanSwapAvatar())
            {
                Error("InjectModules: You do not have permission to swap avatars in this world.");
                return;
            }
            
            Slot processingMarker = null;
            try
            {
                string moduleJsonString = null;

                if (File.Exists(_config.GetValue(ModuleJson)))
                {
                    moduleJsonString = File.ReadAllText(_config.GetValue(ModuleJson));
                }
                else
                {
                    const string defaultModule = "[\n  {\n    \"Name\": \"\",\n    \"URI\": \"\",\n    \"ExcludeIfExists\": false,\n    \"ScaleToUser\": false,\n    \"IsNameBadge\": false,\n    \"IsIconBadge\": false,\n    \"IsLiveBadge\": false\n  },\n  {\n    \"Name\": \"\",\n    \"URI\": \"\",\n    \"ExcludeIfExists\": false,\n    \"ScaleToUser\": false,\n    \"IsNameBadge\": false,\n    \"IsIconBadge\": false,\n    \"IsLiveBadge\": false\n  }\n]";

                    File.WriteAllText(_config.GetValue(ModuleJson), defaultModule);
                    Msg($"No modules file found at {_config.GetValue(ModuleJson)}. Created a new one with default template.");
                }

                if (string.IsNullOrEmpty(moduleJsonString))
                {
                    Msg("InjectModules: Module JSON string is null or empty");
                    return;
                }

                Modules.Clear();
                Modules.AddRange(JsonSerializer.Deserialize<List<Module>>(moduleJsonString, new JsonSerializerOptions { AllowTrailingCommas = true }));
                if (Modules.Count == 0)
                {
                    Msg("InjectModules: No modules found after deserialization");
                    return;
                }

                List<Slot> oldMarker = avatar.GetChildrenWithTag(ProcessingMarkerTag);
                if (oldMarker.Count != 0)
                {
                    Msg("InjectModules: Processing marker already exists");
                    return;
                }

                Slot rootContainer = avatar.AddSlot(ContainerTag, false);
                rootContainer.Tag = ContainerTag;
                rootContainer.OrderOffset = long.MaxValue;
                AvatarModuleList[avatar] = rootContainer;

                processingMarker = avatar.AddSlot(ProcessingMarkerTag, false);
                processingMarker.Tag = ProcessingMarkerTag;

                AvatarManager avatarManager = avatar.LocalUser.Root.GetRegisteredComponent<AvatarManager>();
                
                bool avatarHasCustomNameBadge = avatar.GetComponentInChildren<AvatarNameTagAssigner>(a => a.Slot != avatarManager.AutomaticNameBadge && !a.Slot.IsUnderView()) != null;
                bool avatarHasCustomIconBadge = avatar.GetComponentInChildren<AvatarBadgeManager>(a => a.Slot != avatarManager.AutomaticIconBadge && !a.Slot.IsUnderView()) != null;
                bool avatarHasCustomLiveBadge = avatar.GetComponentInChildren<AvatarLiveIndicator>(a => a.Slot != avatarManager.AutomaticLiveBadge && !a.Slot.IsUnderView()) != null;
                
                AvatarObjectSlot avatarObjectSlot = avatar.LocalUser.Root.GetRegisteredComponent<AvatarObjectSlot>();
                for (int i = 0; i < Modules.Count; i++)
                {
                    Module module = Modules[i];

                    string name = string.IsNullOrEmpty(module.Name) ? $"__AMI_MODULE_{i}" : module.Name;
                    Uri uri = string.IsNullOrEmpty(module.Uri) ? null : new Uri(module.Uri);
                    bool excludeIfExists = module.ExcludeIfExists;
                    bool scaleToUser = module.ScaleToUser;
                    bool isNameBadge = module.IsNameBadge;
                    bool isIconBadge = module.IsIconBadge;
                    bool isLiveBadge = module.IsLiveBadge;

                    if (uri == null)
                    {
                        Msg($"InjectModules: Skipping module {name} - URI is null");
                        continue;
                    }

                    if (avatarHasCustomNameBadge && isNameBadge)
                    {
                        Msg($"InjectModules: Skipping name badge module {name} - avatar already has custom name badge");
                        continue;
                    }
                    
                    if (avatarHasCustomIconBadge && isIconBadge)
                    {
                        Msg($"InjectModules: Skipping icon badge module {name} - avatar already has custom icon badge");
                        continue;
                    }
                    
                    if (avatarHasCustomLiveBadge && isLiveBadge)
                    {
                        Msg($"InjectModules: Skipping live badge module {name} - avatar already has custom live badge");
                        continue;
                    }

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

                        if (found)
                        {
                            Msg($"InjectModules: Skipping module {name} - already exists");
                            continue;
                        }
                    }

                    Slot moduleContainer = rootContainer.AddSlot(name, false);
                    moduleContainer.StartTask(async () =>
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

                        avatar.RunInUpdates(5, () =>
                        {
                            if (!avatarHasCustomNameBadge && isNameBadge)
                                avatarManager.AutomaticNameBadge?.Destroy();

                            if (!avatarHasCustomIconBadge && isIconBadge)
                                avatarManager.AutomaticIconBadge?.Destroy();

                            if (!avatarHasCustomLiveBadge && isLiveBadge)
                                avatarManager.AutomaticLiveBadge?.Destroy();
                        });
                        
                        Msg($"Module '{name}' Injected to {avatar.Name.GetRawString()}");
                    });

                    avatar.RunInUpdates(2, () =>
                    {
                        List<Slot> markers = avatar.GetChildrenWithTag(ProcessingMarkerTag);
                        foreach (Slot marker in markers)
                        {
                            marker.Destroy();
                        }
                    });
                }
            }
            catch (Exception e)
            {
                if (processingMarker != null)
                {
                    processingMarker.Name += "_ERROR";
                    Comment comment = processingMarker.GetComponentOrAttach<Comment>();
                    comment.Text.Value = e.Message;
                }
                Console.WriteLine(e);
                throw;
            }
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    private struct Module
    {
        [JsonPropertyName("Name")] public string Name { get; set; }
        [JsonPropertyName("URI")] public string Uri { get; set; }
        [JsonPropertyName("ExcludeIfExists")] public bool ExcludeIfExists { get; set; }
        [JsonPropertyName("ScaleToUser")] public bool ScaleToUser { get; set; }
        [JsonPropertyName("IsNameBadge")] public bool IsNameBadge { get; set; }
        [JsonPropertyName("IsIconBadge")] public bool IsIconBadge { get; set; }
        [JsonPropertyName("IsLiveBadge")] public bool IsLiveBadge { get; set; }
    }
}

public static class Helpers
{
    public static bool IsUnderView(this Slot slot)
        => slot.GetComponentInParents((AvatarPoseNode p) => p.Node.Value == BodyNode.View) != null;

    public static string GetRawString(this string text)
        => string.IsNullOrEmpty(text) ? "Null" : new StringRenderTree(text).GetRawString();
}