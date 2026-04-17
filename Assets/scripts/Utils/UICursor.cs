using UnityEngine;

namespace ChemLab.Utils
{
    public static class UICursor
    {
        // 约定：把“手型光标”贴图放到 Resources/Cursors/hand.png
        // Unity 加载时不带扩展名：Resources.Load<Texture2D>("Cursors/hand")
        private const string DefaultHandCursorResourcePath = "Cursors/hand";

        private static Texture2D _handCursor;
        private static Vector2 _handHotspot;
        private static bool _loaded;

        /// <summary>
        /// 可选：在启动时预加载手型光标；如果找不到资源，会保持使用系统默认光标。
        /// </summary>
        public static void Preload(string handCursorResourcePath = DefaultHandCursorResourcePath, Vector2? hotspot = null)
        {
            if (_loaded) return;
            _loaded = true;

            _handCursor = Resources.Load<Texture2D>(handCursorResourcePath);
            _handHotspot = hotspot ?? Vector2.zero;
        }

        public static void SetHand()
        {
            if (!_loaded) Preload();
            if (_handCursor == null) return; // 没有配置资源就不强行切

            Cursor.SetCursor(_handCursor, _handHotspot, CursorMode.Auto);
        }

        public static void SetDefault()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}

