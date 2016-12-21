﻿#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Edwon.VR.Gesture
{
    public enum MoveOption { ToPlugin, ToDev }

    public class VRGestureDevTool : ScriptableObject
    {
        const string GESTURE_DEV_PATH = @"Assets/Edwon/VR/Gesture Dev/";
        const string GESTURE_PLUGIN_PATH = @"Assets/Edwon/VR/Gesture/";
        const string GESTURE_PLUGIN_EXPORT_PATH = @"/Edwon/VR/Gesture/";

        const string EXAMPLES_PATH = "Examples/";
        const string INTEGRATIONS_PATH = "Integrations/";

        const string PLUGIN_PACKAGE_NAME = "VR_INFINITE_GESTURE.unitypackage";
        const string PLAYMAKER_PACKAGE_NAME = "PlaymakerIntegration.unitypackage";

        public void BuildAndExportPlugin()
        {
            MoveIntegrations(MoveOption.ToPlugin);
            ExportIntegrationsPackages();
            MoveIntegrations(MoveOption.ToDev);
            ExportPlugin();
            DeleteGeneratedPackages();
        }

        public void ExportPlugin()
        {
            string fromPath = 
                GESTURE_PLUGIN_PATH.Substring(0, GESTURE_PLUGIN_PATH.Length - 1);
            string exportPath =
                Application.dataPath + GESTURE_PLUGIN_EXPORT_PATH + PLUGIN_PACKAGE_NAME;
            AssetDatabase.ExportPackage(fromPath, exportPath, ExportPackageOptions.Recurse);
            AssetDatabase.Refresh();
        }

        public void MoveExamples(MoveOption moveOption)
        {

        }

        public void MoveIntegrations(MoveOption moveOption)
        {
            // first move playmaker folder from dev to normal
            // this way it will re-import to the correct spot when users re-import the package
            string playmakerDev = GESTURE_DEV_PATH + INTEGRATIONS_PATH + "Playmaker/";
            string playmakerPlugin = GESTURE_PLUGIN_PATH + INTEGRATIONS_PATH + "Playmaker/";
            switch (moveOption)
            {
                case MoveOption.ToPlugin:
                    MoveFolder(playmakerDev, playmakerPlugin);
                    break;
                case MoveOption.ToDev:
                    MoveFolder(playmakerPlugin, playmakerDev);
                    break;
            }
        }

        public void ExportIntegrationsPackages()
        {
            string fromPath = 
                GESTURE_PLUGIN_PATH + INTEGRATIONS_PATH + "Playmaker";

            string exportPath = 
                Application.dataPath + GESTURE_PLUGIN_EXPORT_PATH + INTEGRATIONS_PATH + PLAYMAKER_PACKAGE_NAME;

            ExportPackage(fromPath, exportPath);
        }

        public void DeleteGeneratedPackages()
        {
            AssetDatabase.DeleteAsset(GESTURE_PLUGIN_PATH + INTEGRATIONS_PATH + PLAYMAKER_PACKAGE_NAME);
            AssetDatabase.Refresh();
        }

        #region UTILS

        void MoveFolder(string from, string to)
        {
            FileUtil.MoveFileOrDirectory(from, to);
            AssetDatabase.Refresh();
        }

        void ExportPackage(string fromPath, string exportPath)
        {
            AssetDatabase.ExportPackage(
                fromPath,
                exportPath,
                ExportPackageOptions.Recurse);
            AssetDatabase.Refresh();
        }

        #endregion
    }
}

#endif