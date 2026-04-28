using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Utils
{
    /// <summary>
    /// 把需要“禁用 hover 上移效果”的 Button 拖进列表里即可。
    /// 不影响手型光标/点击，只会把 UICursorHoverTarget.hoverMoveUp 设为 0。
    /// </summary>
    public sealed class DisableButtonHoverMoveUp : MonoBehaviour
    {
        [Header("拖入不希望上移的按钮")]
        public List<Button> buttons = new List<Button>();

        [Tooltip("也处理子物体上的 Button（例如按钮在子节点上）")]
        public bool includeChildren = false;

        private void OnEnable()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 编辑器里改完列表立刻生效，方便预览
            if (!isActiveAndEnabled) return;
            Apply();
        }
#endif

        public void Apply()
        {
            if (buttons == null) return;

            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                if (btn == null) continue;

                DisableMoveUpOn(btn.gameObject);

                if (includeChildren)
                {
                    var childBtns = btn.GetComponentsInChildren<Button>(true);
                    for (int j = 0; j < childBtns.Length; j++)
                    {
                        var cb = childBtns[j];
                        if (cb == null) continue;
                        DisableMoveUpOn(cb.gameObject);
                    }
                }
            }
        }

        private static void DisableMoveUpOn(GameObject go)
        {
            if (go == null) return;
            var hover = go.GetComponent<UICursorHoverTarget>();
            if (hover == null) hover = go.AddComponent<UICursorHoverTarget>();
            hover.hoverMoveUp = 0f;
        }
    }
}

