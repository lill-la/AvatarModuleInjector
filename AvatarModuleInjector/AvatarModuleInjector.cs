using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Elements.Core;

using FrooxEngine;
using FrooxEngine.CommonAvatar;

using HarmonyLib;
using ResoniteModLoader;

namespace AvatarModuleInjector;
public class AvatarModuleInjector : ResoniteMod {
	internal const string VERSION_CONSTANT = "0.1.0";
	public override string Name => "AvatarModuleInjector";
	public override string Author => "lill";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/lill-la/AvatarModuleInjector/";
	
	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<Uri?> MODULE_RESDB = new("moduleResdb", "Module resdb", () => null);
	
	[AutoRegisterConfigKey]
	private static readonly ModConfigurationKey<dummy> DUMMY = new("dummy", "Dummy", () => new dummy());
	
	private static ModConfiguration? _config;

	public override void OnEngineInit() {
		_config = GetConfiguration();
		
		Harmony harmony = new Harmony("la.lill.AvatarModuleInjector");
		harmony.PatchAll();
	}
	
	[HarmonyPatch]
	class AvatarObjectSlotPatch {

		[HarmonyReversePatch]
		[HarmonyPatch(typeof(ComponentBase<Component>), "OnChanges")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void OnChangesBase(AvatarObjectSlot instance) { }

		[HarmonyPatch(typeof(AvatarObjectSlot), "OnChanges"), HarmonyPrefix]
		static void InternalRunUpdatePrefix(AvatarObjectSlot __instance) {
			OnChangesBase(__instance);
			if (!__instance.IsUnderLocalUser) return;
			if (__instance.Node.Value != BodyNode.Root) return;
			var moduleUri = _config?.GetValue(MODULE_RESDB);
			if (moduleUri == null) return;

			World? world = null;
			if (__instance is IWorldElement iWorldElementInstance) {
				world = iWorldElementInstance.World;
			}
			if (world == null) return;
			UserRoot userRoot = world.LocalUser.Root;
			
			var avatar = __instance.Equipped.Target.Slot;

			if (avatar.Name == "Dummy Head") return;

			var oldMarker = avatar.GetChildrenWithTag("__AMI_PROCESSING_MARKER");
			if (oldMarker.Count != 0) return;
			
			var olds = avatar.GetChildrenWithTag("__AMI_CONTAINER");
			foreach (Slot slot in olds)
			{
				slot.Destroy();
			}
			
			var container = avatar.AddSlot("__AMI_CONTAINER", false);
			container.Tag = "__AMI_CONTAINER";
			var processingMarker = avatar.AddSlot("__AMI_PROCESSING_MARKER", false);
			processingMarker.Tag = "__AMI_PROCESSING_MARKER";
			var module = container.AddSlot("__AMI_MODULE");
			module.StartTask(async delegate {
				await module.LoadObjectAsync(moduleUri);
				module = module.GetComponent<InventoryItem>()?.Unpack() ?? module;
				world.RunInUpdates(1, delegate {
					AvatarManager avatarManager = userRoot.GetRegisteredComponent<AvatarManager>();

					var dummyHead = world.LocalUserSpace.AddSlot("Dummy Head", false);
					dummyHead.AttachComponent<AvatarPoseNode>().Node.Value = BodyNode.Head;
					dummyHead.AttachComponent<AvatarDestroyOnDequip>();
					avatarManager.Equip(dummyHead);
					avatarManager.Equip(avatar);

					world.RunInUpdates(2, delegate {
						var markers = avatar.GetChildrenWithTag("__AMI_PROCESSING_MARKER");
						foreach (Slot marker in markers)
						{
							marker.Destroy();
						}
					});
				});
			});
		}
	}
}
