using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using UnityEngine.UI;

public class FindResWindows : EditorWindow
{
    [MenuItem("Build/资源查找依赖,替换,去无效项")]
    static void ShowWin()
    {
        FindResWindows.GetWindow<FindResWindows>();
    }

    string findSpritePackingTag = "";

    Object prefab4CheckAltas;
    Object res;
    Object destDir;

    //把其他的替换为这个res
    Object replaceRes;

    List<Object> objs4EditorShow = new List<Object>();
    Dictionary<Object, string> objs = new Dictionary<Object, string>();

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
@"本工具是用来反向查找依赖的
比如找到依赖某个贴图a的所有预制体b
使用方法：把被依赖的资源a和要查找的目录拖到进来，点查找即可显示所有依赖资源b
这里要传一个目录是为了节省查找时间，不过目录包含东西越多查找越慢
默认查找 prefab, unity, mat, asset

如果想正向查找依赖
比如找到某预制体用到的所有资源
那么只要右键这个预制体菜单选“select dependencis ”就可以了
", MessageType.Info);
        prefab4CheckAltas = EditorGUILayout.ObjectField("prefab检查图集", prefab4CheckAltas, typeof(object), false);

        if (prefab4CheckAltas != null && GUILayout.Button("查找图集引用"))
        {
            GameObject prefab = prefab4CheckAltas as GameObject;
            //找出下面所有的Image
            Image[] images = prefab.GetComponentsInChildren<Image>(true);
            Texture2D packTexture = images[0].sprite.texture;
        }

        res = EditorGUILayout.ObjectField("被依赖资源", res, typeof(object), false);
        replaceRes = EditorGUILayout.ObjectField("替换为这个资源", replaceRes, typeof(object), false);
        destDir = EditorGUILayout.ObjectField("要查找的目录", destDir, typeof(Object), false);

        findSpritePackingTag = EditorGUILayout.TextField("要查找的SpritePackingTag", findSpritePackingTag);
        Repaint();

        string resPath = AssetDatabase.GetAssetPath(res);
        string resGuid = AssetDatabase.AssetPathToGUID(resPath);

        string replacePath = "";
        string replaceGuid = "";

        if (replaceRes != null && replaceRes != res)
        {
            replacePath = AssetDatabase.GetAssetPath(replaceRes);
            replaceGuid = AssetDatabase.AssetPathToGUID(replacePath);
        }

