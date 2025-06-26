using System;
using System.Collections.Generic;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.CommonAvatar;

using HarmonyLib;

using ResoniteModLoader;

namespace AvatarModuleInjector;

public class AvatarModuleInjector : ResoniteMod {
	internal const string VERSION_CONSTANT = "0.3.0";
	public override string Name => "AvatarModuleInjector";
	public override string Author => "lill";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/lill-la/AvatarModuleInjector/";

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<bool> AUTO_EXCLUDE = new("AutoExclude", "If a slot with the same name as ModuleName exists directly under ExcludeSlot, it will be excluded.", () => false);
	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> EXCLUDE_SLOT = new("ExcludeSlot", "Exclude slot name under avatar root. (Setting null will search directly under the avatar root)", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_EXCLUDE = new("dummyExclude", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_00_NAME =
		new("module00Name", "Module 00 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_00_RESDB =
		new("module00Resrec", "Module 00 resrec", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_00 = new("dummy00", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_01_NAME =
		new("module01Name", "Module 01 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_01_RESDB =
		new("module01Resrec", "Module 01 resrec", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_01 = new("dummy01", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_02_NAME =
		new("module02Name", "Module 02 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_02_RESDB =
		new("module02Resrec", "Module 02 resrec", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_02 = new("dummy02", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_03_NAME =
		new("module03Name", "Module 03 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_03_RESDB =
		new("module03Resrec", "Module 03 resrec", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_03 = new("dummy03", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_04_NAME =
		new("module04Name", "Module 04 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_04_RESDB =
		new("module04Resrec", "Module 04 resrec", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_04 = new("dummy04", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_05_NAME =
		new("module05Name", "Module 05 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_05_RESDB =
		new("module05Resrec", "Module 05 resrec", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_05 = new("dummy05", "-----", () => new dummy());


	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_06_NAME =
		new("module06Name", "Module 06 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_06_RESDB =
		new("module06Resrec", "Module 06 resrec", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_06 = new("dummy06", "-----", () => new dummy());

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<string?> MODULE_07_NAME =
		new("module07Name", "Module 07 name", () => null);

	[AutoRegisterConfigKey] private static readonly ModConfigurationKey<Uri?> MODULE_07_RESDB =
		new("module07Resrec", "Module 07 resrec", () => null);

	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY_07 = new("dummy07", "-----", () => new dummy());

	private static ModConfiguration? _config;

	private static Dictionary<RefID, Slot> avatars = new();

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
					__instance.Disposing += Worker_Disposing;
					Msg($"Found AvatarObjectSlot. RefID: {avatarObjSlot.ReferenceID}");
				}
			}
		}

		private static void Worker_Disposing(Worker obj) {
			avatars.Remove(obj.ReferenceID);
			Msg($"Dispose AvatarObjectSlot. RefID: {obj.ReferenceID}");
		}

		private static void Equipped_OnTargetChange(SyncRef<IAvatarObject> avatarObj) {
			if (avatarObj?.Target?.Node != BodyNode.Root) return;
			if (avatars.TryGetValue(avatarObj.Worker.ReferenceID, out var oldAvatar)) {
				if (!(oldAvatar is null)) {
					Msg($"Avatar DeEquip : {oldAvatar.Name}");
					avatars[avatarObj.Worker.ReferenceID] = null!;
				}
			}

			if (avatarObj.State == ReferenceState.Available) {
				Msg($"Avatar Equip : {avatarObj.Target.Slot.Name}");
				InjectModules(avatarObj.Target.Slot);
				avatars[avatarObj.Worker.ReferenceID] = avatarObj.Target.Slot;
			}
		}

		private static void InjectModules(Slot avatar) {
			World world = avatar.World;

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

			bool autoExclude = _config?.GetValue(AUTO_EXCLUDE) ?? false;
			string? excludeSlotName = _config?.GetValue(EXCLUDE_SLOT);
			List<string?> excludeSlotNameList = new();
			if (autoExclude) {
				Slot? slot =  excludeSlotName != null ? avatar.FindChild(excludeSlotName) : avatar;
				if (slot != null) {
					foreach (Slot slotChild in slot.Children)
					{
						excludeSlotNameList.Add(slotChild.Name);
					}
				}
			}

			AvatarObjectSlot avatarObjectSlot = world.LocalUser.Root.GetRegisteredComponent<AvatarObjectSlot>();
			for (var i = 0; i < moduleUriList.Count; i++) {
				var moduleUri = moduleUriList[i];
				var moduleName = moduleNameList[i];
				
				if (moduleUri == null) continue;
				if (autoExclude & excludeSlotNameList.Contains(moduleName)) continue;

				var moduleContainer = rootContainer.AddSlot(moduleName ?? $"__AMI_MODULE_{i:D2}");
				var moduleSlot = moduleContainer.AddSlot($"__AMI_MODULE_{i:D2}");
				moduleContainer.StartTask(async delegate {
					await moduleSlot.LoadObjectAsync(moduleUri);
					Msg($"Module {moduleSlot.Name} Injected to {avatar.Name}");
					moduleSlot.GetComponent<InventoryItem>()?.Unpack();
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

			world.RunInUpdates(1, delegate {
				AvatarManager avatarManager = world.LocalUser.Root.GetRegisteredComponent<AvatarManager>();

				if (rootContainer.GetComponentInChildren((AvatarNameTagAssigner a) =>
					    a.Slot != avatarManager.AutomaticNameBadge && !IsUnderView(a.Slot)) != null) {
					avatarManager.AutomaticNameBadge?.Destroy();
				}
				if (rootContainer.GetComponentInChildren((AvatarNameTagAssigner a) =>
					    a.Slot != avatarManager.AutomaticIconBadge && !IsUnderView(a.Slot)) != null) {
					avatarManager.AutomaticIconBadge?.Destroy();
				}
				if (rootContainer.GetComponentInChildren((AvatarNameTagAssigner a) =>
					    a.Slot != avatarManager.AutomaticLiveBadge && !IsUnderView(a.Slot)) != null) {
					avatarManager.AutomaticLiveBadge?.Destroy();
				}

				var markers = avatar.GetChildrenWithTag("__AMI_PROCESSING_MARKER");
				foreach (Slot marker in markers) {
					marker.Destroy();
				}
			});
		}
		
		private static bool IsUnderView(Slot slot)
		{
			return slot.GetComponentInParents((AvatarPoseNode p) => p.Node.Value == BodyNode.View) != null;
		}
	}
}
