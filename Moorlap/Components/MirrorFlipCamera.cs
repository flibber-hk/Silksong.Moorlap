using UnityEngine;

namespace Moorlap.Components
{
    [RequireComponent(typeof(Camera))]
    public class MirrorFlipCamera : MonoBehaviour
    {
        Camera camera;

        public void Awake()
        {
            camera = GetComponent<Camera>();
        }
        public void OnPreCull()
        {
            camera.ResetWorldToCameraMatrix();
            camera.ResetProjectionMatrix();
            Vector3 scale = new(-1, 1, 1);
            camera.projectionMatrix *= Matrix4x4.Scale(scale);
        }
        public void OnPreRender()
        {
            GL.invertCulling = true;
        }
        public void OnPostRender()
        {
            GL.invertCulling = false;
        }
    }
}
