using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TopFun.Editor
{
    /// <summary>
    /// 查找所在目录的所有mat 的shader
    /// </summary>
    public class SeekShader
    {
        static Dictionary<string, string> shaderNameMap = new Dictionary<string, string>();
        [MenuItem("Build/寻找shader")]
        static void SeekAllShaders()
        {
            Object[] m_objects = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);//选择的所以对象  

            int progress = 0;
            string title = "正在查找shader";
            foreach (Object item in m_objects)
            {
                progress++;
                string path = AssetDatabase.GetAssetPath(item);
                EditorUtility.DisplayProgressBar(title, path, 1f * progress / m_objects.Length);

                if (System.IO.Path.GetExtension(path) != "")//判断路径是否为空  
                {
                    if (item.GetType() == typeof(Material))
                    {
                        Material m = (Material)item;
                        Debug.Log("Material=" + m.name + "   Shader=" + m.shader.name);
                        string[] shaders = m.shader.name.Split('/');

                        string key = shaders[shaders.Length - 1];
                        string value = "";
                        if (!shaderNameMap.TryGetValue(key, out value))
                        {
                            shaderNameMap.Add(key, m.shader.name);
                        }
                    }

                }

            }

            AssetDatabase.Refresh();
            Debug.Log("Complete!");

            int count = 0;
            foreach (var sValue in shaderNameMap)
            {
                count++;
                Debug.Log("shaderNameList [" + count + "]=" + sValue.Value);
            }
            shaderNameMap.Clear();
            EditorUtility.ClearProgressBar();
        }

    }
}
