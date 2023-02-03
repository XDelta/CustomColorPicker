using System;
using System.Collections.Generic;
using BaseX;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using SpecialItemsLib;

namespace AvatarCreator {
	public class AvatarCreator : NeosMod {
		public override string Name => "CustomColorPicker";
		public override string Author => "Delta";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/XDelta/CustomColorPicker/";

		private static ModConfiguration Config;

		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<bool> enabled = new ModConfigurationKey<bool>("enabled", "Enables the mod", () => true);

		public override void OnEngineInit() {
			Config = GetConfiguration();
			Config.Save(true);
			Harmony harmony = new Harmony("net.deltawolf.CustomColorPicker");
			ColorPickerObject = SpecialItemsLib.SpecialItemsLib.RegisterItem(COLOR_PICKER_TAG);
			harmony.PatchAll();
		}
		private static string COLOR_PICKER_TAG { get { return "custom_color_picker"; } }
		private static CustomSpecialItem ColorPickerObject;

		[HarmonyPatch(typeof(SlotHelper), "GenerateTags", new Type[] { typeof(Slot), typeof(HashSet<string>) })]
		class SlotHelper_GenerateTags_Patch {
			static void Postfix(Slot slot, HashSet<string> tags) {
				if (slot.GetComponent<NeosColorDialog>() != null) {
					tags.Add(COLOR_PICKER_TAG);
				}
			}
		}

		[HarmonyPatch(typeof(NeosColorDialog), "SetupColor")]
		class NeosColorDialog_SetupColor_Patch {
			static bool Prefix(NeosColorDialog __instance, color defaultColor) {
				if (!Config.GetValue(enabled)) { return true; }
				if (ColorPickerObject.Uri == null) { return true; }

				var slot = __instance.Slot;

				slot.StartTask(async delegate () {
					await slot.LoadObjectAsync(ColorPickerObject.Uri);
					InventoryItem component = slot.GetComponent<InventoryItem>();
					slot = ((component != null) ? component.Unpack() : null) ?? slot;
					__instance.Enabled = false;
					slot.LocalScale = float3.One * 0.5f;

					NeosColorDialog colorDialog = slot.GetComponentInChildren<NeosColorDialog>(excludeDisabled:true);

					colorDialog.TargetField.Value = __instance.TargetField.Value;
					var colorDialogOriginalColor = (AccessTools.Field(typeof(NeosColorDialog), "_originalColor").GetValue(colorDialog) as Sync<color>);
					colorDialogOriginalColor.Value = defaultColor;
					__instance.Destroy();
					slot.PositionInFrontOfUser(float3.Backward);
				});
				return false;
			}
		}
	}
}