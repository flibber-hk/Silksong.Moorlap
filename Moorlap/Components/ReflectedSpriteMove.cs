using UnityEngine;

namespace Moorlap.Components;

public class ReflectedSpriteMove : MonoBehaviour
{
    public GameObject? Original
    {
        get => field;
        set
        {
            field = value;
            _prevPosition = value!.transform.localPosition;
        }
    }
    private Vector3 _prevPosition;

    void OnEnable()
    {
        if (Original != null)
        {
            _prevPosition = Original.transform.localPosition;
            transform.localPosition = Original.transform.localPosition;
        }
    }

    void Update()
    {
        if (Original == null) return;
        Vector3 newPosition = Original.transform.localPosition;
        Vector3 delta = newPosition - _prevPosition;

        Vector3 reflectedDelta = new(-delta.x, delta.y, delta.z);
        transform.localPosition += reflectedDelta;

        _prevPosition = newPosition;
    }
}
