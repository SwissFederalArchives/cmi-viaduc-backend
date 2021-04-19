using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CMI.Manager.Index.CodeConfiguration;
using CMI.Manager.Index.CodeCreation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;

namespace CMI.Manager.Index.CodeConfiguration
{
    public class CustomFieldsConfiguration
    {
        private readonly Assembly assembly;

        public List<FieldConfiguration> Fields { get; }

        public CustomFieldsConfiguration(string configurationFile)
        {
            if (System.IO.File.Exists(configurationFile))
            {
                var json = System.IO.File.ReadAllText(configurationFile);
                Fields = JsonConvert.DeserializeObject<List<FieldConfiguration>>(json);

                var code = GetClassCode();
                assembly = CreateAssemblyDefinition(code);

            }
            else
            {
                throw new FileNotFoundException($"could not find the configuration file {configurationFile}.");
            }
        }

        private string GetClassCode()
        {
            // Create a namespace: (namespace CodeGenerationSample)
            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("CMI.Contract.Common")).NormalizeWhitespace();

            // Add System using statement: (using System)
            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
            @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")));

            //  Create a class
            var classDeclaration = SyntaxFactory.ClassDeclaration("CustomFields");

            // Add the public modifier
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            // Inherit BaseClass
            classDeclaration = classDeclaration.AddBaseListTypes(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ElasticCustomBase")));

            foreach (var field in Fields.Where(f => f.IsDefaultField == false))
            {
                // Create the properties
                var fieldType = field.IsRepeatable ? $"List<{field.Type}>" : field.Type;
                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(fieldType), field.TargetField)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

                // Add the field, the property and method to the class.
                classDeclaration = classDeclaration.AddMembers(propertyDeclaration);
            }

            // Add the class to the namespace.
            @namespace = @namespace.AddMembers(classDeclaration);

            // Normalize and get code as string.
            var code = @namespace
                .NormalizeWhitespace()
                .ToFullString();

            return code;
        }

        private Assembly CreateAssemblyDefinition(string code)
        {
            var sourceLanguage = new CSharpLanguage();
            var syntaxTree = sourceLanguage.ParseText(code, SourceCodeKind.Regular);
            var compilation = sourceLanguage
                .CreateLibraryCompilation(assemblyName: "InMemoryAssembly", enableOptimisations: false)
                .AddSyntaxTrees(syntaxTree);

            var stream = new MemoryStream();
            var emitResult = compilation.Emit(stream);

            if (emitResult.Success)
            {
                stream.Seek(0, SeekOrigin.Begin);
                return  Assembly.Load(stream.ToArray());
            }

            throw new InvalidOperationException("Unable to create dynamic assembly");

        }

        public object CreateCustomFieldsInstance()
        {
            return assembly.CreateInstance("CMI.Contract.Common.CustomFields");
        }
    }
}
