using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Packages.ProjectAuditor.Editor.SettingsEvaluators
{
    [Unity.ProjectAuditor.Editor.SettingsAnalyzers.Attribute]
    public class StaticBatchingAndHybridPackage : ISettingsAnalyzer
    {
        private static ProblemDescriptor descriptor = new ProblemDescriptor
        {
            id = 202000,
            description = "Static-batching is enabled",
            area = "CPU",
            problem = "Static-batching is enabled and the package com.unity.rendering.hybrid is installed",
            solution = "Disable static-batching"
        }; 

        public StaticBatchingAndHybridPackage(SettingsAuditor auditor)
        {
            auditor.RegisterDescriptor(descriptor);
        }
        
        public int GetDescriptorId()
        {
            return descriptor.id;
        }

        public ProjectIssue Analyze()
        {
#if UNITY_2019_3_OR_NEWER
            if (PackageInfo.FindForAssetPath("Packages/com.unity.rendering.hybrid") != null && IsStaticBatchingEnabled(EditorUserBuildSettings.activeBuildTarget))
            {
                return new ProjectIssue(descriptor, descriptor.description, IssueCategory.ProjectSettings);
            }
#endif
            return null;
        }
        
        private static bool IsStaticBatchingEnabled(BuildTarget platform)
        {
            var method = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform", BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new NotSupportedException("Getting batching per platform is not supported");
            }

            var staticBatching = 0;
            var dynamicBatching = 0;
            var args = new object[]
            {
                platform,
                staticBatching,
                dynamicBatching    
            };
 
            method.Invoke(null, args);
            return (int)args[1] > 0;
        }
    }
}