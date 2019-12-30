using UnityEngine;
using UnityEditor;
using System;

namespace Sean.Editor
{
    static public class EditorMenuItem
    {
        [MenuItem("UI/UIMaker")]
        public static void ShowUIMaker()
        {
            UIMakeWindow window = EditorWindow.GetWindow<UIMakeWindow>("UI制作");
            window.minSize = new Vector2(100, 100);
            window.Show();
        }
    }
}