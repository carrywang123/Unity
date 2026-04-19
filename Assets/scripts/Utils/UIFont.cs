// 与 Assets/Resources/Fonts/NotoSansSC-Regular.otf 一致（场景/预制体 GUID：7c4a9e2f1d8b4036a5f0e9c1d2b3a4f1）
using UnityEngine;

namespace ChemLab.Utils
{
    public static class UIFont
    {
        private const string ResourcesPath = "Fonts/NotoSansSC-Regular";

        private static Font _cached;
        private static bool _loaded;

        public static Font Get()
        {
            if (!_loaded)
            {
                _loaded = true;
                _cached = Resources.Load<Font>(ResourcesPath);
                if (_cached == null)
                    _cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return _cached;
        }
    }
}
