using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.ProjectAuditor.Editor.SettingsAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Compilation;
using UnityEditor.Macros;
using UnityEngine.Profiling;
using Assembly = System.Reflection.Assembly;
using Attribute = Unity.ProjectAuditor.Editor.SettingsAnalyzers.Attribute;
using Debug = UnityEngine.Debug;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public class SettingsAuditor : IAuditor
    {
        private readonly List<Assembly> m_Assemblies = new List<Assembly>();
        private readonly Evaluators m_Helpers = new Evaluators();

        private readonly List<KeyValuePair<string, string>> m_ProjectSettingsMapping =
            new List<KeyValuePair<string, string>>();

        private readonly Dictionary<int, ISettingsAnalyzer> m_SettingsAnalyzers =
            new Dictionary<int, ISettingsAnalyzer>();
        private List<ProblemDescriptor> m_ProblemDescriptors;

        internal SettingsAuditor(ProjectAuditorConfig config)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            m_Assemblies.Add(assemblies.First(a => a.Location.Contains("UnityEngine.dll")));
            m_Assemblies.Add(assemblies.First(a => a.Location.Contains("UnityEditor.dll")));

            // UnityEditor
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEditor.PlayerSettings",
                "Project/Player"));
            m_ProjectSettingsMapping.Add(
                new KeyValuePair<string, string>("UnityEditor.Rendering.EditorGraphicsSettings", "Project/Graphics"));

            // UnityEngine
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Physics", "Project/Physics"));
            m_ProjectSettingsMapping.Add(
                new KeyValuePair<string, string>("UnityEngine.Physics2D", "Project/Physics 2D"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.Time", "Project/Time"));
            m_ProjectSettingsMapping.Add(new KeyValuePair<string, string>("UnityEngine.QualitySettings",
                "Project/Quality"));
        }

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public void LoadDatabase(string path)
        {
            m_ProblemDescriptors = ProblemDescriptorHelper.LoadProblemDescriptors(path, "ProjectSettings");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in GetAnalyzerTypes(assembly))
                AddAnalyzer(Activator.CreateInstance(type, this) as ISettingsAnalyzer);
        }

        public IEnumerable<Type> GetAnalyzerTypes(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                if (type.GetCustomAttributes(typeof(Attribute), true).Length > 0)
                    yield return type;
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public void Audit(Action<ProjectIssue> onNewIssue, Action onComplete, IProgressBar progressBar = null)
        {
            if (progressBar != null)
                progressBar.Initialize("Analyzing Settings", "Analyzing project settings", m_SettingsAnalyzers.Count);

            foreach (var keyValuePair in m_SettingsAnalyzers)
            {
                if (progressBar != null)
                    progressBar.AdvanceProgressBar();

                var projectIssue = keyValuePair.Value.Analyze();
                if (projectIssue != null) onNewIssue(projectIssue);             
            }

            AnalyzeSettings(onNewIssue, progressBar);
            
            if (progressBar != null)
                progressBar.ClearProgressBar();

            onComplete();
        }

        private void AnalyzeSettings(Action<ProjectIssue> onNewIssue, IProgressBar progressBar = null)
        {
            var descriptors = m_ProblemDescriptors.Where(descriptor => !m_SettingsAnalyzers.ContainsKey(descriptor.id));

            if (progressBar != null)
                progressBar.Initialize("Analyzing Settings", "Analyzing project settings", descriptors.Count());

            foreach (var descriptor in descriptors)
            {
                if (progressBar != null)
                    progressBar.AdvanceProgressBar();

                SearchAndEval(descriptor, onNewIssue);
            }
            if (progressBar != null)
                progressBar.ClearProgressBar();
        }

        private void AddAnalyzer(ISettingsAnalyzer analyzer)
        {
            m_SettingsAnalyzers.Add(analyzer.GetDescriptorId(), analyzer);
        }

        private void AddIssue(ProblemDescriptor descriptor, string description, Action<ProjectIssue> onNewIssue)
        {
            var projectWindowPath = "";
            var mappings = m_ProjectSettingsMapping.Where(p => p.Key.Contains(descriptor.type));
            if (mappings.Count() > 0)
                projectWindowPath = mappings.First().Value;
            onNewIssue(new ProjectIssue
            (
                descriptor,
                description,
                IssueCategory.ProjectSettings,
                new Location {path = projectWindowPath}
            ));
        }

        private void SearchAndEval(ProblemDescriptor descriptor, Action<ProjectIssue> onNewIssue)
        {
            if (string.IsNullOrEmpty(descriptor.customevaluator))
            {
                var paramTypes = new Type[0] { };
                var args = new object[0] { };
                var found = false;

                // do we actually need to look in all assemblies? Maybe we can find a way to only evaluate on the right assembly
                foreach (var assembly in m_Assemblies)
                    try
                    {
                        var value = MethodEvaluator.Eval(assembly.Location,
                            descriptor.type, "get_" + descriptor.method, paramTypes, args);
                        
                        if (value.ToString() == descriptor.value)
                        {
                            AddIssue(descriptor, string.Format("{0}: {1}", descriptor.description, value), onNewIssue);
                        }

                        // Eval did not throw exception so we can stop iterating
                        found = true;
                        break;
                    }
                    catch (Exception)
                    {
                        // this is safe to ignore
                    }

                if (!found)
                    Debug.Log(descriptor.method + " not found in any assembly");
            }
            else
            {
                var helperType = m_Helpers.GetType();
                var theMethod = helperType.GetMethod(descriptor.customevaluator);
                var isIssue = (bool) theMethod.Invoke(m_Helpers, null);
                if (isIssue) AddIssue(descriptor, descriptor.description, onNewIssue);
            }
        }
    }
}