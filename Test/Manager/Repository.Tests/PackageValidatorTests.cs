using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CMI.Contract.Common;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CMI.Manager.Repository.Tests
{
    [TestFixture]
    public class PackageValidatorTests
    {
        private const int MaxLevel = 5;
        private const int NumberOfFoldersPerLevel = 2;
        private const int NumberOfFilesPerLevel = 2;
        private string[] fileTestData;
        private string[] folderTestData;
        private int folderIndexCounter;
        private int fileIndexCounter;

        public RepositoryPackage CreateTestData()
        {
            var package = new RepositoryPackage();

            fileTestData = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, "FileTestData.csv"), Encoding.UTF8);
            folderTestData = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, "FolderTestData.csv"), Encoding.UTF8);
            folderIndexCounter = 0;
            fileIndexCounter = 0;
            CreateTestFiles(package.Files);
            CreateTestFolders(package.Folders, 1);

            return package;
        }


        private void CreateTestFolders(List<RepositoryFolder> folderList, int level)
        {
            if (level > MaxLevel)
            {
                return;
            }

            for (var i = 0; i < NumberOfFoldersPerLevel; i++)
            {
                var folderParts = folderTestData[folderIndexCounter].Split(';');
                var newFolder = new RepositoryFolder
                {
                    Id = folderParts[0],
                    LogicalName = folderParts[1]
                };
                folderList.Add(newFolder);
                folderIndexCounter++;
                if (folderIndexCounter == folderTestData.Length)
                {
                    folderIndexCounter = 0;
                }

                // Create subfolders
                CreateTestFolders(newFolder.Folders, level + 1);
                CreateTestFiles(newFolder.Files);
            }
        }

        private void CreateTestFiles(List<RepositoryFile> files)
        {
            for (var i = 0; i < NumberOfFilesPerLevel; i++)
            {
                var fileParts = fileTestData[fileIndexCounter].Split(';');
                var newFile = new RepositoryFile
                {
                    Id = fileParts[0],
                    LogicalName = fileParts[1]
                };
                files.Add(newFile);
                fileIndexCounter++;
                if (fileIndexCounter == fileTestData.Length)
                {
                    fileIndexCounter = 0;
                }
            }
        }

        [Test]
        public void Invalid_chars_in_path_are_replaced_with_underscore()
        {
            // Arrange
            var package = CreateTestData();
            var validator = new PackageValidator();

            // Act
            validator.CreateValidNames(package);

            // Assert
            package.Folders.First(f => f.Id == "Dir00000001").PhysicalName.Should().Be("Das ist ein Ordner_name mit _ ungültigen _ Zeichen _");
        }

        [Test]
        public void Invalid_chars_in_folder_with_long_name_are_replaced_with_underscore()
        {
            // Arrange
            var package = CreateTestData();
            var validator = new PackageValidator();

            // Act
            validator.CreateValidNames(package);

            // Assert
            package.Folders.First(f => f.Id == "Dir00000001").Folders.First(f => f.Id == "Dir00000002").PhysicalName.Should().Be(
                "Das ist ein Ordner_name mit _ ungültigen _ Zeichen _ und der dann noch überaus lang ist und viele Zeichen enthält bis er dann irgend wann");
        }

        [Test]
        public void Invalid_chars_in_file_are_replaced_with_underscore()
        {
            // Arrange
            var package = CreateTestData();
            var validator = new PackageValidator();

            // Act
            validator.CreateValidNames(package);

            // Assert
            package.Files.First(f => f.Id == "F00000001").PhysicalName.Should().Be("Und _ ein Datei_Name mit _ _ Sonderzeichen.pdf");
        }

        [Test]
        public void Invalid_chars_in_file_with_long_name_are_replaced_with_underscore()
        {
            // Arrange
            var package = CreateTestData();
            var validator = new PackageValidator();

            // Act
            validator.CreateValidNames(package);

            // Assert
            package.Files.First(f => f.Id == "F00000002").PhysicalName.Should().Be(
                "Und _ ein Datei_Name mit _ _ Sonderzeichen und deren Name dann noch überaus lang ist und viele Zeichen enthält bis er dann irgend wann einfach zu lang ist.pdf");
        }

        [Test]
        public void Make_sure_object_count_of_temp_object_is_correct()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();
            validator.CreateValidNames(package);

            // Act
            var list = validator.ConvertToRepositoryObject(package);

            // Assert
            var numFolders = 2 + 2 * 2 + 4 * 2 + 8 * 2 + 16 * 2;
            var numFiles = 2 + 2 * 2 + 4 * 2 + 8 * 2 + 16 * 2 + 32 * 2;

            list.Count.Should().Be(numFolders + numFiles);
            list.Count(f => f.Type == TempValidationObjectType.File).Should().Be(numFiles);
            list.Count(f => f.Type == TempValidationObjectType.Folder).Should().Be(numFolders);
            list.Max(l => l.HierachyLevel).Should().Be(MaxLevel + 1); // 5 Folder levels + 1 File Level
        }

        [Test]
        public void Make_sure_we_have_too_long_path_names_correct()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();
            validator.CreateValidNames(package);

            // Act
            var list = validator.ConvertToRepositoryObject(package);

            // Assert
            (list.Max(l => l.FullName.Length) > validator.MaxPathLength).Should().BeTrue();
        }

        [Test]
        public void Make_sure_too_long_path_names_are_shortened()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();

            // Act
            validator.EnsureValidPhysicalFileAndFolderNames(package, @"C:\Users\jlang\Downloads\Repository");
            var list = validator.ConvertToRepositoryObject(package);

            // Assert
            (list.Max(l => l.FullName.Length) > validator.MaxPathLength).Should().BeFalse();
        }

        [Test]
        public void Make_sure_a_long_root_path_shortens_max_path_length_below_200_chars()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();

            // Act
            validator.EnsureValidPhysicalFileAndFolderNames(package,
                @"C:\Users\jlang\Downloads\Repository\with a very long path name\that exceeds 60 chars\just for testing");
            var list = validator.ConvertToRepositoryObject(package);

            // Assert
            validator.MaxPathLength.Should().BeLessThan(200);
        }

        [Test]
        public void Make_sure_a_normal_root_path_shortens_max_path_length_to_exactly_200_chars()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();

            // Act
            validator.EnsureValidPhysicalFileAndFolderNames(package, @"C:\Users\jlang\Downloads\Repository\");
            var list = validator.ConvertToRepositoryObject(package);

            // Assert
            validator.MaxPathLength.Should().Be(200);
        }

        [Test]
        public void New_name_returns_empty_value_if_no_element_too_short()
        {
            // Arrange
            var validator = new PackageValidator();
            var packageItems = new List<TempValidationObject>
            {
                new TempValidationObject
                {
                    Path = @"Test\Test",
                    Name = "My Directory",
                    FullName = @"Test\Test\My Directory",
                    HierachyLevel = 3,
                    IdPath = @"id1\id2",
                    Type = TempValidationObjectType.Folder
                }
            };

            // Act
            var result = validator.GetNewShorterNameForLongestElement(packageItems);

            // Assert
            result.Key.Should().BeNullOrEmpty();
        }

        [Test]
        public void New_name_returns_shorter_name_for_folder()
        {
            // Arrange
            var validator = new PackageValidator();
            var longEntry =
                "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores";
            var packageItems = new List<TempValidationObject>
            {
                new TempValidationObject
                {
                    // Erster Eintrag hat 200 Zeichen
                    // Fullname sind 217
                    Path = $@"{longEntry}\Test",
                    Name = "My Directory",
                    FullName = $@"{longEntry}\Test\My Directory",
                    HierachyLevel = 3,
                    IdPath = @"id1\id2",
                    RepositoryId = "id3",
                    Type = TempValidationObjectType.Folder
                }
            };

            // Act
            var result = validator.GetNewShorterNameForLongestElement(packageItems);

            // Assert
            result.Key.Should().Be("id1");
            // Ester Eintrag um 17 Zeichen gekürzt (und getrimmt)
            result.Value.Should().Be(longEntry.Substring(0, longEntry.Length - 17).Trim());
        }

        [Test]
        public void New_name_returns_shorter_name_for_folder_on_second_level()
        {
            // Arrange
            var validator = new PackageValidator();
            // 300 Zeichen
            var longEntry =
                "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lore";
            var packageItems = new List<TempValidationObject>
            {
                new TempValidationObject
                {
                    // Fullname sind 331
                    Path = $@"Test1\{longEntry}\Test2\Test3",
                    Name = "My Directory",
                    FullName = $@"Test1\{longEntry}\Test2\Test3\My Directory",
                    HierachyLevel = 5, // gives an average lenth of 40 allowed chars per level
                    IdPath = @"id1\id2\id3\id4",
                    RepositoryId = "id5",
                    Type = TempValidationObjectType.Folder
                }
            };

            // Act
            var result = validator.GetNewShorterNameForLongestElement(packageItems);

            // Assert
            result.Key.Should().Be("id2");
            // zweiter Eintrag um 131 Zeichen gekürzt
            result.Value.Should().Be(longEntry.Substring(0, longEntry.Length - 131));
        }

        [Test]
        public void New_name_returns_shorter_name_for_folder_with_more_than_average_overflow()
        {
            // Arrange
            var validator = new PackageValidator();
            // 40 Zeichen
            var longEntry =
                "Lorem ipsum dolor sit amet, consetetur s";
            var packageItems = new List<TempValidationObject>
            {
                new TempValidationObject
                {
                    // Fullname sind 331
                    Path = $@"{longEntry}1\{longEntry}2\{longEntry}3\{longEntry}4",
                    Name = $"{longEntry}5",
                    FullName = $@"{longEntry}1\{longEntry}2\{longEntry}3\{longEntry}4\{longEntry}5",
                    HierachyLevel = 5, // gives an average lenth of 40 allowed chars per level
                    IdPath = @"id1\id2\id3\id4",
                    RepositoryId = "id5",
                    Type = TempValidationObjectType.Folder
                }
            };

            // Act
            var result = validator.GetNewShorterNameForLongestElement(packageItems);

            // Assert
            result.Key.Should().Be("id1"); // The first found entry
            // we just cut off one char from the longest entry, as we don't want to cut off
            // too much chars from one entry, if the distribution of length is similar
            result.Value.Should().Be(longEntry.Substring(0, longEntry.Length));
        }

        [Test]
        public void New_name_returns_shorter_name_for_file()
        {
            // Arrange
            var validator = new PackageValidator();
            // file name with 200 chars + 4 chars for extension
            var longEntry =
                "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores.pdf";
            var packageItems = new List<TempValidationObject>
            {
                new TempValidationObject
                {
                    // Fullname sind 208
                    Path = @"Test",
                    Name = longEntry,
                    FullName = $@"Test\{longEntry}",
                    HierachyLevel = 2,
                    IdPath = @"id1",
                    RepositoryId = "id2",
                    Type = TempValidationObjectType.File
                }
            };

            // Act
            var result = validator.GetNewShorterNameForLongestElement(packageItems);

            // Assert
            result.Key.Should().Be("id2");
            // Ester Eintrag um 8 + 4 Zeichen gekürzt
            result.Value.Should().Be(longEntry.Substring(0, longEntry.Length - (8 + 4)) + ".pdf");
            // aber extension immer noch vorhanden
            result.Value.Should().EndWith(".pdf");
        }

        [Test]
        public void New_name_returns_shorter_name_for_file_without_extension()
        {
            // Arrange
            var validator = new PackageValidator();
            // file name with 200 chars
            var longEntry =
                "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua, At vero eos et accusam et justo duo doloresi";
            var packageItems = new List<TempValidationObject>
            {
                new TempValidationObject
                {
                    // Fullname sind 205
                    Path = @"Test",
                    Name = longEntry,
                    FullName = $@"Test\{longEntry}",
                    HierachyLevel = 2,
                    IdPath = @"id1",
                    RepositoryId = "id2",
                    Type = TempValidationObjectType.File
                }
            };

            // Act
            var result = validator.GetNewShorterNameForLongestElement(packageItems);

            // Assert
            result.Key.Should().Be("id2");
            // Ester Eintrag um 5 gekürzt
            result.Value.Should().Be(longEntry.Substring(0, longEntry.Length - 5));
            // Make sure not dot in filename
            result.Value.Should().NotContain(".");
        }

        [Test]
        public void Update_package_folder_with_new_name()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();

            // Act
            validator.UpdatePackage(package, new KeyValuePair<string, string>("Dir00000010", "Happy"));


            // Assert
            var flatList = validator.ConvertToRepositoryObject(package);
            flatList.First(f => f.RepositoryId == "Dir00000010").Name.Should().Be("Happy");
        }


        [Test]
        public void Update_package_file_with_new_name()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();

            // Act
            validator.UpdatePackage(package, new KeyValuePair<string, string>("F00000119", "Happy"));


            // Assert
            var flatList = validator.ConvertToRepositoryObject(package);
            flatList.First(f => f.RepositoryId == "F00000119").Name.Should().Be("Happy");
        }


        [Test]
        public void Make_sure_trainling_dots_at_the_end_of_names_are_removed()
        {
            // Arrange
            var validator = new PackageValidator();
            // file 119 and Dir 5 have illegal endings in test file
            var package = CreateTestData();

            // Act
            validator.EnsureValidPhysicalFileAndFolderNames(package, @"C:\Users\jlang\Downloads\Repository");

            // Assert
            var flatList = validator.ConvertToRepositoryObject(package);
            flatList.First(f => f.RepositoryId == "F00000119").Name.Should().Be("TABLE BROWN.pdf");
            flatList.First(f => f.RepositoryId == "Dir00000005").Name.Should().Be("Zjing Oil");
        }

        [Test]
        public void Make_sure_duplicate_file_names_are_extended_with_suffix()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();
            // Pick a directory and add files with same name
            var dir = package.Folders.FirstOrDefault(f => f.Id == "Dir00000032");
            dir.Files.AddRange(new[]
            {
                new RepositoryFile {Id = "T1", LogicalName = "p1.pdf", PhysicalName = "p1.pdf"},
                new RepositoryFile {Id = "T2", LogicalName = "p1.pdf", PhysicalName = "p1.pdf"},
                new RepositoryFile {Id = "T3", LogicalName = "p1.pdf", PhysicalName = "p1.pdf"}
            });

            // Act
            validator.EnsureValidPhysicalFileAndFolderNames(package, @"C:\Users\jlang\Downloads\Repository");

            // Assert
            var flatList = validator.ConvertToRepositoryObject(package);
            flatList.First(f => f.RepositoryId == "T1").Name.Should().Be("p1.pdf");
            flatList.First(f => f.RepositoryId == "T2").Name.Should().Be("p1_1.pdf");
            flatList.First(f => f.RepositoryId == "T3").Name.Should().Be("p1_2.pdf");
        }

        [Test]
        public void Make_sure_duplicate_file_names_are_extended_with_suffix2()
        {
            // Arrange
            var validator = new PackageValidator();

            // Sample Package has one duplicate file "\PLUS_3\PLUS3_Bern_V1\P0.pdf"
            var packageTestData = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "packageSampleData.json"), Encoding.UTF8);
            var package = JsonConvert.DeserializeObject<RepositoryPackage>(packageTestData);

            // Act
            validator.EnsureValidPhysicalFileAndFolderNames(package, @"C:\Users\jlang\Downloads\Repository");

            // Assert
            var flatList = validator.ConvertToRepositoryObject(package);
            flatList.First(f => f.RepositoryId == "sdb:digitalFile|685fbf49-d67c-4f60-9842-96c35d009210").Name.Should().Be("P0_1.pdf");
        }

        [Test]
        public void Make_sure_duplicate_file_names_on_deep_nested_folders_with_illegal_chars_are_extended_with_suffix()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();

            // Get to the lowest level folder
            var dir = package.Folders.First();
            while (dir.Folders.Any())
            {
                dir = dir.Folders.First();
            }

            // Add duplicate files with illegal chars
            dir.Files.AddRange(new[]
            {
                new RepositoryFile {Id = "T1", LogicalName = "p??1.pdf", PhysicalName = "p??1.pdf"},
                new RepositoryFile {Id = "T2", LogicalName = "p??1.pdf", PhysicalName = "p??1.pdf"},
                new RepositoryFile {Id = "T3", LogicalName = "p??1.pdf", PhysicalName = "p??1.pdf"}
            });

            // Act
            validator.EnsureValidPhysicalFileAndFolderNames(package, @"C:\Users\jlang\Downloads\Repository");

            // Assert
            var flatList = validator.ConvertToRepositoryObject(package);
            flatList.First(f => f.RepositoryId == "T1").Name.Should().Be("p__1.pdf");
            flatList.First(f => f.RepositoryId == "T2").Name.Should().Be("p__1_1.pdf");
            flatList.First(f => f.RepositoryId == "T3").Name.Should().Be("p__1_2.pdf");
        }

        [Test]
        public void Make_sure_very_long_duplicate_file_names_on_deep_nested_folders_are_extended_with_suffix()
        {
            // Arrange
            var validator = new PackageValidator();
            var package = CreateTestData();

            // Get to the lowest level folder
            var dir = package.Folders.First();
            while (dir.Folders.Any())
            {
                dir = dir.Folders.First();
            }

            // Add duplicate files too long names
            dir.Files.AddRange(new[]
            {
                new RepositoryFile
                {
                    Id = "T1",
                    LogicalName =
                        "this is a very long name for the pdf file that might lead to problems when saving the content to disk and might be problematic when creating the package.pdf",
                    PhysicalName =
                        "this is a very long name for the pdf file that might lead to problems when saving the content to disk and might be problematic when creating the package.pdf"
                },
                new RepositoryFile
                {
                    Id = "T2",
                    LogicalName =
                        "this is a very long name for the pdf file that might lead to problems when saving the content to disk and might be problematic when creating the package.pdf",
                    PhysicalName =
                        "this is a very long name for the pdf file that might lead to problems when saving the content to disk and might be problematic when creating the package.pdf"
                },
                new RepositoryFile
                {
                    Id = "T3",
                    LogicalName =
                        "this is a very long name for the pdf file that might lead to problems when saving the content to disk and might be problematic when creating the package.pdf",
                    PhysicalName =
                        "this is a very long name for the pdf file that might lead to problems when saving the content to disk and might be problematic when creating the package.pdf"
                }
            });

            // Act
            validator.EnsureValidPhysicalFileAndFolderNames(package, @"C:\Users\jlang\Downloads\Repository");

            // Assert
            var flatList = validator.ConvertToRepositoryObject(package);
            flatList.First(f => f.RepositoryId == "T1").Name.Should().Be("this is a very long name for.pdf");
            flatList.First(f => f.RepositoryId == "T2").Name.Should().Be("this is a very long name for_1.pdf");
            flatList.First(f => f.RepositoryId == "T3").Name.Should().Be("this is a very long name for_2.pdf");
        }
    }
}