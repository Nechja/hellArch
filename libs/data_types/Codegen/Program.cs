using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var sourceDir = args.Length > 0 ? args[0] : "../csharp";
var outputDir = args.Length > 1 ? args[1] : "..";

var csFiles = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories);
Console.WriteLine($"Found {csFiles.Length} C# files");

// First pass: collect all type definitions to build type->domain map
var typeMap = new Dictionary<string, string>(); // typeName -> domain
var typeDefs = new List<(TypeDef Def, string Domain)>();

foreach (var file in csFiles)
{
    var code = File.ReadAllText(file);
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();

    var ns = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
    if (ns is null) continue;

    var namespaceName = ns.Name.ToString();
    var domain = namespaceName.Split('.').Last();

    foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
    {
        var typeDef = ParseClass(classDecl, namespaceName);
        typeMap[typeDef.Name] = domain;
        typeDefs.Add((typeDef, domain));
    }

    foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>()
        .Where(e => e.Parent is BaseNamespaceDeclarationSyntax))
    {
        // Standalone enums are handled separately if needed
    }
}

// Second pass: generate files with proper imports
foreach (var (typeDef, domain) in typeDefs)
{
    Console.WriteLine($"  {typeDef.Namespace}.{typeDef.Name}");

    GenerateGo(typeDef, domain, outputDir);
    GenerateRust(typeDef, domain, outputDir);
    GeneratePython(typeDef, domain, outputDir);
    GenerateFSharp(typeDef, domain, outputDir);
    GenerateTypeScript(typeDef, domain, outputDir, typeMap);
}

Console.WriteLine("\nDone!");

TypeDef ParseClass(ClassDeclarationSyntax classDecl, string namespaceName)
{
    var className = classDecl.Identifier.Text;
    var doc = GetDocComment(classDecl);

    var properties = new List<PropertyDef>();
    foreach (var prop in classDecl.Members.OfType<PropertyDeclarationSyntax>())
    {
        var propName = prop.Identifier.Text;
        var typeSyntax = prop.Type;
        var typeStr = typeSyntax.ToString();

        var isNullable = typeSyntax is NullableTypeSyntax;
        var isRequired = prop.Modifiers.Any(m => m.IsKind(SyntaxKind.RequiredKeyword));

        var baseType = isNullable
            ? ((NullableTypeSyntax)typeSyntax).ElementType.ToString()
            : typeStr;

        string? defaultValue = null;
        if (prop.Initializer is not null)
        {
            defaultValue = prop.Initializer.Value.ToString();
        }

        properties.Add(new PropertyDef(propName, baseType, isNullable, isRequired, defaultValue));
    }

    var enums = new List<EnumDef>();
    foreach (var enumDecl in classDecl.Parent!.DescendantNodes().OfType<EnumDeclarationSyntax>())
    {
        var enumName = enumDecl.Identifier.Text;
        var values = enumDecl.Members.Select(m => m.Identifier.Text).ToList();
        enums.Add(new EnumDef(enumName, values));
    }

    return new TypeDef(className, namespaceName, doc, properties, enums);
}

string GetDocComment(SyntaxNode node)
{
    var trivia = node.GetLeadingTrivia()
        .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
        .FirstOrDefault();

    if (trivia == default) return "";

    var xml = trivia.ToString();
    var summaryMatch = System.Text.RegularExpressions.Regex.Match(
        xml, @"<summary>\s*(.*?)\s*</summary>", System.Text.RegularExpressions.RegexOptions.Singleline);

    if (!summaryMatch.Success) return "";

    return System.Text.RegularExpressions.Regex.Replace(
        summaryMatch.Groups[1].Value, @"///\s*", "").Trim();
}

string ToSnakeCase(string name) =>
    System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", "_$1").ToLower();

string[] SplitDocLines(string doc) =>
    doc.Split('\n', StringSplitOptions.RemoveEmptyEntries)
       .Select(line => line.Trim())
       .ToArray();

string ToGoFieldName(string name)
{
    // Go convention: acronyms like ID, URL, IP should be all caps
    return name
        .Replace("Id", "ID")
        .Replace("Url", "URL")
        .Replace("Ip", "IP")
        .Replace("Api", "API")
        .Replace("Uuid", "UUID")
        .Replace("Http", "HTTP")
        .Replace("Https", "HTTPS")
        .Replace("Sql", "SQL");
}

