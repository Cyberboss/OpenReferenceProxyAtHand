using Elements.Core;

using FrooxEngine;

using HarmonyLib;

using ResoniteModLoader;

namespace OpenReferenceProxyAtHand
{
	public sealed class OpenReferenceProxyAtHand : ResoniteMod
	{
		// UPDATE VERSIONS HERE AND IN GITHUB ACTIONS. DON'T FORGET RELEASE NOTES!
		internal const string VersionConstant = "1.0.0";

		public override string Name => "OpenReferenceProxyAtHand";

		public override string Author => "Dominion";

		public override string Version => VersionConstant;

		public override string Link => "https://github.com/Cyberboss/OpenReferenceProxyAtHand";

		private static float3? ActiveGlobalPosition;

		[AutoRegisterConfigKey]
		private static readonly ModConfigurationKey<bool> Enabled = new ModConfigurationKey<bool>("Enabled", "Mod Enabled", () => true);

		private static ModConfiguration? Config;

		public override void OnEngineInit()
		{
			Config = GetConfiguration()!;
			Config.Save(true);

			Harmony harmony = new Harmony("net.dextraspace.OpenReferenceProxyAtHand");
			harmony.PatchAll();
		}

		[HarmonyPatch(typeof(ReferenceProxy), nameof(ReferenceProxy.Trigger))]
		static class ReferenceProxy_Trigger_Patches
		{
			public static bool Prefix(ReferenceProxy __instance)
			{
				if (Config?.GetValue(Enabled) ?? false)
				{
					ActiveGlobalPosition = __instance.Slot.GlobalPosition;
				}

				return true;
			}

			public static void Postfix(ReferenceProxy __instance)
			{
				ActiveGlobalPosition = null;
			}
		}

		[HarmonyPatch(typeof(SlotPositioning), nameof(SlotPositioning.PositionInFrontOfUser))]
		static class SlotPositioning_PositionInFrontOfUser_Patch
		{
			public static void Postfix(Slot slot, float3? faceDirection, float3? offset, float distance, User user, bool scale, bool checkOcclusion, bool preserveUp)
			{
				if (!ActiveGlobalPosition.HasValue)
				{
					return;
				}

				var activeGlobalPosition = ActiveGlobalPosition.Value;
				ActiveGlobalPosition = null;

				var world = slot.World;
				var localUser = world.LocalUser;
				if (user != null && user != localUser)
				{
					return;
				}

				if (!localUser.VR_Active)
				{
					return;
				}

				var screen = slot.World.GetScreen();
				if (screen == null)
				{
					Warn("Unable to find screen for interaction!");
					return;
				}

				slot.GlobalPosition = activeGlobalPosition;
				slot.GlobalRotation = floatQ.LookRotation(activeGlobalPosition - localUser.LocalUserRoot.ViewPosition);
			}
		}
	}
};
