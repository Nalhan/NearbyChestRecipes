using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NearbyChestRecipes;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string ModGUID = "com.pergola.nearbychestrecipes";
    public const string ModName = "NearbyChestRecipes";
    public const string ModVersion = "1.0.0";

    internal static Plugin Instance = null!;
    internal static ManualLogSource Log = null!;

    // Fast reflection delegates
    private static readonly Func<Container, long, bool>? CheckAccessDelegate =
        AccessTools.MethodDelegate<Func<Container, long, bool>>(AccessTools.Method(typeof(Container), "CheckAccess"));

    private static readonly AccessTools.FieldRef<Player, bool>? IsLoadingField =
        AccessTools.FieldRefAccess<Player, bool>("m_isLoading");

    // Configuration
    public ConfigEntry<bool> ModEnabled = null!;
    public ConfigEntry<float> ScanRange = null!;
    public ConfigEntry<float> ScanInterval = null!;
    public ConfigEntry<bool> RequireAccess = null!;
    public ConfigEntry<bool> LogDiscoveries = null!;

    // Container tracking
    internal static readonly HashSet<Container> ActiveContainers = new();
    private static readonly List<Container> ToRemove = new();

    private float _lastScanTime;
    private Harmony _harmony = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        ModEnabled = Config.Bind(
            "1 - General",
            "Enabled",
            true,
            "Enable or disable discovering recipes/materials from nearby containers."
        );

        ScanRange = Config.Bind(
            "2 - Proximity",
            "Range",
            20f,
            "Proximity radius in meters to scan for containers (matches AzuCraftyBoxes default of 20m)."
        );

        ScanInterval = Config.Bind(
            "2 - Proximity",
            "Interval",
            2.0f,
            "Interval in seconds between proximity scans."
        );

        RequireAccess = Config.Bind(
            "3 - Access",
            "RequireAccess",
            true,
            "If true, respects ward protection and private chests (must have permission to open)."
        );

        LogDiscoveries = Config.Bind(
            "4 - Debug",
            "LogDiscoveries",
            false,
            "If true, outputs discovered materials and recipes to the BepInEx console log."
        );

        _harmony = new Harmony(ModGUID);
        _harmony.PatchAll();

        Log.LogInfo($"{ModName} v{ModVersion} loaded successfully.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        ActiveContainers.Clear();
    }

    private void Update()
    {
        if (!ModEnabled.Value) return;

        Player player = Player.m_localPlayer;
        if (player == null || player.IsDead()) return;
        if (player.IsTeleporting()) return;

        if (IsLoadingField != null && IsLoadingField(player)) return;

        if (Time.time < _lastScanTime + ScanInterval.Value) return;
        _lastScanTime = Time.time;

        ScanNearbyContainers(player, ScanRange.Value);
    }

    private void ScanNearbyContainers(Player player, float range)
    {
        Vector3 playerPos = player.transform.position;
        float rangeSqr = range * range;

        // Fallback: If no containers are registered yet, find existing loaded containers
        if (ActiveContainers.Count == 0)
        {
            Container[] existing = FindObjectsByType<Container>(FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null)
                {
                    ActiveContainers.Add(existing[i]);
                }
            }
        }

        ToRemove.Clear();

        foreach (Container container in ActiveContainers)
        {
            if (container == null || container.gameObject == null)
            {
                if (container != null) ToRemove.Add(container);
                continue;
            }

            Vector3 diff = container.transform.position - playerPos;
            if (diff.sqrMagnitude > rangeSqr)
            {
                continue;
            }

            if (RequireAccess.Value && !CanPlayerAccess(container, player))
            {
                continue;
            }

            DiscoverFromContainer(player, container);
        }

        // Clean up unreferenced/destroyed containers
        if (ToRemove.Count > 0)
        {
            for (int i = 0; i < ToRemove.Count; i++)
            {
                ActiveContainers.Remove(ToRemove[i]);
            }
            ToRemove.Clear();
        }
    }

    private static bool CanPlayerAccess(Container container, Player player)
    {
        if (CheckAccessDelegate != null)
        {
            try
            {
                return CheckAccessDelegate(container, player.GetPlayerID());
            }
            catch
            {
                return true;
            }
        }
        return true;
    }

    internal void DiscoverFromContainer(Player player, Container container)
    {
        if (container == null) return;

        Inventory inventory = container.GetInventory();
        if (inventory == null) return;

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        if (items == null || items.Count == 0) return;

        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];
            if (item?.m_shared == null) continue;

            string sharedName = item.m_shared.m_name;
            if (string.IsNullOrEmpty(sharedName)) continue;

            if (!player.IsMaterialKnown(sharedName))
            {
                player.AddKnownItem(item);

                if (LogDiscoveries.Value)
                {
                    Log.LogInfo($"Discovered new material from container: '{sharedName}'");
                }
            }
        }
    }

    // --- Harmony Patches to track containers and trigger instant discovery on open ---

    [HarmonyPatch(typeof(Container), "Awake")]
    private static class ContainerAwakePatch
    {
        private static void Postfix(Container __instance)
        {
            if (__instance != null)
            {
                ActiveContainers.Add(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Container), "OnDestroyed")]
    private static class ContainerOnDestroyedPatch
    {
        private static void Postfix(Container __instance)
        {
            if (__instance != null)
            {
                ActiveContainers.Remove(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    private static class ContainerInteractPatch
    {
        private static void Postfix(Container __instance, Humanoid user)
        {
            if (__instance != null && user == Player.m_localPlayer && Instance != null && Instance.ModEnabled.Value)
            {
                Instance.DiscoverFromContainer(Player.m_localPlayer, __instance);
            }
        }
    }
}