void GenerateGo(TypeDef def, string domain, string outDir)
{
    var dir = Path.Combine(outDir, "go", domain.ToLower());
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{ToSnakeCase(def.Name)}.go");

    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"package {domain.ToLower()}");
    sb.AppendLine();

    if (def.Properties.Any(p => p.Type is "DateTime" or "DateTimeOffset"))
    {
        sb.AppendLine("import \"time\"");
        sb.AppendLine();
    }

    foreach (var e in def.Enums)
    {
        sb.AppendLine($"type {e.Name} string");
        sb.AppendLine();
        sb.AppendLine("const (");
        foreach (var v in e.Values)
            sb.AppendLine($"\t{e.Name}{v} {e.Name} = \"{v.ToLower()}\"");
        sb.AppendLine(")");
        sb.AppendLine();
    }

    if (!string.IsNullOrEmpty(def.Doc))
        foreach (var line in SplitDocLines(def.Doc))
            sb.AppendLine($"// {line}");
    sb.AppendLine($"type {def.Name} struct {{");

    foreach (var p in def.Properties)
    {
        var goType = CSharpToGo(p.Type, p.IsNullable);
        var goField = ToGoFieldName(p.Name);
        var jsonTag = ToSnakeCase(p.Name);
        var omit = p.IsNullable ? ",omitempty" : "";
        sb.AppendLine($"\t{goField} {goType} `json:\"{jsonTag}{omit}\"`");
    }

    sb.AppendLine("}");
    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    go: ok");
}

string CSharpToGo(string type, bool nullable)
{
    if (type.StartsWith("ICollection<", StringComparison.Ordinal) ||
        type.StartsWith("List<", StringComparison.Ordinal) ||
        type.StartsWith("IEnumerable<", StringComparison.Ordinal))
    {
#pragma warning disable CA1865, CA1307
        var innerType = type.Substring(type.IndexOf('<') + 1).TrimEnd('>');
#pragma warning restore CA1865, CA1307
        var goInner = CSharpToGo(innerType, false);
        return $"[]*{goInner}";
    }

    var goType = type switch
    {
        "Guid" => "string",
        "string" => "string",
        "int" => "int",
        "long" => "int64",
        "bool" => "bool",
        "DateTime" or "DateTimeOffset" => "time.Time",
        _ => type
    };

    return nullable ? $"*{goType}" : goType;
}

