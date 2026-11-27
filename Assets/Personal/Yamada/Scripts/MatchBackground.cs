using UnityEngine;

namespace Scorpion.UI
{
    /// <summary>
    ///     カメラの奥にある板に、ステージごとの2D背景を表示する
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class MatchBackground : MonoBehaviour
    {
        private static readonly int BASE_MAP_ID = Shader.PropertyToID("_BaseMap");

        private Camera _camera;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _properties;

        /// <summary>
        ///     背景画像を切り替える。画像が未指定の場合はマテリアルの共通背景に戻す
        /// </summary>
        public void SetTexture(Texture2D texture)
        {
            Texture background = texture != null ? texture : _renderer.sharedMaterial.GetTexture(BASE_MAP_ID);
            _renderer.GetPropertyBlock(_properties);
            _properties.SetTexture(BASE_MAP_ID, background != null ? background : Texture2D.whiteTexture);
            _renderer.SetPropertyBlock(_properties);

            if (background != null && _camera != null)
            {
                FitToView((float)background.width / background.height);
            }
        }

        private void Awake()
        {
            _camera = GetComponentInParent<Camera>();
            _renderer = GetComponent<MeshRenderer>();
            _properties = new MaterialPropertyBlock();
        }

        /// <summary>
        ///     画像の縦横比を保ち、横長の画面でも背景の端が見えないようにする
        /// </summary>
        private void FitToView(float imageAspect)
        {
            float depth = _camera.transform.InverseTransformPoint(transform.position).z;
            float viewHeight = _camera.orthographic
                ? _camera.orthographicSize * 2f
                : 2f * depth * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewAspect = Mathf.Max(_camera.aspect, 21f / 9f);
            float height = viewHeight * Mathf.Max(1f, viewAspect / imageAspect) * 1.02f;
            transform.localScale = new Vector3(height * imageAspect, height, transform.localScale.z);
        }
    }
}
