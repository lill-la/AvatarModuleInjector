using System;
using System.Runtime.CompilerServices;

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
			
			var avatar = __instance.Equipped.Target.Slot;
			
			var olds = avatar.GetChildrenWithTag("__AMI_CONTAINER");
			foreach (Slot slot in olds)
			{
				slot.Destroy();
			}
			
			var container = avatar.AddSlot("__AMI_CONTAINER");
			container.Tag = "__AMI_CONTAINER";
			var module = container.AddSlot("__AMI_MODULE");
			module.StartTask(async delegate {
				await module.LoadObjectAsync(moduleUri);
				module = module.GetComponent<InventoryItem>()?.Unpack() ?? module;
			});
		}
	}
}