void GenerateRust(TypeDef def, string domain, string outDir)
{
    var dir = Path.Combine(outDir, "rust", domain.ToLower());
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{ToSnakeCase(def.Name)}.rs");

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("use serde::{Deserialize, Serialize};");

    if (def.Properties.Any(p => p.Type is "DateTime" or "DateTimeOffset"))
        sb.AppendLine("use chrono::{DateTime, Utc};");

    sb.AppendLine();

    foreach (var e in def.Enums)
    {
        sb.AppendLine("#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]");
        sb.AppendLine("#[serde(rename_all = \"snake_case\")]");
        sb.AppendLine($"pub enum {e.Name} {{");
        foreach (var v in e.Values)
            sb.AppendLine($"    {v},");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    if (!string.IsNullOrEmpty(def.Doc))
        foreach (var line in SplitDocLines(def.Doc))
            sb.AppendLine($"/// {line}");
    sb.AppendLine("#[derive(Debug, Clone, Serialize, Deserialize)]");
    sb.AppendLine($"pub struct {def.Name} {{");

    foreach (var p in def.Properties)
    {
        var rustType = CSharpToRust(p.Type, p.IsNullable);
        var fieldName = ToSnakeCase(p.Name);
        sb.AppendLine($"    #[serde(rename = \"{fieldName}\")]");
        sb.AppendLine($"    pub {fieldName}: {rustType},");
    }

    sb.AppendLine("}");
    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    rust: ok");
}

string CSharpToRust(string type, bool nullable)
{
    if (type.StartsWith("ICollection<", StringComparison.Ordinal) ||
        type.StartsWith("List<", StringComparison.Ordinal) ||
        type.StartsWith("IEnumerable<", StringComparison.Ordinal))
    {
#pragma warning disable CA1865, CA1307
        var innerType = type.Substring(type.IndexOf('<') + 1).TrimEnd('>');
#pragma warning restore CA1865, CA1307
        var rustInner = CSharpToRust(innerType, false);
        return $"Vec<{rustInner}>";
    }

    var rustType = type switch
    {
        "Guid" or "string" => "String",
        "int" => "i32",
        "long" => "i64",
        "bool" => "bool",
        "DateTime" or "DateTimeOffset" => "DateTime<Utc>",
        _ => type
    };

    return nullable ? $"Option<{rustType}>" : rustType;
}

void GeneratePython(TypeDef def, string domain, string outDir)
{
    var dir = Path.Combine(outDir, "python", domain.ToLower());
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{ToSnakeCase(def.Name)}.py");

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("from __future__ import annotations");
    sb.AppendLine();

    if (def.Properties.Any(p => p.Type is "DateTime" or "DateTimeOffset"))
        sb.AppendLine("from datetime import datetime");

    if (def.Enums.Count > 0)
        sb.AppendLine("from enum import Enum");

    sb.AppendLine();
    sb.AppendLine("from pydantic import BaseModel, ConfigDict");
    sb.AppendLine();

    foreach (var e in def.Enums)
    {
        sb.AppendLine($"class {e.Name}(str, Enum):");
        foreach (var v in e.Values)
            sb.AppendLine($"    {v.ToUpper()} = \"{v.ToLower()}\"");
        sb.AppendLine();
    }

    sb.AppendLine($"class {def.Name}(BaseModel):");
    if (!string.IsNullOrEmpty(def.Doc))
    {
        var docLines = SplitDocLines(def.Doc);
        if (docLines.Length == 1)
            sb.AppendLine($"    \"\"\"{docLines[0]}\"\"\"");
        else
        {
            sb.AppendLine($"    \"\"\"{docLines[0]}");
            foreach (var line in docLines.Skip(1))
                sb.AppendLine($"    {line}");
            sb.AppendLine("    \"\"\"");
        }
    }
    sb.AppendLine("    model_config = ConfigDict(populate_by_name=True)");
    sb.AppendLine();

    var required = def.Properties.Where(p => !p.IsNullable && p.DefaultValue is null).ToList();
    var optional = def.Properties.Where(p => p.IsNullable || p.DefaultValue is not null).ToList();

    foreach (var p in required)
    {
        var pyType = CSharpToPython(p.Type, false);
        sb.AppendLine($"    {ToSnakeCase(p.Name)}: {pyType}");
    }

    foreach (var p in optional)
    {
        var pyType = CSharpToPython(p.Type, p.IsNullable);
        var defaultVal = p.IsNullable ? " = None" : $" = {CSharpToPythonDefault(p.DefaultValue!)}";
        sb.AppendLine($"    {ToSnakeCase(p.Name)}: {pyType}{defaultVal}");
    }

    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    python: ok");
}

string CSharpToPython(string type, bool nullable)
{
    if (type.StartsWith("ICollection<", StringComparison.Ordinal) ||
        type.StartsWith("List<", StringComparison.Ordinal) ||
        type.StartsWith("IEnumerable<", StringComparison.Ordinal))
    {
#pragma warning disable CA1865, CA1307
        var innerType = type.Substring(type.IndexOf('<') + 1).TrimEnd('>');
#pragma warning restore CA1865, CA1307
        var pyInner = CSharpToPython(innerType, false);
        return $"list[{pyInner}]";
    }

    var pyType = type switch
    {
        "Guid" or "string" => "str",
        "int" or "long" => "int",
        "bool" => "bool",
        "DateTime" or "DateTimeOffset" => "datetime",
        _ => type
    };

    return nullable ? $"{pyType} | None" : pyType;
}

string CSharpToPythonDefault(string value)
{
    if (value == "true") return "True";
    if (value == "false") return "False";

    if (value.Contains('.'))
    {
        var parts = value.Split('.');
        if (parts.Length == 2)
            return $"{parts[0]}.{parts[1].ToUpper()}";
    }

    return value;
}

void GenerateFSharp(TypeDef def, string domain, string outDir)
{
    var dir = Path.Combine(outDir, "fsharp", domain);
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{def.Name}.fs");

    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"namespace HellArch.DataTypes.{domain}");
    sb.AppendLine();
    sb.AppendLine("open System");
    sb.AppendLine();

    foreach (var e in def.Enums)
    {
        sb.AppendLine($"type {e.Name} =");
        foreach (var v in e.Values)
            sb.AppendLine($"    | {v}");
        sb.AppendLine();
    }

    if (!string.IsNullOrEmpty(def.Doc))
        foreach (var line in SplitDocLines(def.Doc))
            sb.AppendLine($"/// {line}");
    sb.AppendLine($"type {def.Name} = {{");

    foreach (var p in def.Properties)
    {
        var fsType = CSharpToFSharp(p.Type, p.IsNullable);
        sb.AppendLine($"    {p.Name}: {fsType}");
    }

    sb.AppendLine("}");
    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    fsharp: ok");
}

string CSharpToFSharp(string type, bool nullable)
{
    // Handle collection types: ICollection<T>, List<T>, IEnumerable<T> -> T list
    if (type.StartsWith("ICollection<", StringComparison.Ordinal) ||
        type.StartsWith("List<", StringComparison.Ordinal) ||
        type.StartsWith("IEnumerable<", StringComparison.Ordinal))
    {
#pragma warning disable CA1865, CA1307
        var innerType = type.Substring(type.IndexOf('<') + 1).TrimEnd('>');
#pragma warning restore CA1865, CA1307
        var fsInner = CSharpToFSharp(innerType, false);
        return $"{fsInner} list";
    }

    var fsType = type switch
    {
        "Guid" => "Guid",
        "string" => "string",
        "int" => "int",
        "long" => "int64",
        "bool" => "bool",
        "DateTime" => "DateTime",
        "DateTimeOffset" => "DateTimeOffset",
        _ => type
    };

    return nullable ? $"{fsType} option" : fsType;
}

