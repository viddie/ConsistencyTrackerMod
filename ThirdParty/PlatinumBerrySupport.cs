using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.ConsistencyTracker.ThirdParty
{
  /// <summary>
  /// Platinum berries are <see cref="Entity"/>s implementing <see cref="IStrawberry"/>, not <see cref="Strawberry"/>s,
  /// so the vanilla strawberry hooks never see them. This hooks their pickup and collect methods directly and
  /// forwards them to the shared berry handling.
  /// Multiple mods ship their own copy of the same PlatinumBerry class, so all known copies are hooked.
  /// </summary>
  public static class PlatinumBerrySupport
  {

    // Mod name, minimum mod version, berry type name
    private static readonly Tuple<string, Version, string>[] BerryTypes = {
            Tuple.Create("PlatinumStrawberry", new Version(1, 0, 0), "Celeste.Mod.PlatinumStrawberry.Entities.PlatinumBerry"),
            Tuple.Create("Monika's D-Sides", new Version(4, 0, 0), "Celeste.Mod.DSidesPlatinum.Entities.PlatinumBerry"),
        };

    private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

    private static readonly List<Hook> Hooks = new List<Hook>();
    private static readonly Dictionary<Type, BerryFields> FieldsByType = new Dictionary<Type, BerryFields>();

    private class BerryFields
    {
      public FieldInfo ID;
      public FieldInfo Golden;
      public FieldInfo Follower;
    }

    public static void Load()
    {
      foreach (Tuple<string, Version, string> berryType in BerryTypes)
      {
        TryHookBerryType(berryType.Item1, berryType.Item2, berryType.Item3);
      }
    }

    public static void Unload()
    {
      foreach (Hook hook in Hooks)
      {
        hook.Dispose();
      }
      Hooks.Clear();
      FieldsByType.Clear();
    }

    private static void TryHookBerryType(string modName, Version minVersion, string typeName)
    {
      // Not using Everest.Loader.TryGetDependency, as that requires the major version to match. Major version bumps
      // in the D-Sides are unlikely to change the platinum, and the berry members are validated below anyway.
      EverestModule module = Everest.Modules.FirstOrDefault(m => m.Metadata?.Name == modName);
      if (module == null)
      {
        Mod.Log($"'{modName}' is not installed, not hooking '{typeName}'.");
        return;
      }
      Version version = module.Metadata.Version;
      bool isDevVersion = version.Major == 0 && version.Minor == 0; // Same exception as Everest makes for dev builds
      if (!isDevVersion && version < minVersion)
      {
        Mod.Log($"'{modName}' is installed in version {version}, but at least {minVersion} is required. Not hooking '{typeName}'.");
        return;
      }

      Type berryType = module.GetType().Assembly.GetType(typeName);
      MethodInfo onPlayer = berryType?.GetMethod("OnPlayer", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Player) }, null);
      MethodInfo onCollect = berryType?.GetMethod("OnCollect", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
      BerryFields fields = new BerryFields
      {
        ID = berryType?.GetField("ID", BindingFlags.Public | BindingFlags.Instance),
        Golden = berryType?.GetField("Golden", BindingFlags.Public | BindingFlags.Instance),
        Follower = berryType?.GetField("Follower", BindingFlags.Public | BindingFlags.Instance),
      };

      // Check the field types as well, so that the casts in the hooks can't fail
      bool idValid = fields.ID?.FieldType == typeof(EntityID);
      bool goldenValid = fields.Golden?.FieldType == typeof(bool);
      bool followerValid = fields.Follower?.FieldType == typeof(Follower);

      if (onPlayer == null || onCollect == null || !idValid || !goldenValid || !followerValid)
      {
        Mod.Log($"'{modName}' is installed, but '{typeName}' doesn't have the expected members " +
                $"(type: {berryType != null}, OnPlayer: {onPlayer != null}, OnCollect: {onCollect != null}, " +
                $"ID: {idValid}, Golden: {goldenValid}, Follower: {followerValid}). Not hooking it.");
        return;
      }

      // Apply both hooks or neither
      Hook onPlayerHook = null;
      try
      {
        onPlayerHook = new Hook(onPlayer, typeof(PlatinumBerrySupport).GetMethod(nameof(PlatinumBerry_OnPlayer), BindingFlags.NonPublic | BindingFlags.Static));
        Hook onCollectHook = new Hook(onCollect, typeof(PlatinumBerrySupport).GetMethod(nameof(PlatinumBerry_OnCollect), BindingFlags.NonPublic | BindingFlags.Static));
        Hooks.Add(onPlayerHook);
        Hooks.Add(onCollectHook);
      }
      catch (Exception ex)
      {
        onPlayerHook?.Dispose();
        Mod.Log($"Failed to hook '{typeName}' from '{modName}': {ex}");
        return;
      }

      FieldsByType[berryType] = fields;
      Mod.Log($"Hooked '{typeName}' from '{modName}'.");
    }

    private delegate void orig_OnPlayer(Entity self, Player player);
    private static void PlatinumBerry_OnPlayer(orig_OnPlayer orig, Entity self, Player player)
    {
      if (!FieldsByType.TryGetValue(self.GetType(), out BerryFields fields))
      {
        orig(self, player);
        return;
      }

      // OnPlayer is a PlayerCollider callback and fires on every frame of overlap. Only the call where the berry
      // starts following the player is the actual pickup.
      Follower follower = (Follower)fields.Follower.GetValue(self);
      bool hadLeader = follower?.Leader != null;

      orig(self, player);

      if (hadLeader || follower?.Leader == null) return;

      // Never let an exception in the tracking break the other mod's berry
      try
      {
        Mod.OnBerryTouched((EntityID)fields.ID.GetValue(self), self.GetType().Name, (bool)fields.Golden.GetValue(self), false);
      }
      catch (Exception ex)
      {
        Mod.Log($"Failed to handle platinum berry pickup: {ex}");
      }
    }

    private delegate void orig_OnCollect(Entity self);
    private static void PlatinumBerry_OnCollect(orig_OnCollect orig, Entity self)
    {
      orig(self);

      if (!FieldsByType.TryGetValue(self.GetType(), out BerryFields fields)) return;

      // Never let an exception in the tracking break the other mod's berry
      try
      {
        Mod.OnBerryCollected((EntityID)fields.ID.GetValue(self), self.GetType().Name, (bool)fields.Golden.GetValue(self), false);
      }
      catch (Exception ex)
      {
        Mod.Log($"Failed to handle platinum berry collect: {ex}");
      }
    }
  }
}
