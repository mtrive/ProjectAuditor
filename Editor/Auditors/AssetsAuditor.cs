using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public class AssetsAuditor : IAuditor
    {
        private static readonly ProblemDescriptor s_Descriptor = new ProblemDescriptor
            (
            302000,
            "Resources folder asset",
            Area.BuildSize,
            "The Resources folder is a common source of many problems in Unity projects. Improper use of the Resources folder can bloat the size of a project’s build, lead to uncontrollable excessive memory utilization, and significantly increase application startup times.",
            "Use AssetBundles when possible"
            );

        private readonly List<ProblemDescriptor> m_ProblemDescriptors = new List<ProblemDescriptor>();

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
            RegisterDescriptor(s_Descriptor);
        }

        public void Reload(string path)
        {
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            AnalyzeResources(onIssueFound);
            onComplete();
        }

        private static void AnalyzeResources(Action<ProjectIssue> onIssueFound)
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var allResources = allAssetPaths.Where(path => path.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase) >= 0);
            var allPlayerResources = allResources.Where(path => path.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) == -1);

            var assetPathsDict = new Dictionary<string, DependencyNode>();
            foreach (var assetPath in allPlayerResources.Where(ass => ass.Contains("ModularTrack")))
            {
                if ((File.GetAttributes(assetPath) & FileAttributes.Directory) == FileAttributes.Directory)
                    continue;

                var root = AddResourceAsset(assetPath, assetPathsDict, onIssueFound, null);
                var dependencies = AssetDatabase.GetDependencies(assetPath, true);
                foreach (var depAssetPath in dependencies)
                {
                    // skip self
                    if (depAssetPath.Equals(assetPath))
                        continue;

                    root = AddResourceAsset(depAssetPath, assetPathsDict, onIssueFound, root);
                }
            }
        }

        private static DependencyNode AddResourceAsset(
            string assetPath, Dictionary<string, DependencyNode> assetPathsDict, Action<ProjectIssue> onIssueFound, DependencyNode parent)
        {
            // skip C# scripts
            if (Path.GetExtension(assetPath).Equals(".cs"))
                return parent;

            if (assetPathsDict.ContainsKey(assetPath))
            {
                if (parent == null)
                    return assetPathsDict[assetPath];
                parent.AddChild(assetPathsDict[assetPath]);
                return parent;
            }

            var location = new Location(assetPath);
            var dependencyNode = new AssetDependencyNode
            {
                location = new Location(assetPath)
            };
            if (parent == null)
                parent = dependencyNode;
            else
            {
                parent.AddChild(dependencyNode);
            }

            onIssueFound(new ProjectIssue
                (
                    s_Descriptor,
                    Path.GetFileNameWithoutExtension(location.Path),
                    IssueCategory.Assets,
                    location
                )
                {
                    dependencies = parent
                }
            );

            assetPathsDict.Add(assetPath, dependencyNode);

            return parent;
        }
    }
}
