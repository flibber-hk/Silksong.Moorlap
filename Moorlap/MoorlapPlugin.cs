using BepInEx;
using MonoDetour.DetourTypes;
using Moorlap.Components;
using UnityEngine;
using Silksong.UnityHelper.Extensions;

namespace Moorlap;

[BepInAutoPlugin(id: "io.github.flibber-hk.moorlap")]
public partial class MoorlapPlugin : BaseUnityPlugin
{
    public static MoorlapPlugin Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        // Actually flip the main camera
        Md.GameCameras.Start.Postfix(FlipCameras);

        // Flip controls when in game
        Md.HeroController.Start.Postfix(DoFlip);
        Md.HeroController.OnDestroy.Postfix(DoUnflip);

        // Unflip controls when interacting with an inventory pane
        Md.InventoryPaneInput.PressDirection.Prefix(RemoveInvFlip);

        // Map stuff
        MapFlipper.Hook();

        // Prevent saving the modified key bindings (this could be made better by only preventing saving horizontal binds)
        Md.InputHandler.SendKeyBindingsToGameSettings.ControlFlowPrefix(PreventSavingBindsHook);
        Md.InputHandler.SendButtonBindingsToGameSettings.ControlFlowPrefix(PreventSavingBindsHook);

        // Avoid reflecting the audio from the save sprite
        Md.CheckpointSprite.Awake.Postfix(UnflipCheckpointSpriteAudiosource);

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    private void UnflipCheckpointSpriteAudiosource(CheckpointSprite self)
    {
        self.audioSource.gameObject.GetOrAddComponent<AudioChannelSwapper>();
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
