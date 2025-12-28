using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var sourceDir = args.Length > 0 ? args[0] : "../csharp";
var outputDir = args.Length > 1 ? args[1] : "..";

var csFiles = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories);
Console.WriteLine($"Found {csFiles.Length} C# files");

foreach (var file in csFiles)
{
    var code = File.ReadAllText(file);
    var tree = CSharpSyntaxTree.ParseText(code);
    var root = tree.GetRoot();

    var ns = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
    if (ns is null) continue;

    var namespaceName = ns.Name.ToString();
    var domain = namespaceName.Split('.').Last().ToLower();

    var enums = new List<EnumDef>();
    foreach (var enumDecl in ns.DescendantNodes().OfType<EnumDeclarationSyntax>())
    {
        var enumName = enumDecl.Identifier.Text;
        var values = enumDecl.Members.Select(m => m.Identifier.Text).ToList();
        enums.Add(new EnumDef(enumName, values));
    }

    foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
    {
        var typeDef = ParseClass(classDecl, namespaceName, enums);
        Console.WriteLine($"  {namespaceName}.{typeDef.Name}");

        GenerateGo(typeDef, domain, outputDir);
        GenerateRust(typeDef, domain, outputDir);
        GeneratePython(typeDef, domain, outputDir);
        GenerateFSharp(typeDef, domain, outputDir);
    }
}

Console.WriteLine("\nDone!");

TypeDef ParseClass(ClassDeclarationSyntax classDecl, string namespaceName, List<EnumDef> allEnums)
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

        var baseType = isNullable
            ? ((NullableTypeSyntax)typeSyntax).ElementType.ToString()
            : typeStr;

        properties.Add(new PropertyDef(propName, baseType, isNullable));
    }

    var usedTypes = properties.Select(p => p.Type).ToHashSet();
    var usedEnums = allEnums.Where(e => usedTypes.Contains(e.Name)).ToList();

    return new TypeDef(className, namespaceName, doc, properties, usedEnums);
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

string ToGoFieldName(string name) =>
    name.Replace("Id", "ID")
        .Replace("Url", "URL")
        .Replace("Ip", "IP")
        .Replace("Api", "API")
        .Replace("Uuid", "UUID");

void GenerateGo(TypeDef def, string domain, string outDir)
{
    var dir = Path.Combine(outDir, "go", domain);
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{ToSnakeCase(def.Name)}.go");

    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"package {domain}");
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
            sb.AppendLine($"\t{e.Name}{v} {e.Name} = \"{ToSnakeCase(v)}\"");
        sb.AppendLine(")");
        sb.AppendLine();
    }

    if (!string.IsNullOrEmpty(def.Doc))
        foreach (var line in SplitDocLines(def.Doc))
            sb.AppendLine($"// {line}");
    sb.AppendLine($"type {def.Name} struct {{");

    foreach (var p in def.Properties)
    {
        var goType = CSharpToGo(p.Type, p.IsNullable, def.Enums);
        var goField = ToGoFieldName(p.Name);
        var jsonTag = ToSnakeCase(p.Name);
        var omit = p.IsNullable ? ",omitempty" : "";
        sb.AppendLine($"\t{goField} {goType} `json:\"{jsonTag}{omit}\"`");
    }

    sb.AppendLine("}");
    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    go: ok");
}

string CSharpToGo(string type, bool nullable, List<EnumDef> enums)
{
    var goType = type switch
    {
        "string" => "string",
        "int" or "int32" => "int32",
        "long" or "int64" => "int64",
        "bool" => "bool",
        "DateTime" or "DateTimeOffset" => "time.Time",
        "Guid" => "string",
        _ when enums.Any(e => e.Name == type) => type,
        _ => type
    };

    return nullable ? $"*{goType}" : goType;
}

