using System;
using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class LocationTests
    {
        // [Test]
        // public void LocationIsValid()
        // {
        //     var location = new Location("some/path");
        //     Assert.IsTrue(location.IsValid());
        //     Assert.IsTrue(location.Type == LocationType.Asset);
        // }

        [Test]
        public void AssetLocationIsValid()
        {
            var location = new Location("some/path/file.cs", 0);
            Assert.IsTrue(location.IsValid());
            Assert.IsTrue(location.Filename.Equals("file.cs"));
            Assert.IsTrue(location.Path.Equals("some/path/file.cs"));
            Assert.IsTrue(location.Type == LocationType.Asset);
        }

        [Test]
        public void SettingLocationIsValid()
        {
            var location = new Location("Project/Player");
            Assert.IsTrue(location.IsValid());
            Assert.IsTrue(location.Path.Equals("Project/Player"));
            Assert.IsTrue(location.Type == LocationType.Setting);
        }

        // [Test]
        // public void LocationRelativePathFromBuiltInPackage()
        // {
        //     var location = new Location
        //     {
        //         path = "some/path/BuiltInPackages/com.unity.mypackage"
        //     };
        //     Assert.IsTrue(location.Path.Equals("com.unity.mypackage"));
        // }

        // [Test]
        // public void LocationRelativePathFromAssets()
        // {
        //     var location = new Location
        //     {
        //         path = Path.Combine(Application.dataPath, "file.cs")
        //     };
        //     Assert.IsTrue(location.relativePath.Equals("Assets/file.cs"));
        // }

        /*  [Test]
          public void UninitializedLocationIsNotValid()
          {
              var location = new Location(null);
              Assert.IsFalse(location.IsValid());
          }

          [Test]
          public void UninitializedLocationFilenameIsEmpty()
          {
              var location = new Location(null);
              Assert.IsTrue(location.Filename.Equals(string.Empty));
          }

          [Test]
          public void UninitializedLocationRelativePathIsEmpty()
          {
              var location = new Location();
              Assert.IsTrue(location.Path.Equals(string.Empty));
          }
                                               */
        // [Test]
        // public void LocationAssetDatabasePathIsCorrect()
        // {
        //     var location = new Location
        //     {
        //         path = "Assets/Dummy.cs"
        //     };
        //     Assert.IsTrue(location.assetDatabasePath.Equals("Assets/Dummy.cs"));
        // }

        // [Test]
        // public void LocationAssetDatabasePathFromPackageIsCorrect()
        // {
        //     var location = new Location
        //     {
        //         path = "Library/PackageCache/com.unity.mypackage@0.0.0-preview.1/Unity.MyPackage/Dummy.cs"
        //     };
        //     Assert.IsTrue(location.assetDatabasePath.Equals("Packages/com.unity.mypackage/Unity.MyPackage/Dummy.cs"));
        // }
    }
}
