using UnityEngine;

namespace PlatformTools.Mock
{
    internal sealed class MockAdOverlay : MonoBehaviour
    {
        static MockAdOverlay _instance;
        string _label;
        GUIStyle _style;

        public static void Show(string label)
        {
            if (_instance == null)
            {
                const string mockName = "[MockAdOverlay]";
                var go = new GameObject(mockName);
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<MockAdOverlay>();
            }
            _instance._label = label;
            _instance.enabled = true;
        }

        public static void Hide()
        {
            if (_instance != null) _instance.enabled = false;
        }

        private void OnGUI()
        {
            GUI.depth = -1000;
            var rect = new Rect(0, 0, Screen.width, Screen.height);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 32 };
            GUI.Label(rect, _label, _style);
        }
    }
}