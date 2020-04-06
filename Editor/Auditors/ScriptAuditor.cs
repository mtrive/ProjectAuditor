using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using Attribute = Unity.ProjectAuditor.Editor.InstructionAnalyzers.Attribute;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public class ScriptAuditor : IAuditor
    {
        private readonly ProjectAuditorConfig m_Config;
        private readonly List<IInstructionAnalyzer> m_InstructionAnalyzers = new List<IInstructionAnalyzer>();
        private readonly List<OpCode> m_OpCodes = new List<OpCode>();
        private string[] m_AssemblyNames;
        private List<ProblemDescriptor> m_ProblemDescriptors;

        private Thread m_AssemblyAnalysisThread;

        internal ScriptAuditor(ProjectAuditorConfig config)
        {
            m_Config = config;
        }

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            if (m_Config.enableBackgroundAnalysis && m_AssemblyAnalysisThread != null)
                m_AssemblyAnalysisThread.Join();

            using (var compilationHelper = new AssemblyCompilationHelper())
            {
                var compiledAssemblyPaths = compilationHelper.Compile(progressBar).Select(Path.GetFullPath);
                if (m_Config.enableBackgroundAnalysis)
                {
                    // if (m_AssemblyAnalysisThread == null)
                    {
                        m_AssemblyAnalysisThread = new Thread(() => AnalyzeAssemblies(compiledAssemblyPaths, onIssueFound, onComplete));
                        m_AssemblyAnalysisThread.Name = "Assembly Analysis";
                        m_AssemblyAnalysisThread.Priority = ThreadPriority.BelowNormal;
                    }
                    m_AssemblyAnalysisThread.Start();
                }
                else
                {
                    AnalyzeAssemblies(compiledAssemblyPaths, onIssueFound, onComplete, progressBar);
                }
            }
        }

        private void AnalyzeAssemblies(IEnumerable<string> compiledAssemblyPaths, Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            var compiledAssemblyDirectories = compiledAssemblyPaths.Select(Path.GetDirectoryName).Distinct();
            var callCrawler = new CallCrawler();
            var issues = new List<ProjectIssue>();
            using (var assemblyResolver = new DefaultAssemblyResolver())
            {
                foreach (var dir in AssemblyHelper.GetPrecompiledAssemblyDirectories())
                    assemblyResolver.AddSearchDirectory(dir);

                foreach (var dir in AssemblyHelper.GetPrecompiledEngineAssemblyDirectories())
                    assemblyResolver.AddSearchDirectory(dir);

                foreach (var dir in compiledAssemblyDirectories)
                    assemblyResolver.AddSearchDirectory(dir);

                if (progressBar != null)
                    progressBar.Initialize("Analyzing Scripts", "Analyzing project scripts",
                        compiledAssemblyPaths.Count());

                // Analyse all Player assemblies
                foreach (var assemblyPath in compiledAssemblyPaths)
                {
                    if (progressBar != null)
                        progressBar.AdvanceProgressBar(string.Format("Analyzing {0} scripts",
                            Path.GetFileName(assemblyPath)));

                    if (!File.Exists(assemblyPath))
                    {
                        Debug.LogError(assemblyPath + " not found.");
                        continue;
                    }

                    AnalyzeAssembly(assemblyPath, assemblyResolver, callCrawler, (issue) =>
                    {
                        issues.Add(issue);
                        onIssueFound(issue);
                        //          Thread.Sleep(20);
                    });
                }
            }

            if (progressBar != null)
                progressBar.ClearProgressBar();

            callCrawler.BuildCallHierarchies(issues, progressBar);

            onComplete();
        }

        public void LoadDatabase(string path)
        {
            m_ProblemDescriptors = ProblemDescriptorHelper.LoadProblemDescriptors(path, "ApiDatabase");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in GetAnalyzerTypes(assembly))
                    AddAnalyzer(Activator.CreateInstance(type, this) as IInstructionAnalyzer);
        }

        public IEnumerable<Type> GetAnalyzerTypes(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
                if (type.GetCustomAttributes(typeof(Attribute), true).Length > 0)
                    yield return type;
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            // TODO: check for id conflict
            m_ProblemDescriptors.Add(descriptor);
        }

        private void AnalyzeAssembly(string assemblyPath, IAssemblyResolver assemblyResolver, CallCrawler callCrawler, Action<ProjectIssue> onIssueFound)
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath,
                new ReaderParameters {ReadSymbols = true, AssemblyResolver = assemblyResolver}))
            {
                foreach (var methodDefinition in MonoCecilHelper.AggregateAllTypeDefinitions(assembly.MainModule.Types)
                         .SelectMany(t => t.Methods))
                {
                    if (!methodDefinition.HasBody)
                        continue;

                    AnalyzeMethodBody(assembly, methodDefinition, callCrawler, onIssueFound);
                }
            }
        }

        private void AnalyzeMethodBody(AssemblyDefinition a, MethodDefinition caller,
            CallCrawler callCrawler, Action<ProjectIssue> onIssueFound)
        {
            if (!caller.DebugInformation.HasSequencePoints)
                return;

            var callerNode = new CallTreeNode(caller);
            var perfCriticalContext = IsPerformanceCriticalContext(caller);

            foreach (var inst in caller.Body.Instructions.Where(i => m_OpCodes.Contains(i.OpCode)))
            {
                SequencePoint s = null;
                for (var i = inst; i != null; i = i.Previous)
                {
                    s = caller.DebugInformation.GetSequencePoint(i);
                    if (s != null)
                        break;
                }

                Location location = null;
                if (s != null)
                {
                    location = new Location
                    {path = s.Document.Url.Replace("\\", "/"), line = s.StartLine};
                    callerNode.location = location;
                }
                else
                {
                    // sequence point not found. Assuming caller.IsHideBySig == true
                }

                if (inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt)
                    callCrawler.Add(caller, (MethodReference)inst.Operand, location, perfCriticalContext);

                foreach (var analyzer in m_InstructionAnalyzers)
                    if (analyzer.GetOpCodes().Contains(inst.OpCode))
                    {
                        var projectIssue = analyzer.Analyze(caller, inst);
                        if (projectIssue != null)
                        {
                            projectIssue.callTree.perfCriticalContext = perfCriticalContext;
                            projectIssue.callTree.AddChild(callerNode);
                            projectIssue.location = location;
                            projectIssue.assembly = a.Name.Name;

                            onIssueFound(projectIssue);
                        }
                    }
            }
        }

        private void AddAnalyzer(IInstructionAnalyzer analyzer)
        {
            m_InstructionAnalyzers.Add(analyzer);
            m_OpCodes.AddRange(analyzer.GetOpCodes());
        }

        public static IEnumerable<ProjectIssue> FindScriptIssues(ProjectReport projectReport, string relativePath)
        {
            return projectReport.GetIssues(IssueCategory.ApiCalls).Where(i => i.relativePath.Equals(relativePath));
        }

        private static bool IsPerformanceCriticalContext(MethodDefinition methodDefinition)
        {
            if (MonoBehaviourAnalysis.IsMonoBehaviour(methodDefinition.DeclaringType) &&
                MonoBehaviourAnalysis.IsMonoBehaviourUpdateMethod(methodDefinition))
                return true;
            if (ComponentSystemAnalysis.IsComponentSystem(methodDefinition.DeclaringType) &&
                ComponentSystemAnalysis.IsOnUpdateMethod(methodDefinition))
                return true;
            return false;
        }
    }
}
