using InControl;
using MonoDetour.HookGen;

[assembly: MonoDetourTargets(typeof(UIManager))]
[assembly: MonoDetourTargets(typeof(GameManager))]
[assembly: MonoDetourTargets(typeof(GameCameras))]
[assembly: MonoDetourTargets(typeof(HeroController))]
[assembly: MonoDetourTargets(typeof(HeroActions))]
[assembly: MonoDetourTargets(typeof(PlayerAction))]
[assembly: MonoDetourTargets(typeof(InventoryPaneBase))]
[assembly: MonoDetourTargets(typeof(InventoryPaneInput))]
[assembly: MonoDetourTargets(typeof(InputHandler), GenerateControlFlowVariants = true)]
[assembly: MonoDetourTargets(typeof(InventoryMapManager))]
[assembly: MonoDetourTargets(typeof(GameMap))]
[assembly: MonoDetourTargets(typeof(CheckpointSprite))]
