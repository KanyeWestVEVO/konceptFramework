using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace koncept.Tools
{
    public class Helpers : MonoBehaviour
    {
        /// <summary>
        /// Checks if a string is either empty or contains only white spaces.
        /// </summary>
        /// <param name="stringToCheck">The string to check for emptiness.</param>
        /// <returns>The truth that is sought.</returns>
        public static bool IsStringBlank(string stringToCheck)
        {
            return string.IsNullOrEmpty(stringToCheck) || string.IsNullOrWhiteSpace(stringToCheck);
        }

        public static string RemoveSpaces(string stringToClean)
        {
            stringToClean = stringToClean.Replace(" ", "");

            return stringToClean;
        }

        /// <summary>
        /// Checks if a directory exists at the given path.
        /// </summary>
        /// <param name="path">The path to check for emptiness.</param>
        /// <returns>The truth that is sought.</returns>
        public static bool DoesDirectoryExist(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Returns a script formatted for koncept CodeGen.
        /// </summary>
        /// <param name="script">The full code of the script to parse.</param>
        /// <param name="scriptName">The name of the script being parse.</param>
        /// <param name="appName">The name of the koncept App.</param>
        /// <returns></returns>
        public static string ReplaceTokens(string script, string scriptName, string appName)
        {
            script = script.Replace("##NAME##", scriptName);
            script = script.Replace("##APP##", appName);

            return script;
        }

        /// <summary>
        /// Returns a type if it exists.
        /// </summary>
        /// <param name="typeName">The type to look for.</param>
        /// <returns>The type specified.</returns>
        public static Type GetType(string typeName, string nameSpace)
        {
            Type type = null;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var types = assemblies[i].GetTypes().Where(t => t.IsClass && t.Namespace == nameSpace).ToList();
                for (int n = 0; n < types.Count; n++)
                {
                    if (typeName == types[n].Name)
                        return types[n];
                }
            }

            return type;
        }

        /// <summary>
        /// Opens a script in the Unity Editor's designated IDE.
        /// </summary>
        /// <param name="component">The type of script to open.</param>
        /// <param name="gotoLine">The line to go to.</param>
        public static void OpenComponentInVisualStudioIDE(MonoBehaviour component, string appName, int gotoLine = 0)
        {
            string componentFileName = component.GetType().ToString().Split('.').Last() + ".cs";

            string[] fileNames = Directory.GetFiles(Application.dataPath + "/" + appName,
                componentFileName,
                SearchOption.AllDirectories);

            if (fileNames.Length > 0)
            {
                string finalFileName = Path.GetFullPath(fileNames[0]);
                System.Diagnostics.Process.Start("devenv", " /edit \"" + finalFileName + "\" /command \"edit.goto " + gotoLine.ToString() + " \" ");
            }
            else
            {
                Debug.Log("File Not Found:" + componentFileName);
            }
        }

        /// <summary>
        /// Opens a script in the Unity Editor's designated IDE.
        /// </summary>
        /// <param name="scriptName">The name of the script to open.</param>
        /// <param name="gotoLine">The line to go to.</param>
        public static void OpenComponentInVisualStudioIDE(string scriptName, string appName, int gotoLine = 0)
        {
            string componentFileName = scriptName + ".cs";

            string[] fileNames = Directory.GetFiles(Application.dataPath + "/" + appName,
                componentFileName,
                SearchOption.AllDirectories);

            if (fileNames.Length > 0)
            {
                string finalFileName = Path.GetFullPath(fileNames[0]);
                System.Diagnostics.Process.Start("devenv", " /edit \"" + finalFileName + "\" /command \"edit.goto " + gotoLine.ToString() + " \" ");
            }
            else
            {
                Debug.Log("File Not Found:" + componentFileName);
            }
        }
    }
}