using BepInEx;
using MonoDetour.DetourTypes;
using Moorlap.Components;
using UnityEngine;

namespace Moorlap;

// TODO - adjust the plugin guid as needed
[BepInAutoPlugin(id: "io.github.flibber-hk.moorlap")]
public partial class MoorlapPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Md.GameCameras.Start.Postfix(FlipCameras);
        Md.HeroController.Start.Postfix(DoFlip);
        Md.HeroController.OnDestroy.Postfix(DoUnflip);
        Md.InventoryPaneInput.PressDirection.Prefix(RemoveInvFlip);
        Md.InputHandler.SendKeyBindingsToGameSettings.ControlFlowPrefix(PreventSavingBindsHook);
        Md.InputHandler.SendButtonBindingsToGameSettings.ControlFlowPrefix(PreventSavingBindsHook);

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    private ReturnFlow PreventSavingBindsHook(InputHandler self)
    {
        if (!ControlFlipper.IsFlipped())
        {
            return ReturnFlow.None;
        }
        Logger.LogInfo("Not saving binds to settings");
        return ReturnFlow.SkipOriginal;
    }

    private void RemoveInvFlip(InventoryPaneInput self, ref InventoryPaneBase.InputEventType eventType)
    {
        if (!ControlFlipper.IsFlipped())
        {
            return;
        }

        InventoryPaneBase.InputEventType arg = eventType;
        switch (arg)
        {
            case InventoryPaneBase.InputEventType.Left:
                eventType = InventoryPaneBase.InputEventType.Right;
                break;
            case InventoryPaneBase.InputEventType.Right:
                eventType = InventoryPaneBase.InputEventType.Left;
                break;
        }
    }

    private void DoUnflip(HeroController self) => ControlFlipper.SetFlippedControls(false);
    private void DoFlip(HeroController self) => ControlFlipper.SetFlippedControls(true);

    private void FlipCameras(GameCameras self)
    {
        foreach (Camera cam in self.mainCamera.gameObject.GetComponentsInChildren<Camera>())
        {
            cam.gameObject.AddComponent<MirrorFlipCamera>();
        }

        self.mainCamera.gameObject.AddComponent<AudioChannelSwapper>();
    }
}