void GenerateRust(TypeDef def, string domain, string outDir)
{
    var dir = Path.Combine(outDir, "rust", "src", domain);
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{ToSnakeCase(def.Name)}.rs");

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("use chrono::{DateTime, Utc};");
    sb.AppendLine("use serde::Deserialize;");
    sb.AppendLine();

    foreach (var e in def.Enums)
    {
        sb.AppendLine("#[derive(Debug, Clone, Copy, PartialEq, Eq, Deserialize)]");
        sb.AppendLine($"#[serde(rename_all = \"snake_case\")]");
        sb.AppendLine($"pub enum {e.Name} {{");
        foreach (var v in e.Values)
            sb.AppendLine($"    {v},");
        sb.AppendLine("}");
        sb.AppendLine();
    }

    if (!string.IsNullOrEmpty(def.Doc))
        foreach (var line in SplitDocLines(def.Doc))
            sb.AppendLine($"/// {line}");
    sb.AppendLine("#[derive(Debug, Clone, Deserialize)]");
    sb.AppendLine($"pub struct {def.Name} {{");

    foreach (var p in def.Properties)
    {
        var rustType = CSharpToRust(p.Type, p.IsNullable, def.Enums);
        var fieldName = ToSnakeCase(p.Name);
        sb.AppendLine($"    pub {fieldName}: {rustType},");
    }

    sb.AppendLine("}");
    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    rust: ok");
}

string CSharpToRust(string type, bool nullable, List<EnumDef> enums)
{
    var rustType = type switch
    {
        "string" => "String",
        "int" or "int32" => "i32",
        "long" or "int64" => "i64",
        "bool" => "bool",
        "DateTime" or "DateTimeOffset" => "DateTime<Utc>",
        "Guid" => "String",
        _ when enums.Any(e => e.Name == type) => type,
        _ => type
    };

    return nullable ? $"Option<{rustType}>" : rustType;
}

void GeneratePython(TypeDef def, string domain, string outDir)
{
    var dir = Path.Combine(outDir, "python", domain);
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{ToSnakeCase(def.Name)}.py");

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("from datetime import datetime");
    sb.AppendLine("from enum import Enum");
    sb.AppendLine("from typing import Optional");
    sb.AppendLine();
    sb.AppendLine("from pydantic import BaseModel, ConfigDict");
    sb.AppendLine();

    foreach (var e in def.Enums)
    {
        sb.AppendLine($"class {e.Name}(str, Enum):");
        foreach (var v in e.Values)
            sb.AppendLine($"    {v.ToUpper()} = \"{ToSnakeCase(v)}\"");
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

    foreach (var p in def.Properties)
    {
        var pyType = CSharpToPython(p.Type, p.IsNullable, def.Enums);
        var fieldName = ToSnakeCase(p.Name);
        sb.AppendLine($"    {fieldName}: {pyType}");
    }

    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    python: ok");
}

string CSharpToPython(string type, bool nullable, List<EnumDef> enums)
{
    var pyType = type switch
    {
        "string" => "str",
        "int" or "int32" or "long" or "int64" => "int",
        "bool" => "bool",
        "DateTime" or "DateTimeOffset" => "datetime",
        "Guid" => "str",
        _ when enums.Any(e => e.Name == type) => type,
        _ => type
    };

    return nullable ? $"Optional[{pyType}]" : pyType;
}

void GenerateFSharp(TypeDef def, string domain, string outDir)
{
    var dir = Path.Combine(outDir, "fsharp", domain);
    Directory.CreateDirectory(dir);
    var file = Path.Combine(dir, $"{ToSnakeCase(def.Name)}.fs");

    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"namespace HellArch.CloudEvents.{char.ToUpper(domain[0]) + domain[1..]}");
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
        var fsType = CSharpToFSharp(p.Type, p.IsNullable, def.Enums);
        sb.AppendLine($"    {p.Name}: {fsType}");
    }

    sb.AppendLine("}");
    File.WriteAllText(file, sb.ToString());
    Console.WriteLine($"    fsharp: ok");
}

string CSharpToFSharp(string type, bool nullable, List<EnumDef> enums)
{
    var fsType = type switch
    {
        "string" => "string",
        "int" or "int32" => "int",
        "long" or "int64" => "int64",
        "bool" => "bool",
        "DateTime" or "DateTimeOffset" => "DateTimeOffset",
        "Guid" => "Guid",
        _ when enums.Any(e => e.Name == type) => type,
        _ => type
    };

    return nullable ? $"{fsType} option" : fsType;
}

record TypeDef(string Name, string Namespace, string Doc, List<PropertyDef> Properties, List<EnumDef> Enums);
record PropertyDef(string Name, string Type, bool IsNullable);
record EnumDef(string Name, List<string> Values);
