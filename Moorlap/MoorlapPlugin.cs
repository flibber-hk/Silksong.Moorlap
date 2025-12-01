using BepInEx;
using MonoDetour.DetourTypes;
using Moorlap.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using Silksong.UnityHelper.Extensions;
using System.Collections.Generic;

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
        // We can't use HC.OnDestroy because that is sometimes called mid-gameplay for some reason
        SceneManager.activeSceneChanged += DoUnflip;

        // Unflip controls when interacting with an inventory pane
        Md.InventoryPaneInput.PressDirection.Prefix(RemoveInvFlip);

        // Manually unflip controls while paused because I don't want to deal with unity event systems
        Md.UIManager.UIGoToPauseMenu.Postfix(_ => ControlFlipper.SetFlippedControls(false));
        Md.UIManager.UIClosePauseMenu.Postfix(_ => ControlFlipper.SetFlippedControls(true));

        // Map stuff
        MapFlipper.Hook();

        // Prevent saving the modified key bindings (this could be made better by only preventing saving horizontal binds)
        Md.InputHandler.SendKeyBindingsToGameSettings.ControlFlowPrefix(PreventSavingBindsHook);
        Md.InputHandler.SendButtonBindingsToGameSettings.ControlFlowPrefix(PreventSavingBindsHook);

        // Avoid reflecting the audio from the save sprite
        Md.CheckpointSprite.Awake.Postfix(UnflipCheckpointSpriteAudiosource);

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    private static readonly HashSet<string> _menuScenes =
    [
        "quit_to_menu",
        "menu_title"
    ];

    private void DoUnflip(Scene oldScene, Scene newScene)
    {
        string sceneName = newScene.name.ToLower();
        
        if (_menuScenes.Contains(sceneName))
        {
            ControlFlipper.SetFlippedControls(false);
        }
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
