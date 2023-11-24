using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FrostweepGames.MicrophonePro
{
    public sealed class MicrophonePostProcess
    {
        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (!GeneralConfig.Config.usePostProcessBuildInject)
                return;

            if (target == BuildTarget.WebGL)
            {
                string indexPath = $"{pathToBuiltProject}/index.html";

                if (System.IO.File.Exists(indexPath))
                {
                    string indexData = System.IO.File.ReadAllText(indexPath);

                    string dependencies =
    @"    <!-- MICROPHONE PRO START -->
    <script src='./microphone.js'></script>
    <!-- MICROPHONE PRO END -->";

                    if (!indexData.Contains(dependencies))
                    {
                        indexData = indexData.Insert(indexData.IndexOf("</head>"), $"\n{dependencies}\n");

                        System.IO.File.WriteAllText(indexPath, indexData);
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError("Process of MICROPHONE PRO failed due to: index.html not found!");
                }

                string pluginFolder = GetPluginFolderPath();

                File.Copy($"{pluginFolder}/Scripts/Native/microphone.txt", $"{pathToBuiltProject}/microphone.js", true);
                File.Copy($"{pluginFolder}/Scripts/Native/mic-worklet-module.txt", $"{pathToBuiltProject}/mic-worklet-module.js", true);
            }
        }

        private static string GetPluginFolderPath()
        {
            return SearchFolder(Application.dataPath, "MicrophonePro");
        }

        private static string SearchFolder(string path, string name)
        {
            string[] directories = System.IO.Directory.GetDirectories(path);

            for (int i = 0; i < directories.Length; i++)
            {
                if (directories[i].EndsWith(name))
                {
                    return directories[i];
                }
                else
                {
                    string exportPath = SearchFolder(directories[i], name);

                    if (!string.IsNullOrEmpty(exportPath))
                        return exportPath;
                }
            }

            return null;
        }
    }
}