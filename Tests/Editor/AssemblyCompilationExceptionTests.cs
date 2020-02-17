using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.TestTools;

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
            LogAssert.ignoreFailingMessages = true;

            var exceptionThrown = false;
            try
            {
                using (var compilationHelper = new AssemblyCompilationHelper())
                {
                    compilationHelper.Compile();
                }
            }
            {
                exceptionThrown = true;
            }
            LogAssert.ignoreFailingMessages = false;
            
            Assert.True(exceptionThrown);
        }
    }
}