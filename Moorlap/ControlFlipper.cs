using BepInEx.Logging;
using InControl;
using System.Reflection;
using Logger = BepInEx.Logging.Logger;

namespace Moorlap;

internal static class ControlFlipper
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource(nameof(ControlFlipper));

    private static FieldInfo _left = typeof(HeroActions).GetField(nameof(HeroActions.Left));
    private static FieldInfo _right = typeof(HeroActions).GetField(nameof(HeroActions.Right));
    
    public static bool IsFlipped()
    {
        HeroActions ha = InputHandler.Instance.inputActions;

        bool fromBinds = ha.Left.Name != "Left";
        bool fromVector = ha.MoveVector.InvertXAxis;

        if (fromBinds != fromVector)
        {
            Log.LogWarning($"{nameof(IsFlipped)}: fromBinds {fromBinds}, fromVector {fromVector}");
        }

        return fromBinds;
    }

    public static void SetFlippedControls(bool flipped)
    {
        bool currentlyFlipped = IsFlipped();
        HeroActions ha = InputHandler.Instance.inputActions;

        Log.LogDebug($"Setting controls flipped: {flipped} (current {currentlyFlipped})");

        if (currentlyFlipped ^ flipped)
        {
            PlayerAction left = ha.Left;
            PlayerAction right = ha.Right;
            _left.SetValue(ha, right);
            _right.SetValue(ha, left);
        }

        ha.MoveVector.InvertXAxis = flipped;
    }
}
