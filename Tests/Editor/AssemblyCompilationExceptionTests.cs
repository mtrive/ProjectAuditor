using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Utils;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class AssemblyCompilationExceptionTests
    {
        private ScriptResource m_ScriptResource;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptResource = new ScriptResource("MyClass.cs", @"
class MyClass {
#if !UNITY_EDITOR
    asd
#endif
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_ScriptResource.Delete();
        }
        
        [Test]
        public void ExceptionIsThrownOnCompilationError()
        {
            var exceptionThrown = false;
            try
            {
                using (var compilationHelper = new AssemblyCompilationHelper())
                {
                    compilationHelper.Compile();
                }
            }
            catch (AssemblyCompilationException)
            {
                exceptionThrown = true;
            }
            
            Assert.True(exceptionThrown);
        }
    }
}