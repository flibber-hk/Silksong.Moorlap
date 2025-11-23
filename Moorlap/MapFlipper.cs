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

        // Move the map marker cursor according to what they press
        Md.MapMarkerMenu.StartMarkerPlacement.Postfix(CreateVisualCursor);
        Md.MapMarkerMenu.PlaceMarker.ILHook(MovePlaceRemoveEffect);
        Md.MapMarkerMenu.RemoveMarker.ILHook(MovePlaceRemoveEffect);
        Md.MapMarkerMenu.Close.Postfix(DisableClone);

        // Flip the text on the map
        Md.GameMap.Awake.Postfix(FlipMapText);

        // Flip the wide map
        Md.InventoryMapManager.Awake.Postfix(FlipWideMap);
        Md.InventoryItemWideMapZone.GetNextSelectable.Prefix(ModifyWideMapNav);
        Md.InventoryItemWideMapZone.GetClosestNodePosLocalBounds.Prefix(FlipClosestNode);
    }

    private static void FlipClosestNode(InventoryItemWideMapZone self, ref Vector2 localBoundsPos)
    {
        localBoundsPos = new(1f - localBoundsPos.x, localBoundsPos.y);
    }

    private static void ModifyWideMapNav(InventoryItemWideMapZone self, ref InventoryItemManager.SelectionDirection direction)
    {
        InventoryItemManager.SelectionDirection dir = direction;
        switch (dir)
        {
            case InventoryItemManager.SelectionDirection.Left:
                direction = InventoryItemManager.SelectionDirection.Right;
                break;
            case InventoryItemManager.SelectionDirection.Right:
                direction = InventoryItemManager.SelectionDirection.Left;
                break;
        }
    }

    private static void FlipWideMap(InventoryMapManager self)
    {
        InventoryWideMap wideMap = self.wideMap;

        foreach (TMProOld.TextMeshPro tmpro in wideMap.GetComponentsInChildren<TMProOld.TextMeshPro>(true))
        {
            FlipTextObject(tmpro);
        }

        foreach (InventoryItemWideMapZone iwmz in wideMap.GetComponentsInChildren<InventoryItemWideMapZone>(true))
        {
            iwmz.gameObject.transform.FlipLocalScale(x: true);
            Vector3 pos = iwmz.transform.position;
            iwmz.transform.position = new(-pos.x, pos.y, pos.z);
        }

        foreach (QuestMapMarker qmm in wideMap.GetComponentsInChildren<QuestMapMarker>(true))
        {
            Vector3 pos = qmm.transform.position;
            qmm.transform.position = new(-pos.x, pos.y, pos.z);
        }
    }

    private static TMProOld.TextAlignmentOptions FlipAlignment(TMProOld.TextAlignmentOptions alignment)
    {
        return alignment switch
        {
            TMProOld.TextAlignmentOptions.TopLeft => TMProOld.TextAlignmentOptions.TopRight,
            TMProOld.TextAlignmentOptions.TopRight => TMProOld.TextAlignmentOptions.TopLeft,
            TMProOld.TextAlignmentOptions.Left => TMProOld.TextAlignmentOptions.Right,
            TMProOld.TextAlignmentOptions.Right => TMProOld.TextAlignmentOptions.Left,
            TMProOld.TextAlignmentOptions.BottomLeft => TMProOld.TextAlignmentOptions.BottomRight,
            TMProOld.TextAlignmentOptions.BottomRight => TMProOld.TextAlignmentOptions.BottomLeft,

            TMProOld.TextAlignmentOptions.BaselineLeft => TMProOld.TextAlignmentOptions.BaselineRight,
            TMProOld.TextAlignmentOptions.BaselineRight => TMProOld.TextAlignmentOptions.BaselineLeft,
            TMProOld.TextAlignmentOptions.MidlineLeft => TMProOld.TextAlignmentOptions.MidlineRight,
            TMProOld.TextAlignmentOptions.MidlineRight => TMProOld.TextAlignmentOptions.MidlineLeft,
            TMProOld.TextAlignmentOptions.CaplineLeft => TMProOld.TextAlignmentOptions.CaplineRight,
            TMProOld.TextAlignmentOptions.CaplineRight => TMProOld.TextAlignmentOptions.CaplineLeft,
            _ => alignment
        };
    }

    private static void FlipTextObject(TMProOld.TextMeshPro tmpro)
    {
        RectTransform rt = tmpro.gameObject.GetComponent<RectTransform>();
        rt.FlipLocalScale(x: true);
        tmpro.alignment = FlipAlignment(tmpro.alignment);
        Vector4 vec = tmpro.margin;
        tmpro.margin = new(vec.z, vec.y, vec.x, vec.w);
        rt.pivot = new(1 - rt.pivot.x, rt.pivot.y);
    }

    private static void FlipMapText(GameMap self)
    {
        foreach (TMProOld.TextMeshPro tmpro in self.GetComponentsInChildren<TMProOld.TextMeshPro>(true))
        {
            FlipTextObject(tmpro);
        }
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
