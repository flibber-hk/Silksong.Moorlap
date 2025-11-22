using BepInEx.Logging;
using Mono.Cecil.Cil;
using MonoDetour.Cil;
using MonoMod.Cil;
using Moorlap.Components;
using Silksong.UnityHelper.Extensions;
using System;
using System.Linq;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;
using UObject = UnityEngine.Object;

namespace Moorlap;

internal static class MapFlipper
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource(nameof(MapFlipper));

    public static void Hook()
    {
        // Flip the map cameras (does not cover the "wide map" aka the zoomed out map but I don't really care about that
        Md.InventoryMapManager.Awake.Prefix(FlipMapCams);

        // Make the left/right pan arrows disappear properly
        Md.GameMap.UpdatePanArrows.ILHook(SwapMapArrows);

        // TODO - flip the text

        // Move the map marker cursor according to what they press
        Md.MapMarkerMenu.StartMarkerPlacement.Postfix(CreateVisualCursor);
        Md.MapMarkerMenu.PlaceMarker.ILHook(MovePlaceRemoveEffect);
        Md.MapMarkerMenu.RemoveMarker.ILHook(MovePlaceRemoveEffect);
        Md.MapMarkerMenu.Close.Postfix(DisableClone);
    }

    private static void DisableClone(MapMarkerMenu self)
    {
        GetRSMClone(self.placementCursor).SetActive(false);
    }

    private static GameObject GetRSMClone(GameObject go)
    {
        GameObject clone = go.GetComponent<RSMTracker>().Clone!.gameObject;
        return clone;
    }

    private static void MovePlaceRemoveEffect(ILManipulationInfo info)
    {
        ILCursor cursor = new(info.Context);

        cursor.GotoNext(i => i.MatchLdfld<MapMarkerMenu>(nameof(MapMarkerMenu.placeEffectPrefab))
                             || i.MatchLdfld<MapMarkerMenu>(nameof(MapMarkerMenu.removeEffectPrefab)));
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<MapMarkerMenu>(nameof(MapMarkerMenu.placementCursor)));
        cursor.EmitDelegate(GetRSMClone);
    }

    private static void CreateVisualCursor(MapMarkerMenu self)
    {
        GameObject placementCursor = self.placementCursor;
        GameObject visualCursor;

        if (placementCursor.GetComponent<RSMTracker>() != null)
        {
            visualCursor = placementCursor.GetComponent<RSMTracker>().Clone!.gameObject;
        }
        else
        {
            visualCursor = UObject.Instantiate(placementCursor);

            foreach (Transform t in visualCursor.transform)
            {
                UObject.Destroy(t.gameObject);
            }

            ReflectedSpriteMove rsm = visualCursor.AddComponent<ReflectedSpriteMove>();
            rsm.Original = self.placementCursor;

            RSMTracker tracker = self.placementCursor.GetOrAddComponent<RSMTracker>();
            tracker.Clone = rsm;
        }

        visualCursor.transform.parent = self.placementCursor.transform.parent;
        visualCursor.transform.localPosition = self.placementCursor.transform.localPosition;

        visualCursor.SetActive(self.placementCursor.activeSelf);
        self.placementCursor.GetOrAddComponent<Hider>();
    }

    private static void SwapMapArrows(ILManipulationInfo info)
    {
        ILCursor cursor = new(info.Context);

        Instruction loadPanL = info.Context.Instrs.Last(x => x.MatchLdfld<GameMap>(nameof(GameMap.panArrowL)));
        Instruction loadPanR = info.Context.Instrs.Last(x => x.MatchLdfld<GameMap>(nameof(GameMap.panArrowR)));
        (loadPanR.Operand, loadPanL.Operand) = (loadPanL.Operand, loadPanR.Operand);
    }

    private static void FlipMapCams(InventoryMapManager self)
    {
        self.mapCamera.gameObject.AddComponent<MirrorFlipCamera>();
        self.mapCamera.gameObject.transform.parent.Find("Decorator Camera").gameObject.AddComponent<MirrorFlipCamera>();
    }

}
