using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;

namespace Packages.ProjectAuditor.Editor.SettingsEvaluators
{
    public interface ISettingsAnalyzer
    {
        int GetDescriptorId();

        ProjectIssue Analyze();
    }
}