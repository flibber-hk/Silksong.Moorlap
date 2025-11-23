using MonoDetour.HookGen;

[assembly: MonoDetourTargets(typeof(GameCameras))]
[assembly: MonoDetourTargets(typeof(HeroController))]
[assembly: MonoDetourTargets(typeof(InventoryPaneInput))]
[assembly: MonoDetourTargets(typeof(InputHandler), GenerateControlFlowVariants = true)]
[assembly: MonoDetourTargets(typeof(CheckpointSprite))]
[assembly: MonoDetourTargets(typeof(InventoryMapManager))]
[assembly: MonoDetourTargets(typeof(GameMap))]
[assembly: MonoDetourTargets(typeof(MapMarkerMenu))]
[assembly: MonoDetourTargets(typeof(InventoryItemWideMapZone))]
