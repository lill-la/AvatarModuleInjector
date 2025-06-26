using System;
using System.Collections.Generic;
using System.Linq;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.CommonAvatar;

using HarmonyLib;

using ResoniteModLoader;

namespace AvatarModuleInjector;

public class AvatarModuleInjector : ResoniteMod {
	internal const string VERSION_CONSTANT = "0.6.0";
	public override string Name => "AvatarModuleInjector";
	public override string Author => "lill";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/lill-la/AvatarModuleInjector/";

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> AUTO_EXCLUDE = new("AutoExclude",
		"If a slot with the same name as ModuleName exists directly under ExcludeSlot, it will be excluded.",
		() => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> EXCLUDE_SLOT = new("ExcludeSlotList",
		"Exclude slot name under avatar root. \"__AMI_AVATAR_ROOT\" is avatar root slot. Comma will separate (Cannot specify a slot contains comma in its name). Only the first matching slot is considered. ex: \"__AMI_AVATAR_ROOT,Flux,System\"",
		() => "__AMI_AVATAR_ROOT");

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<int> EXCLUDE_SLOT_SEARCH_MAX_DEPTH =
		new("ExcludeSlotSearchMaxDepth",
			"Maximum depth for FindChildByName when searching for ExcludeSlot from the avatar root. Set -1 to no-limit.",
			() => -1);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_EXCLUDE = new("dummyExclude", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_00_NAME =
		new("module00Name", "Module 00 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_00_RESDB =
		new("module00Resrec", "Module 00 resrec", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_00_SCALE_TO_USER =
		new("module00ScaleTouser", "Module 00 scale will set to user global scale", () => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_00_NAME_BADGE =
		new("module00NameBadge",
			"If avatar already has custom namebadge (AvatarNameTagAssigner or AvatarBadgeManager or AvatarLiveIndicator), this module will not inject.",
			() => false);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_00 = new("dummy00", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_01_NAME =
		new("module01Name", "Module 01 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_01_RESDB =
		new("module01Resrec", "Module 01 resrec", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_01_SCALE_TO_USER =
		new("module01ScaleTouser", "Module 01 scale will set to user global scale", () => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_01_NAME_BADGE =
		new("module01NameBadge",
			"If avatar already has custom namebadge (AvatarNameTagAssigner or AvatarBadgeManager or AvatarLiveIndicator), this module will not inject.",
			() => false);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_01 = new("dummy01", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_02_NAME =
		new("module02Name", "Module 02 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_02_RESDB =
		new("module02Resrec", "Module 02 resrec", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_02_SCALE_TO_USER =
		new("module02ScaleTouser", "Module 02 scale will set to user global scale", () => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_02_NAME_BADGE =
		new("module02NameBadge",
			"If avatar already has custom namebadge (AvatarNameTagAssigner or AvatarBadgeManager or AvatarLiveIndicator), this module will not inject.",
			() => false);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_02 = new("dummy02", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_03_NAME =
		new("module03Name", "Module 03 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_03_RESDB =
		new("module03Resrec", "Module 03 resrec", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_03_SCALE_TO_USER =
		new("module03ScaleTouser", "Module 03 scale will set to user global scale", () => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_03_NAME_BADGE =
		new("module03NameBadge",
			"If avatar already has custom namebadge (AvatarNameTagAssigner or AvatarBadgeManager or AvatarLiveIndicator), this module will not inject.",
			() => false);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_03 = new("dummy03", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_04_NAME =
		new("module04Name", "Module 04 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_04_RESDB =
		new("module04Resrec", "Module 04 resrec", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_04_SCALE_TO_USER =
		new("module04ScaleTouser", "Module 04 scale will set to user global scale", () => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_04_NAME_BADGE =
		new("module04NameBadge",
			"If avatar already has custom namebadge (AvatarNameTagAssigner or AvatarBadgeManager or AvatarLiveIndicator), this module will not inject.",
			() => false);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_04 = new("dummy04", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_05_NAME =
		new("module05Name", "Module 05 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_05_RESDB =
		new("module05Resrec", "Module 05 resrec", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_05_SCALE_TO_USER =
		new("module05ScaleTouser", "Module 05 scale will set to user global scale", () => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_05_NAME_BADGE =
		new("module05NameBadge",
			"If avatar already has custom namebadge (AvatarNameTagAssigner or AvatarBadgeManager or AvatarLiveIndicator), this module will not inject.",
			() => false);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_05 = new("dummy05", "-----", () => new dummy());


	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_06_NAME =
		new("module06Name", "Module 06 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_06_RESDB =
		new("module06Resrec", "Module 06 resrec", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_06_SCALE_TO_USER =
		new("module06ScaleTouser", "Module 06 scale will set to user global scale", () => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_06_NAME_BADGE =
		new("module06NameBadge",
			"If avatar already has custom namebadge (AvatarNameTagAssigner or AvatarBadgeManager or AvatarLiveIndicator), this module will not inject.",
			() => false);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_06 = new("dummy06", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_07_NAME =
		new("module07Name", "Module 07 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_07_RESDB =
		new("module07Resrec", "Module 07 resrec", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_07_SCALE_TO_USER =
		new("module07ScaleTouser", "Module 07 scale will set to user global scale", () => false);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> MODULE_07_NAME_BADGE =
		new("module07NameBadge",
			"If avatar already has custom namebadge (AvatarNameTagAssigner or AvatarBadgeManager or AvatarLiveIndicator), this module will not inject.",
			() => false);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_07 = new("dummy07", "-----", () => new dummy());

	private static ModConfiguration? _config;

	public override void OnEngineInit() {
		_config = GetConfiguration();

		Harmony harmony = new Harmony("la.lill.AvatarModuleInjector");
		harmony.PatchAll();
	}

	[HarmonyPatch(typeof(ComponentBase<Component>), "OnStart")]
	class ComponentBase_OnStart_Patch {
		static void Postfix(FrooxEngine.Component __instance) {
			if (!(__instance is AvatarObjectSlot)) return;
			if ((__instance.Slot?.ActiveUser?.IsLocalUser ?? false) &&
			    (__instance.Slot.Name?.StartsWith("User") ?? false)) {
				if (__instance is AvatarObjectSlot avatarObjSlot) {
					avatarObjSlot.Equipped.OnTargetChange += Equipped_OnTargetChange;
					Msg($"Found AvatarObjectSlot. RefID: {avatarObjSlot.ReferenceID}");
				}
			}
		}

		private static void Equipped_OnTargetChange(SyncRef<IAvatarObject> avatarObj) {
			if (avatarObj?.Target?.Node != BodyNode.Root) return;

			if (avatarObj.State == ReferenceState.Available) {
				Msg($"Avatar Equip : {avatarObj.Target.Slot.Name}");
				InjectModules(avatarObj);
			}
		}

		private static void InjectModules(SyncRef<IAvatarObject> avatarObject) {
			Slot avatar = avatarObject.Target.Slot;
			World world = avatar.World;
			AvatarManager avatarManager = world.LocalUser.Root.GetRegisteredComponent<AvatarManager>();

			if (avatar.Name == "Dummy Head") return;

			var oldMarker = avatar.GetChildrenWithTag("__AMI_PROCESSING_MARKER");
			if (oldMarker.Count != 0) return;

			var olds = avatar.GetChildrenWithTag("__AMI_CONTAINER");
			foreach (Slot slot in olds) {
				slot.Destroy();
			}

			var rootContainer = avatar.AddSlot("__AMI_CONTAINER", false);
			rootContainer.Tag = "__AMI_CONTAINER";
			var processingMarker = avatar.AddSlot("__AMI_PROCESSING_MARKER", false);
			processingMarker.Tag = "__AMI_PROCESSING_MARKER";
			List<Uri?> moduleUriList = new() {
				_config?.GetValue(MODULE_00_RESDB),
				_config?.GetValue(MODULE_01_RESDB),
				_config?.GetValue(MODULE_02_RESDB),
				_config?.GetValue(MODULE_03_RESDB),
				_config?.GetValue(MODULE_04_RESDB),
				_config?.GetValue(MODULE_05_RESDB),
				_config?.GetValue(MODULE_06_RESDB),
				_config?.GetValue(MODULE_07_RESDB),
			};

			List<string?> moduleNameList = new() {
				_config?.GetValue(MODULE_00_NAME),
				_config?.GetValue(MODULE_01_NAME),
				_config?.GetValue(MODULE_02_NAME),
				_config?.GetValue(MODULE_03_NAME),
				_config?.GetValue(MODULE_04_NAME),
				_config?.GetValue(MODULE_05_NAME),
				_config?.GetValue(MODULE_06_NAME),
				_config?.GetValue(MODULE_07_NAME),
			};

			List<bool?> moduleIsUserScaleList = new() {
				_config?.GetValue(MODULE_00_SCALE_TO_USER),
				_config?.GetValue(MODULE_01_SCALE_TO_USER),
				_config?.GetValue(MODULE_02_SCALE_TO_USER),
				_config?.GetValue(MODULE_03_SCALE_TO_USER),
				_config?.GetValue(MODULE_04_SCALE_TO_USER),
				_config?.GetValue(MODULE_05_SCALE_TO_USER),
				_config?.GetValue(MODULE_06_SCALE_TO_USER),
				_config?.GetValue(MODULE_07_SCALE_TO_USER),
			};

			List<bool?> moduleIsCustomNameBadgeList = new() {
				_config?.GetValue(MODULE_00_NAME_BADGE),
				_config?.GetValue(MODULE_01_NAME_BADGE),
				_config?.GetValue(MODULE_02_NAME_BADGE),
				_config?.GetValue(MODULE_03_NAME_BADGE),
				_config?.GetValue(MODULE_04_NAME_BADGE),
				_config?.GetValue(MODULE_05_NAME_BADGE),
				_config?.GetValue(MODULE_06_NAME_BADGE),
				_config?.GetValue(MODULE_07_NAME_BADGE),
			};

			bool avatarHasCustomNameBadge
				= (avatar.GetComponentInChildren((AvatarNameTagAssigner a) =>
					  a.Slot != avatarManager.AutomaticNameBadge && !IsUnderView(a.Slot)) != null)
				  | (avatar.GetComponentInChildren((AvatarBadgeManager a) =>
					  a.Slot != avatarManager.AutomaticIconBadge && !IsUnderView(a.Slot)) != null)
				  | (avatar.GetComponentInChildren((AvatarLiveIndicator a) =>
					  a.Slot != avatarManager.AutomaticLiveBadge && !IsUnderView(a.Slot)) != null);

			bool autoExclude = _config?.GetValue(AUTO_EXCLUDE) ?? false;
			string? excludeSlotNames = _config?.GetValue(EXCLUDE_SLOT);
			int excludeSlotSearchMaxDepth = _config?.GetValue(EXCLUDE_SLOT_SEARCH_MAX_DEPTH) ?? -1;
			var excludeSlotNameList = excludeSlotNames?.Split(',').ToList() ?? new List<string>();

			AvatarObjectSlot avatarObjectSlot = world.LocalUser.Root.GetRegisteredComponent<AvatarObjectSlot>();
			for (var i = 0; i < moduleUriList.Count; i++) {
				var moduleUri = moduleUriList[i];
				if (moduleUri == null) continue;

				var moduleName = moduleNameList[i];
				if (String.IsNullOrWhiteSpace(moduleName)) moduleName = null;
				var moduleIsUserScale = moduleIsUserScaleList[i] ?? false;
				var moduleIsCustomNameBadge = moduleIsCustomNameBadgeList[i] ?? false;
				if (avatarHasCustomNameBadge & moduleIsCustomNameBadge) continue;

				if (autoExclude & moduleName != null & excludeSlotNames != null) {
					bool exclude = false;
					foreach (string excludeSlotName in excludeSlotNameList) {
						Slot? slot = avatar.FindChild(_slot => _slot.Name == excludeSlotName,
							maxDepth: excludeSlotSearchMaxDepth);
						if (excludeSlotName == "__AMI_AVATAR_ROOT") slot = avatar;
						if (slot != null) {
							if (slot.FindChild(moduleName!) != null) {
								exclude = true;
								break;
							}
						}
					}

					if (exclude) continue;
				}

				var moduleContainer = rootContainer.AddSlot(moduleName ?? $"__AMI_MODULE_{i:D2}");
				var moduleSlot = moduleContainer.AddSlot($"__AMI_MODULE_{i:D2}");
				moduleContainer.StartTask(async delegate {
					await moduleSlot.LoadObjectAsync(moduleUri);
					Msg($"Module {moduleSlot.Name} Injected to {avatar.Name}");
					moduleSlot.GetComponent<InventoryItem>()?.Unpack();
					foreach (Slot child in moduleContainer.Children) {
						child.SetIdentityTransform();
						if (moduleIsUserScale) child.ScaleToUser(world.LocalUser);
					}

					AvatarObjectSlot.ForeachObjectComponent(moduleContainer,
						avatarObjectComponent => {
							try {
								avatarObjectComponent.OnPreEquip(avatarObjectSlot);
							} catch (Exception e) {
								Msg($"Exception in OnPreEquip on {moduleContainer.Name}\n" + e.Message);
							}
						});
					AvatarObjectSlot.ForeachObjectComponent(moduleContainer,
						avatarObjectComponent => {
							try {
								avatarObjectComponent.OnEquip(avatarObjectSlot);
							} catch (Exception e) {
								Msg($"Exception in OnEquip on {moduleContainer.Name}\n" + e.Message);
							}
						});
				});
			}

			world.RunInUpdates(2, delegate {
				if (avatarObject.State != ReferenceState.Available) return;

				if (rootContainer.GetComponentInChildren((AvatarNameTagAssigner a) =>
					    a.Slot != avatarManager.AutomaticNameBadge && !IsUnderView(a.Slot)) != null) {
					avatarManager.AutomaticNameBadge?.Destroy();
				}

				if (rootContainer.GetComponentInChildren((AvatarBadgeManager a) =>
					    a.Slot != avatarManager.AutomaticIconBadge && !IsUnderView(a.Slot)) != null) {
					avatarManager.AutomaticIconBadge?.Destroy();
				}

				if (rootContainer.GetComponentInChildren((AvatarLiveIndicator a) =>
					    a.Slot != avatarManager.AutomaticLiveBadge && !IsUnderView(a.Slot)) != null) {
					avatarManager.AutomaticLiveBadge?.Destroy();
				}

				var markers = avatar.GetChildrenWithTag("__AMI_PROCESSING_MARKER");
				foreach (Slot marker in markers) {
					marker.Destroy();
				}
			});
		}

		private static bool IsUnderView(Slot slot) {
			return slot.GetComponentInParents((AvatarPoseNode p) => p.Node.Value == BodyNode.View) != null;
		}
	}
}
