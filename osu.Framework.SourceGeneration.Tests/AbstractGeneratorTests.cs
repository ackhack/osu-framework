// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace osu.Framework.SourceGeneration.Tests
{
    public abstract class AbstractGeneratorTests
    {
        private const string resources_namespace = "osu.Framework.SourceGeneration.Tests.Resources";

        public async Task RunTest(string name)
        {
            string commonSourcesNamespace = $"{resources_namespace}.CommonSources";
            string commonGeneratedNamespace = $"{resources_namespace}.CommonGenerated";
            string sourcesNamespace = $"{resources_namespace}.{name}.Sources";
            string generatedNamespace = $"{resources_namespace}.{name}.Generated";

            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();

            var sourceFiles = new List<(string filename, string content)>();
            var generatedFiles = new List<(string filename, string content)>();

            foreach (string? file in resourceNames.Where(n => n.StartsWith(commonSourcesNamespace, StringComparison.Ordinal)))
                sourceFiles.Add((getFileNameFromResourceName(commonSourcesNamespace, file), readResourceStream(assembly, file)));

            foreach (string? file in resourceNames.Where(n => n.StartsWith(commonGeneratedNamespace, StringComparison.Ordinal)))
                generatedFiles.Add((getFileNameFromResourceName(commonGeneratedNamespace, file), readResourceStream(assembly, file)));

            foreach (string? file in resourceNames.Where(n => n.StartsWith(sourcesNamespace, StringComparison.Ordinal)))
                sourceFiles.Add((getFileNameFromResourceName(sourcesNamespace, file), readResourceStream(assembly, file)));

            foreach (string? file in resourceNames.Where(n => n.StartsWith(generatedNamespace, StringComparison.Ordinal)))
                generatedFiles.Add((getFileNameFromResourceName(generatedNamespace, file), readResourceStream(assembly, file)));

            removeNameIndices(sourceFiles);
            removeNameIndices(generatedFiles);

            await Verify(sourceFiles.ToArray(), generatedFiles.ToArray()).ConfigureAwait(false);
        }

        /// <summary>
        /// This method is kind of SILLY and is a HACK.
        /// It exists because the order in which sources are present affects the order in which the Roslyn test class compares files.
        /// To get around this, filenames can be prefixed with '[0|1|2|3|etc]' indices to file names to get them into the correct order, which will be removed by this method.
        /// </summary>
        private void removeNameIndices(IList<(string filename, string content)> sources)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                string name = sources[i].filename;

                if (!name.StartsWith('['))
                    continue;

                sources[i] = (name[(name.IndexOf(']') + 1)..], sources[i].content);
            }
        }

        private string getFileNameFromResourceName(string resourceNamespace, string resourceName)
        {
            string extension = Path.GetExtension(resourceName);

            resourceName = resourceName.Replace(resourceNamespace, string.Empty).Substring(1)
                                       .Replace(extension, string.Empty);

            // .txt files are converted to .cs.
            resourceName = extension == ".txt" ? $"{resourceName}.cs" : $"{resourceName}{extension}";

            return resourceName;
        }

        protected abstract Task Verify((string filename, string content)[] sources, (string filename, string content)[] generated);

        private string readResourceStream(Assembly asm, string resource)
        {
            using (var stream = asm.GetManifestResourceStream(resource)!)
            using (var sr = new StreamReader(stream))
                return sr.ReadToEnd().ReplaceLineEndings("\r\n");
        }
    }
}
