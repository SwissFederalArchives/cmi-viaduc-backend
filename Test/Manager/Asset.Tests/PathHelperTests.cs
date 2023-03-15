using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMI.Engine.Asset.PostProcess;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Manager.Asset.Tests
{
    [TestFixture]
    public class PathHelperTests
    {
        [Test]
        public void ReservedCharsAreRemoved()
        {
            // Arrange
            var fileOrPathName = @"this is a test, to see & if & chars are ? rem'oved or sub(stituted) fr*om filename";

            // Act
            var result = PathHelper.CreateShortValidUrlName(fileOrPathName, false);
            // Assert
            result.Should().Be("this_is_a_test__to_see___if___cha_4FA714");
        }

        [Test]
        public void ExtensionsAreHandledCorrectly()
        {
            // Arrange
            var fileOrPathName = @"this is ?,% a test.json";

            // Act
            var result = PathHelper.CreateShortValidUrlName(fileOrPathName, true);
            // Assert
            result.Should().Be("this_is_____a_test.json");
        }

        [Test]
        public void ExtensionsAreHandledCorrectlyWithVeryLongFileName()
        {
            // Arrange
            var fileOrPathName = @"this is ?,% a test for a file whose name is really long.json";

            // Act
            var result = PathHelper.CreateShortValidUrlName(fileOrPathName, true);
            // Assert
            result.Should().Be("this_is_____a_test_for_a_file_who_C8A8BF.json");
        }

        [Test]
        public void PathNameWithSubdirectoriesIsHandledCorrectly()
        {
            // Arrange
            var fileOrPathName = @"C:\Temp\This is a very long path name that should be, truncated\This is a very long path name that should be, truncated\testfile.txt";

            // Act
            var result = PathHelper.CreateShortValidUrlName(fileOrPathName, true);
            
            // Assert
            result.Should().Be(@"C:\Temp\This_is_a_very_long_path_name_tha_0E79E0\This_is_a_very_long_path_name_tha_0E79E0\testfile.txt");
        }

        [Test]
        public void RelativePathNameWithSubdirectoriesIsHandledCorrectly()
        {
            // Arrange
            var fileOrPathName = @"This is a very long path name that should be, truncated\This is a very long path name that should be, truncated\testfile.txt";

            // Act
            var result = PathHelper.CreateShortValidUrlName(fileOrPathName, true);

            // Assert
            result.Should().Be(@"This_is_a_very_long_path_name_tha_0E79E0\This_is_a_very_long_path_name_tha_0E79E0\testfile.txt");
        }

        [Test]
        public void DiacriticsAreNormalizedCorrectly()
        {
            // Arrange
            var fileOrPathName = @"Die Hüte der höheren Lärchen sind Nadeln die nie ausfallen auch nicht im Winter\éàè£àéáíóç\äüöÄÖÜ.txt";

            // Act
            var result = PathHelper.CreateShortValidUrlName(fileOrPathName, true);

            // Assert
            result.Should().Be(@"Die_Hute_der_hoheren_Larchen_sind_CD0DE7\eae£aeaioc\auoAOU.txt");
        }
    }
}
