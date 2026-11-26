using GameFoundation.Runtime.Attributers;
using UnityEngine;

namespace Scorpion.Gimmicks
{
    /// <summary>
    ///     ギミックの表示情報と効果を定義する。
    /// </summary>
    [CreateAssetMenu(menuName = "Scorpion/Gimmicks/Gimmick Definition")]
    public sealed class GimmickDefinition : ScriptableObject
    {
        /// <summary> 通知に表示する名前を取得する。 </summary>
        public string DisplayName => _displayName;
        /// <summary> 通知に表示する効果説明を取得する。 </summary>
        public string Description => _description;
        /// <summary> 効果の継続秒数を取得する。 </summary>
        public float Duration => Mathf.Max(0.01f, _duration);

        internal bool HasAction => _action != null;

        [SerializeField, Tooltip("通知に表示するギミック名。")]
        private string _displayName;
        [SerializeField, TextArea, Tooltip("通知に表示する効果の説明。")]
        private string _description;
        [SerializeField, Min(0.01f), Tooltip("効果の継続秒数。飛来した妨害物の生存時間にも使用します。")]
        private float _duration = 3f;
        [SerializeReference, SubclassSelector, Tooltip("発動時に適用する効果。")]
        private IGimmickAction _action;

        /// <summary>
        ///     設定した効果を今回の発動に適用する。
        /// </summary>
        internal bool TryActivate(GimmickContext context)
        {
            return _action != null && _action.TryActivate(context);
        }
    }
}
