#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MorvaridEssential.Editor
{
    public class SetupWindow : EditorWindow
    {
        // Source paths relative to project root
        private const string AnimaloSourcePath = "Packages/com.morvarid.essential/Runtime/AutoAnim/Actions/Animation Actions";
        private const string ButtonScaleTweenSourcePath = "Packages/com.morvarid.essential/Runtime/ButtonAnim/Profiles/Actions";
        
        // Destination paths in Assets folder
        private const string AnimaloDestPath = "Assets/MorvaridEssential/DefaultActions/Animalo";
        private const string ButtonScaleTweenDestPath = "Assets/MorvaridEssential/DefaultActions/ButtonScaleTween";

        private string GetFullSourcePath(string relativePath)
        {
            // Get project root (parent of Assets folder)
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, relativePath).Replace('\\', '/');
        }

        private string GetFullDestPath(string relativePath)
        {
            // Assets folder path
            return Path.Combine(Application.dataPath, relativePath.Replace("Assets/", "")).Replace('\\', '/');
        }

        [MenuItem("Tools/Morvarid Essential/Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<SetupWindow>("Setup");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Default Actions Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Copy default action assets from package to Assets folder for easy access in Unity Project Window.", MessageType.Info);
            
            EditorGUILayout.Space(20);

            // Animalo Button
            DrawCopyButton(
                "Copy Animalo Default Actions",
                AnimaloSourcePath,
                AnimaloDestPath,
                "*.asset"
            );

            EditorGUILayout.Space(10);

            // ButtonScaleTween Button
            DrawCopyButton(
                "Copy ButtonScaleTween Default Actions",
                ButtonScaleTweenSourcePath,
                ButtonScaleTweenDestPath,
                "*.asset"
            );

            EditorGUILayout.Space(20);
            
            if (GUILayout.Button("Refresh Asset Database", GUILayout.Height(30)))
            {
                AssetDatabase.Refresh();
            }
        }

        private void DrawCopyButton(string buttonLabel, string sourcePath, string destPath, string searchPattern)
        {
            // Check status
            var status = GetCopyStatus(sourcePath, destPath, searchPattern);
            bool allCopied = status.missingCount == 0;
            
            // Set button color
            var originalColor = GUI.color;
            if (allCopied)
            {
                GUI.color = Color.gray;
            }
            else
            {
                GUI.color = Color.green;
            }

            // Button
            string buttonText = allCopied 
                ? $"{buttonLabel} (All Copied)" 
                : $"{buttonLabel} ({status.missingCount} remaining)";
            
            if (GUILayout.Button(buttonText, GUILayout.Height(40)))
            {
                CopyAssets(sourcePath, destPath, searchPattern);
            }

            GUI.color = originalColor;

            // Status info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Total: {status.totalCount} | Copied: {status.copiedCount} | Missing: {status.missingCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private (int totalCount, int copiedCount, int missingCount) GetCopyStatus(string sourcePath, string destPath, string searchPattern)
        {
            string fullSourcePath = GetFullSourcePath(sourcePath);
            string fullDestPath = GetFullDestPath(destPath);

            if (!Directory.Exists(fullSourcePath))
            {
                return (0, 0, 0);
            }

            var sourceFiles = Directory.GetFiles(fullSourcePath, searchPattern);
            int totalCount = 0;
            int copiedCount = 0;
            int missingCount = 0;

            foreach (var sourceFile in sourceFiles)
            {
                // Skip .meta files
                if (sourceFile.EndsWith(".meta")) continue;

                totalCount++;
                string fileName = Path.GetFileName(sourceFile);
                string destFile = Path.Combine(fullDestPath, fileName);

                if (File.Exists(destFile))
                {
                    copiedCount++;
                }
                else
                {
                    missingCount++;
                }
            }

            return (totalCount, copiedCount, missingCount);
        }

        private void CopyAssets(string sourcePath, string destPath, string searchPattern)
        {
            string fullSourcePath = GetFullSourcePath(sourcePath);
            string fullDestPath = GetFullDestPath(destPath);

            if (!Directory.Exists(fullSourcePath))
            {
                EditorUtility.DisplayDialog("Error", $"Source path not found: {fullSourcePath}", "OK");
                return;
            }

            // Create destination directory if it doesn't exist
            if (!Directory.Exists(fullDestPath))
            {
                Directory.CreateDirectory(fullDestPath);
            }

            var sourceFiles = Directory.GetFiles(fullSourcePath, searchPattern);
            int copied = 0;
            int skipped = 0;

            foreach (var sourceFile in sourceFiles)
            {
                // Skip .meta files (we'll copy them separately)
                if (sourceFile.EndsWith(".meta")) continue;

                string fileName = Path.GetFileName(sourceFile);
                string destFile = Path.Combine(fullDestPath, fileName);
                string sourceMetaFile = sourceFile + ".meta";
                string destMetaFile = destFile + ".meta";

                // Only copy if destination doesn't exist
                if (!File.Exists(destFile))
                {
                    File.Copy(sourceFile, destFile, true);
                    copied++;

                    // Copy .meta file if it exists
                    if (File.Exists(sourceMetaFile))
                    {
                        File.Copy(sourceMetaFile, destMetaFile, true);
                    }
                }
                else
                {
                    skipped++;
                }
            }

            AssetDatabase.Refresh();

            string message = $"Copied {copied} asset(s).\n";
            if (skipped > 0)
            {
                message += $"{skipped} asset(s) already exist and were skipped.";
            }

            EditorUtility.DisplayDialog("Copy Complete", message, "OK");
        }
    }
}
#endif