        if (GUILayout.Button("查找设置了SpritePackingTag的图片"))
        {
            //目录下所有的文件
            string[] arryRelativelyFilesPath = Directory.GetFiles(
                AssetDatabase.GetAssetPath(destDir), "*.*", SearchOption.AllDirectories);
            //Debug.LogError(string.Join("\n", files));

            EditorUtility.DisplayCancelableProgressBar("查找不合规资源中...", destDir.name, 0f);

            int i = 0;
            int total = arryRelativelyFilesPath.Length;
            foreach (string relativelyFilePath in arryRelativelyFilesPath)
            {
                ++i;

                EditorUtility.DisplayCancelableProgressBar("查找不合规资源中...", relativelyFilePath.ToString(), 1f * i / total);

                if (!relativelyFilePath.EndsWith(".png")
                    && !relativelyFilePath.EndsWith(".tga")
                    && !relativelyFilePath.EndsWith(".jpg"))
                {
                    continue;
                }

                TextureImporter importer = TextureImporter.GetAtPath(relativelyFilePath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                if (importer.spritePackingTag == findSpritePackingTag)
                {
                    Object obj = AssetDatabase.LoadMainAssetAtPath(relativelyFilePath);
                    string show = "null";
                    if (findSpritePackingTag != "")
                    {
                        show = findSpritePackingTag;
                    }
                    Debug.Log("pack tag:" + show+ " " + relativelyFilePath, obj);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        

        //长宽比例超过1:3
        //长,或宽超过400
        //大小超过200KB
        if (GUILayout.Button("查找不合规资源"))
        {
            //目录下所有的文件
            string[] arryRelativelyFilesPath = Directory.GetFiles(AssetDatabase.GetAssetPath(destDir), "*.*", SearchOption.AllDirectories);
            //Debug.LogError(string.Join("\n", files));

            EditorUtility.DisplayCancelableProgressBar("查找不合规资源中...", destDir.name, 0f);

            int i = 0;
            int total = arryRelativelyFilesPath.Length;
            foreach (string relativelyFilePath in arryRelativelyFilesPath)
            {
                ++i;

                EditorUtility.DisplayCancelableProgressBar("查找不合规资源中...", relativelyFilePath.ToString(), 1f * i / total);

                if (!relativelyFilePath.EndsWith(".png")
                    && !relativelyFilePath.EndsWith(".tga")
                    && !relativelyFilePath.EndsWith(".jpg"))
                {
                    continue;
                }

                TextureImporter importer = TextureImporter.GetAtPath(relativelyFilePath) as TextureImporter;
                

                if (importer == null)
                {
                    continue;
                }

            }

            EditorUtility.ClearProgressBar();
        }

        if (GUILayout.Button("查找"))
        {
            objs.Clear();
            objs4EditorShow.Clear();

#if UNITY_EDITOR_OSX
            string appDataPath = Application.dataPath;
            string output = "";
            //resPath = AssetDatabase.GetAssetPath(res);
            if (res == null)
            {
                resPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            }
			
            List<string> references = new List<string>();

            resGuid = AssetDatabase.AssetPathToGUID(resPath);

            var psi = new System.Diagnostics.ProcessStartInfo();
            psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized;
            psi.FileName = "/usr/bin/mdfind";
            psi.Arguments = "-onlyin " + Application.dataPath + " " + resGuid;
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = psi;

            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                string relativePath = "Assets" + e.Data.Replace(appDataPath, "");

                // skip the meta file of whatever we have selected
                if (relativePath == resPath + ".meta")
                    return;

                references.Add(relativePath);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    return;

                output += "Error: " + e.Data + "\n";
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit(2000);

            foreach (var file in references)
            {
                output += file + "\n";
                Object obj = AssetDatabase.LoadMainAssetAtPath(file);
                Debug.Log(file, obj);

                objs4EditorShow.Add(obj);

                string fullpath = Application.dataPath.Remove(Application.dataPath.Length - 7, 7) + "/" + file.Replace("\\", "/");
                objs.Add(obj, fullpath);
            }

            Debug.LogWarning(references.Count + " references found for object " + res.name + "\n\n" + output);
#else
            //目录下所有的文件
            var files = Directory.GetFiles(AssetDatabase.GetAssetPath(destDir), "*.*", SearchOption.AllDirectories);
            //Debug.LogError(string.Join("\n", files));

            EditorUtility.DisplayCancelableProgressBar(res.name + " 查找依赖中...", resPath, 0f);

            int i = 0;
            int total = files.Length;
            foreach (var file in files)
            {
                ++i;

                EditorUtility.DisplayCancelableProgressBar(res.name + " 查找依赖中...", file.ToString(), 1f * i / total);

                if (!file.EndsWith(".prefab") 
                    && !file.EndsWith(".mat") 
                    && !file.EndsWith(".unity") 
                    && !file.EndsWith(".asset"))
                {
                    continue;
                }

                var depends = AssetDatabase.GetDependencies(new string[] { file });
                if (-1 == System.Array.IndexOf(depends, resPath))
                    continue;

                string fullpath = Application.dataPath.Remove(Application.dataPath.Length - 7, 7) + "/" + file.Replace("\\", "/");
                objs4EditorShow.Add(AssetDatabase.LoadMainAssetAtPath(file));
                objs.Add(AssetDatabase.LoadMainAssetAtPath(file), fullpath);
     
            }

            EditorUtility.ClearProgressBar();
#endif
        }

#if UNITY_EDITOR_OSX
        //检查无引用资源
        if (destDir != null && GUILayout.Button("检查无效资源(only for mac)"))
        {
			try
			{
				string appDataPath = Application.dataPath;
				string output = "";

				List<string> references = new List<string>();

				//遍历查找
				EditorUtility.DisplayCancelableProgressBar("查找路径无引用资源中...", destDir.name, 0f);

				//目录下所有的文件
				string[] files = Directory.GetFiles(AssetDatabase.GetAssetPath(destDir), "*.*", SearchOption.AllDirectories);

				int i = 0;
				int total = files.Length;
				foreach (string fileRelativelyPath in files)
				{
					++i;

					EditorUtility.DisplayCancelableProgressBar("查找路径无引用资源中...", fileRelativelyPath, 1f * i / total);

					if (fileRelativelyPath.EndsWith(".meta")
						|| fileRelativelyPath.EndsWith(".DS_Store"))
					{
						continue;
					}

					//首先找到对应的guid
					resGuid = AssetDatabase.AssetPathToGUID(fileRelativelyPath.Replace("\\", "/"));

					//设置查找数据
					var psi = new System.Diagnostics.ProcessStartInfo();
					psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized;
					psi.FileName = "/usr/bin/mdfind";
					psi.Arguments = "-onlyin " + Application.dataPath + " " + resGuid;
					psi.UseShellExecute = false;
					psi.RedirectStandardOutput = true;
					psi.RedirectStandardError = true;

					System.Diagnostics.Process process = new System.Diagnostics.Process();
					process.StartInfo = psi;

					//清空
					references.Clear();

					process.OutputDataReceived += (sender, e) =>
					{
						if (string.IsNullOrEmpty(e.Data))
							return;

						string relativePath = "Assets" + e.Data.Replace(appDataPath, "");

						// skip the meta file of whatever we have selected
						if (relativePath == fileRelativelyPath + ".meta")
						{
							return;
						}

						references.Add(relativePath);
					};
					process.ErrorDataReceived += (sender, e) =>
					{
						if (string.IsNullOrEmpty(e.Data))
							return;

						output += "Error: " + e.Data + "\n";
					};
					process.Start();
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();

					process.WaitForExit(2000);

					if (references.Count <= 0)
					{
						Object obj = AssetDatabase.LoadMainAssetAtPath(fileRelativelyPath);
						Debug.Log(references.Count + " references found for object " + fileRelativelyPath + "\n\n" + output, obj);
						return;
					}
				}
			}
			catch(System.Exception ex)
			{
				Debug.LogError("查找无效资源出错 " + ex.ToString());
			}

            EditorUtility.ClearProgressBar();
        }
#endif

        if (replaceGuid != "" && replaceRes != res && objs.Count != 0 && GUILayout.Button("替换"))
        {
            bool bReplaced = false;
            foreach (var item in objs)
            {
                //如果是指向自己, 则跳过
                if (item.Key.name == res.name)
                {
                    continue;
                }

                string path = item.Value;
                FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);
                string con = sr.ReadToEnd();
                if (con.IndexOf(resGuid) < 0)
                {
                    sr.Close();
                    fs.Close();
                    continue;
                }

                con = con.Replace(resGuid, replaceGuid);
                sr.Close();
                fs.Close();

                FileStream fs2 = new FileStream(path, FileMode.Open, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs2);
                sw.WriteLine(con);
                sw.Close();
                fs2.Close();

                Debug.Log(string.Format("{0} replace <{1}> to <{2}> suc!", item.Key.name, res.name, replaceRes.name));

                bReplaced = true;
            }

            if (bReplaced)
            {
                AssetDatabase.Refresh();
                bReplaced = false;
            }
        }

        foreach (var obj in objs4EditorShow)
            EditorGUILayout.ObjectField(obj, typeof(Object), false);
    }
}