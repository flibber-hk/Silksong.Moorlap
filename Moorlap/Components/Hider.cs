using UnityEngine;

namespace Moorlap.Components;

internal class Hider : MonoBehaviour
{
    private Renderer _renderer;

    void Awake()
    {
        _renderer = gameObject.GetComponent<Renderer>();
    }

    void LateUpdate()
    {
        _renderer.enabled = false;
    }
}