void GenerateTypeScript(TypeDef def, string domain, string outDir, Dictionary<string, string> typeMap)
{
    var dir = Path.Combine(outDir, "typescript", domain.ToLower());
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{ToSnakeCase(def.Name)}.ts");

    var sb = new System.Text.StringBuilder();

    // Collect imports needed for referenced types
    var imports = new Dictionary<string, HashSet<string>>(); // domain -> types
    foreach (var p in def.Properties)
    {
        var referencedTypes = ExtractReferencedTypes(p.Type);
        foreach (var refType in referencedTypes)
        {
            if (typeMap.TryGetValue(refType, out var refDomain) && refType != def.Name)
            {
                if (!imports.ContainsKey(refDomain))
                    imports[refDomain] = [];
                imports[refDomain].Add(refType);
            }
        }
    }

    // Generate import statements
    foreach (var (importDomain, types) in imports.OrderBy(kv => kv.Key))
    {
        var typeList = string.Join(", ", types.OrderBy(t => t));
        var relativePath = importDomain.Equals(domain, StringComparison.OrdinalIgnoreCase)
            ? $"./{ToSnakeCase(types.First())}"
            : $"../{importDomain.ToLower()}/{ToSnakeCase(types.First())}";

        // For multiple types from same domain, import each separately
        foreach (var type in types.OrderBy(t => t))
        {
            var path = importDomain.Equals(domain, StringComparison.OrdinalIgnoreCase)
                ? $"./{ToSnakeCase(type)}"
                : $"../{importDomain.ToLower()}/{ToSnakeCase(type)}";
            sb.AppendLine($"import type {{ {type} }} from '{path}';");
        }
    }

    if (imports.Count > 0)
        sb.AppendLine();

    foreach (var e in def.Enums)
    {
        sb.AppendLine($"export type {e.Name} = {string.Join(" | ", e.Values.Select(v => $"'{ToSnakeCase(v)}'"))};");
        sb.AppendLine();
    }

    if (!string.IsNullOrEmpty(def.Doc))
        sb.AppendLine($"/** {def.Doc} */");
    sb.AppendLine($"export interface {def.Name} {{");

    foreach (var p in def.Properties)
    {
        var tsType = CSharpToTypeScript(p.Type, p.IsNullable);
        var fieldName = ToSnakeCase(p.Name);
        var optional = p.IsNullable ? "?" : "";
        sb.AppendLine($"  {fieldName}{optional}: {tsType};");
    }

    sb.AppendLine("}");
    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    typescript: ok");
}

List<string> ExtractReferencedTypes(string type)
{
    var result = new List<string>();

    // Handle collection types
    if (type.StartsWith("ICollection<", StringComparison.Ordinal) ||
        type.StartsWith("List<", StringComparison.Ordinal) ||
        type.StartsWith("IEnumerable<", StringComparison.Ordinal))
    {
#pragma warning disable CA1865, CA1307
        var innerType = type.Substring(type.IndexOf('<') + 1).TrimEnd('>');
#pragma warning restore CA1865, CA1307
        result.AddRange(ExtractReferencedTypes(innerType));
    }
    else if (!IsPrimitiveType(type))
    {
        result.Add(type);
    }

    return result;
}

bool IsPrimitiveType(string type) =>
    type is "Guid" or "string" or "int" or "long" or "bool" or "DateTime" or "DateTimeOffset";

string CSharpToTypeScript(string type, bool nullable)
{
    if (type.StartsWith("ICollection<", StringComparison.Ordinal) ||
        type.StartsWith("List<", StringComparison.Ordinal) ||
        type.StartsWith("IEnumerable<", StringComparison.Ordinal))
    {
#pragma warning disable CA1865, CA1307
        var innerType = type.Substring(type.IndexOf('<') + 1).TrimEnd('>');
#pragma warning restore CA1865, CA1307
        var tsInner = CSharpToTypeScript(innerType, false);
        return $"{tsInner}[]";
    }

    var tsType = type switch
    {
        "Guid" or "string" => "string",
        "int" or "long" => "number",
        "bool" => "boolean",
        "DateTime" or "DateTimeOffset" => "string",
        _ => type
    };

    return nullable ? $"{tsType} | null" : tsType;
}

record PropertyDef(string Name, string Type, bool IsNullable, bool IsRequired, string? DefaultValue);
record EnumDef(string Name, List<string> Values);
record TypeDef(string Name, string Namespace, string Doc, List<PropertyDef> Properties, List<EnumDef> Enums);
