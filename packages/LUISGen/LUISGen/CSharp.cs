using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace LUISGen
{
    class CSharp
    {
        static void Header(string description, string space, string className, Writer w)
        {
            w.WriteLine($@"// <auto-generated>
// Code generated by {description}
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace {space}
{{");
            w.Indent();

            // Main class
            w.IndentLine($"public class {className}: IRecognizerConvert");
            w.IndentLine("{");
            w.Indent();

            // Text
            w.IndentLine("public string Text;");
            w.IndentLine("public string AlteredText;");
        }

        static void Intents(dynamic intents, Writer w)
        {
            w.IndentLine("public enum Intent {");
            w.Indent();
            var firstIntent = true;
            foreach (dynamic intent in intents)
            {
                if (firstIntent)
                {
                    firstIntent = false;
                }
                else
                {
                    w.WriteLine(", ");
                }
                w.Indent(Utils.NormalizeName((string)intent.name));
            }
            w.WriteLine();
            w.Outdent();
            w.IndentLine("};"); // Intent enum
            w.IndentLine("public Dictionary<Intent, IntentScore> Intents;");
        }

        static string PropertyName(dynamic name, dynamic app)
        {
            return Utils.JsonPropertyName(name, app);
        }

        static void AddJSonProperty(dynamic name, dynamic app, Writer w)
        {
            /*
            if (Utils.IsPrebuilt(name, app) || name as string == "datetime")
            {
                w.IndentLine($"[JsonProperty(\"builtin_{name}\")]");
            }
            */
        }

        static void WriteEntity(dynamic entity, dynamic type, dynamic app, Writer w)
        {
            Utils.EntityApply((JObject)entity,
                (name) =>
                {
                    var realName = PropertyName(name, app);
                    AddJSonProperty(realName, app, w);
                    switch ((string)type)
                    {
                        case "age":
                            w.IndentLine($"public Age[] {realName};");
                            break;
                        case "dimension":
                            w.IndentLine($"public Dimension[] {realName};");
                            break;
                        case "money":
                            w.IndentLine($"public Money[] {realName};");
                            break;
                        case "temperature":
                            w.IndentLine($"public Temperature[] {realName};");
                            break;
                        case "number":
                        case "ordinal":
                        case "percentage":
                            w.IndentLine($"public double[] {realName};");
                            break;
                        case "datetimeV2":
                            w.IndentLine($"public DateTimeSpec[] {realName};");
                            break;
                        case "list":
                            w.IndentLine($"public string[][] {realName};");
                            break;
                        default:
                            w.IndentLine($"public string[] {realName};");
                            break;
                    }
                }
            );
        }

        static void WriteEntities(dynamic entities, dynamic app, string description, Writer w)
        {
            if (entities != null && entities.Count > 0)
            {
                w.WriteLine();
                w.IndentLine($"// {description}");
                foreach (var entity in entities)
                {
                    WriteEntity(entity, Utils.IsList(entity.name, app) ? "list" : entity.name, app, w);
                }
            }
        }

        static void Entities(dynamic app, Writer w)
        {
            // Entities
            w.WriteLine();
            w.IndentLine("public class _Entities");
            w.IndentLine("{");
            w.Indent();
            if (app.entities != null && app.entities.Count > 0)
            {
                w.IndentLine("// Simple entities");
                foreach (var entity in app.entities)
                {
                    WriteEntity(entity, entity.name, app, w);
                    if (entity.children != null)
                    {
                        // Hiearchical
                        foreach (var child in entity.children)
                        {
                            WriteEntity(Utils.Entity(child), child, app, w);
                        }
                    }
                }
            }

            WriteEntities(app.prebuiltEntities, app, "Built-in entities", w);
            WriteEntities(app.closedLists, app, "Lists", w);
            WriteEntities(app.regex_entities, app, "Regex entities", w);
            WriteEntities(app.patternAnyEntities, app, "Pattern.any", w);

            // Composites
            if (app.composites != null && app.composites.Count > 0)
            {
                w.WriteLine();
                w.IndentLine("// Composites");
                bool first = true;
                foreach (var composite in app.composites)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        w.WriteLine();
                    }
                    var name = Utils.NormalizeName(composite.name);
                    w.IndentLine($"public class _Instance{name}");
                    w.IndentLine("{");
                    w.Indent();
                    foreach (var child in composite.children)
                    {
                        var childName = PropertyName(child, app);
                        AddJSonProperty(childName, app, w);
                        w.IndentLine($"public InstanceData[] {childName};");
                    }
                    w.Outdent();
                    w.IndentLine("}");
                    w.IndentLine($"public class {name}Class");
                    w.IndentLine("{");
                    w.Indent();
                    foreach (var child in composite.children)
                    {
                        WriteEntity(Utils.Entity(child), Utils.IsList(child, app) ? "list" : child, app, w);
                    }
                    w.IndentLine("[JsonProperty(\"$instance\")]");
                    w.IndentLine($"public _Instance{name} _instance;");
                    w.Outdent();
                    w.IndentLine("}");
                    w.IndentLine($"public {name}Class[] {name};");
                }
            }

            // Instance
            w.WriteLine();
            w.IndentLine("// Instance");
            w.IndentLine("public class _Instance");
            w.IndentLine("{");
            w.Indent();
            Utils.WriteInstances((JObject)app, (name) =>
            {
                var realName = PropertyName(name, app);
                AddJSonProperty(realName, app, w);
                w.IndentLine($"public InstanceData[] {realName};");
            });
            w.Outdent();
            w.IndentLine("}");
            w.IndentLine("[JsonProperty(\"$instance\")]");
            w.IndentLine("public _Instance _instance;");

            w.Outdent();
            w.IndentLine("}"); // Entities
            w.IndentLine("public _Entities Entities;");
        }

        static void Converter(string className, Writer w)
        {
            w.WriteLine();
            w.IndentLine($"public void Convert(dynamic result)");
            w.IndentLine("{");
            w.Indent();
            w.IndentLine($"var app = JsonConvert.DeserializeObject<{className}>(JsonConvert.SerializeObject(result));");
            w.IndentLine($"Text = app.Text;");
            w.IndentLine("AlteredText = app.AlteredText;");
            w.IndentLine("Intents = app.Intents;");
            w.IndentLine("Entities = app.Entities;");
            w.IndentLine("Properties = app.Properties;");
            w.Outdent();
            w.IndentLine("}");
        }

        static void TopScoringIntent(Writer w)
        {
            w.WriteLine();
            w.IndentLine(@"public (Intent intent, double score) TopIntent()");
            w.IndentLine("{");
            w.Indent();
            w.IndentLine("Intent maxIntent = Intent.None;");
            w.IndentLine("var max = 0.0;");
            w.IndentLine("foreach (var entry in Intents)");
            w.IndentLine("{");
            w.Indent();
            w.IndentLine("if (entry.Value.Score > max)");
            w.IndentLine("{");
            w.Indent();
            w.IndentLine("maxIntent = entry.Key;");
            w.IndentLine("max = entry.Value.Score.Value;");
            w.Outdent();
            w.IndentLine("}");
            w.Outdent();
            w.IndentLine("}");
            w.IndentLine("return (maxIntent, max);");
            w.Outdent();
            w.IndentLine("}");
        }

        public static void Generate(string description, dynamic app, string className, string space, string outPath)
        {
            var outName = Path.Combine(outPath, $"{className}.cs");
            Console.WriteLine($"Generating file {outName} that contains class {space}.{className}.");
            var w = new Writer(outName);
            Header(description, space, className, w);
            Intents(app.intents, w);
            Entities(app, w);

            w.WriteLine();
            w.IndentLine("[JsonExtensionData(ReadData = true, WriteData = true)]");
            w.IndentLine("public IDictionary<string, object> Properties {get; set; }");

            Converter(className, w);
            TopScoringIntent(w);

            w.Outdent();
            w.IndentLine("}"); // Class

            w.Outdent();
            w.IndentLine("}"); // Namespace

            w.Close();
        }
    }
}
