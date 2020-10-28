using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class PlatformTests
    {
        [Test]
        public void OnlyActiveBuildTargetIssuesAreReported()
        {
            Assert.True(EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS);

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            PlayerSettings.enableMetalAPIValidation = true;

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);

            var enableMetalAPIValidationIssue = issues.FirstOrDefault(i => i.descriptor.method.Equals("enableMetalAPIValidation"));
            Assert.Null(enableMetalAPIValidationIssue);
        }
    }
}
