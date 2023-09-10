using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyEngine.SourceGenerator
{
    internal class SourceTemplate
    {
        private static readonly Assembly Assembly = typeof(SourceTemplate).Assembly;

        private readonly string _contents;
        private readonly IReadOnlyList<TemplatePart> _parts;

        private SourceTemplate(string contents, IReadOnlyList<TemplatePart> parts)
        {
            _contents = contents;
            _parts = parts;
        }

        public void SubstitutePart(string partName, string replacement)
        {
            var foundPart = _parts.FirstOrDefault(x => x.Name == partName);
            if (foundPart is null)
            {
                throw new InvalidOperationException($"Could not find template part with name {partName}. The following partNames exist: {string.Join(";", _parts.Select(x => x.Name))}");
            }

            foundPart.Replacement = replacement;
        }

        public string Build()
        {
            if (_parts.Count == 0)
            {
                return _contents;
            }

            if (_parts.Any(x => x.Replacement is null))
            {
                throw new InvalidOperationException($"Some template parts still have no replacements: {string.Join(", ", _parts.Where(x => x.Replacement is null).Select(x => x.Name))}");
            }

            var stringBuilder = new StringBuilder();
            var previousPartEnd = 0;

            var instances = _parts.SelectMany(x => x.Instances.Select(y => (x.Name, y.Position, y.EndPosition, x.Replacement, Indentation: y.IndentationAtPosition)))
                .OrderBy(x => x.Position)
                .ToArray();

            foreach (var (Name, Position, EndPosition, Replacement, Indentation) in instances)
            {
                stringBuilder.Append(_contents.Substring(previousPartEnd, Position - previousPartEnd));
                if (Replacement.Contains("\r\n"))
                {
                    var lines = Replacement.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    stringBuilder.Append(lines[0]);
                    foreach (var line in lines.Skip(1))
                    {
                        stringBuilder.Append("\r\n");
                        stringBuilder.Append(' ', Indentation - 1);
                        stringBuilder.Append(line);
                    }
                }
                else
                {
                    stringBuilder.Append(Replacement);
                }

                previousPartEnd = EndPosition + 1;
            }

            stringBuilder.Append(_contents.Substring(previousPartEnd, _contents.Length - previousPartEnd));

            return stringBuilder.ToString();
        }

        public static SourceTemplate Load(string templateContents)
        {
            var templateParts = new List<TemplatePart>();

            const string templateStart = "{template:";
            var templateIndex = templateContents.IndexOf(templateStart);
            while (templateIndex != -1)
            {
                var endTemplateIndex = templateContents.IndexOf('}', templateIndex);
                var templatePart = templateContents.Substring(templateIndex + templateStart.Length, (endTemplateIndex - templateIndex) - templateStart.Length);
                if (templatePart.Contains("\r\n"))
                {
                    throw new InvalidOperationException($"Template cannot be across multiple lines");
                }


                var previousNewLine = templateContents.LastIndexOf("\r\n", templateIndex, templateIndex - 1);
                int indentationLevel;
                if (previousNewLine == -1)
                {
                    // no previous new line
                    indentationLevel = templateIndex;
                }
                else
                {
                    indentationLevel = templateIndex - previousNewLine;
                }



                var foundTemplatePart = templateParts.FirstOrDefault(x => x.Name == templatePart);
                if (foundTemplatePart is null)
                {
                    foundTemplatePart = new TemplatePart { Name = templatePart };
                    templateParts.Add(foundTemplatePart);
                }
                foundTemplatePart.Instances.Add(new TemplatePartInstance { Position = templateIndex, EndPosition = endTemplateIndex, IndentationAtPosition = indentationLevel - 1 });

                templateIndex = templateContents.IndexOf(templateStart, endTemplateIndex);
            }

            return new SourceTemplate(templateContents, templateParts);

        }

        public static SourceTemplate LoadFromEmbeddedResource(string fileName)
        {
            var manifestResourceName = Assembly.GetManifestResourceNames()
                .First(x => x.EndsWith(fileName));

            var resourceStream = Assembly.GetManifestResourceStream(manifestResourceName);
            string contents;
            using (var reader = new StreamReader(resourceStream))
            {
                contents = reader.ReadToEnd();
            }

            return Load(contents);
        }
    


        private sealed class TemplatePart
        {
            public string Name { get; set; }

            public string Replacement { get; set; }

            public List<TemplatePartInstance> Instances { get; } = new List<TemplatePartInstance>();
        }

        private sealed class TemplatePartInstance
        {
            public int Position { get; set; } 
            public int EndPosition { get; set; }
            public int IndentationAtPosition { get; set; } 
        }
    }
}
