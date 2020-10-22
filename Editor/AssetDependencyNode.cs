namespace Unity.ProjectAuditor.Editor
{
    public class AssetDependencyNode : DependencyNode
    {
        public override string GetPrettyName()
        {
            return location.Filename;
        }

        public override bool IsPerfCritical()
        {
            return false;
        }
    }
}
