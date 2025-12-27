using System.Reflection;
using System.Text;

var schemaAssemblyPath = args.Length > 0 ? args[0] : "../Schema/bin/Debug/net10.0/Schema.dll";
var outputRoot = args.Length > 1 ? args[1] : "../../../libs/data_types";

if (!File.Exists(schemaAssemblyPath))
{
    Console.WriteLine("Building Schema project...");
    var buildResult = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "build ../Schema/Schema.csproj -c Debug",
        WorkingDirectory = Directory.GetCurrentDirectory(),
        RedirectStandardOutput = true,
        UseShellExecute = false
    });
    buildResult?.WaitForExit();
}

Console.WriteLine($"Loading assembly: {schemaAssemblyPath}");
var assembly = Assembly.LoadFrom(Path.GetFullPath(schemaAssemblyPath));

var entityTypes = assembly.GetTypes()
    .Where(t => t.Namespace?.StartsWith("Schema.Entities") == true && t.IsClass && !t.IsAbstract)
    .ToList();

var enumTypes = assembly.GetTypes()
    .Where(t => t.Namespace?.StartsWith("Schema.Entities") == true && t.IsEnum)
    .ToList();

Console.WriteLine($"Found {entityTypes.Count} entities and {enumTypes.Count} enums");

foreach (var entityType in entityTypes)
{
    var domain = entityType.Namespace!.Split('.').Last().ToLower();
    GenerateGo(entityType, domain, outputRoot);
    GenerateFSharp(entityType, domain, outputRoot);
    GenerateRust(entityType, domain, outputRoot);
    GeneratePython(entityType, domain, outputRoot);
    GenerateCSharp(entityType, domain, outputRoot);
}

foreach (var enumType in enumTypes)
{
    var domain = enumType.Namespace!.Split('.').Last().ToLower();
    GenerateGoEnum(enumType, domain, outputRoot);
    GenerateFSharpEnum(enumType, domain, outputRoot);
    GenerateRustEnum(enumType, domain, outputRoot);
    GeneratePythonEnum(enumType, domain, outputRoot);
    GenerateCSharpEnum(enumType, domain, outputRoot);
}

Console.WriteLine("Codegen complete!");

static void GenerateGo(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    sb.AppendLine($"package {domain}");
    sb.AppendLine();
    sb.AppendLine("import (");
    sb.AppendLine("\t\"time\"");
    sb.AppendLine(")");
    sb.AppendLine();
    sb.AppendLine($"type {type.Name} struct {{");

    foreach (var prop in type.GetProperties().Where(p => !IsNavigationProperty(p)))
    {
        var goType = CSharpToGo(prop.PropertyType);
        var jsonName = ToSnakeCase(prop.Name);
        var dbTag = $"`json:\"{jsonName}\" db:\"{jsonName}\"`";
        sb.AppendLine($"\t{prop.Name} {goType} {dbTag}");
    }

    sb.AppendLine("}");

    var dir = Path.Combine(outputRoot, "go", domain);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{ToSnakeCase(type.Name)}.go"), sb.ToString());
    Console.WriteLine($"  Generated Go: {domain}/{ToSnakeCase(type.Name)}.go");
}

static void GenerateFSharp(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    var moduleName = char.ToUpper(domain[0]) + domain[1..];
    sb.AppendLine($"namespace HellArch.Libs.DataTypes.{moduleName}");
    sb.AppendLine();
    sb.AppendLine("open System");
    sb.AppendLine();
    sb.AppendLine($"type {type.Name} = {{");

    var props = type.GetProperties().Where(p => !IsNavigationProperty(p)).ToList();
    foreach (var prop in props)
    {
        var fsType = CSharpToFSharp(prop.PropertyType);
        sb.AppendLine($"    {prop.Name}: {fsType}");
    }

    sb.AppendLine("}");

    var dir = Path.Combine(outputRoot, "fsharp", moduleName);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{type.Name}.fs"), sb.ToString());
    Console.WriteLine($"  Generated F#: {moduleName}/{type.Name}.fs");
}

static void GenerateRust(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    sb.AppendLine("use chrono::{DateTime, Utc};");
    sb.AppendLine("use serde::{Deserialize, Serialize};");
    sb.AppendLine("use sqlx::FromRow;");
    sb.AppendLine("use uuid::Uuid;");
    sb.AppendLine();
    sb.AppendLine("#[derive(Debug, Clone, Serialize, Deserialize, FromRow)]");
    sb.AppendLine($"pub struct {type.Name} {{");

    foreach (var prop in type.GetProperties().Where(p => !IsNavigationProperty(p)))
    {
        var rustType = CSharpToRust(prop.PropertyType);
        var snakeName = ToSnakeCase(prop.Name);
        sb.AppendLine($"    pub {snakeName}: {rustType},");
    }

    sb.AppendLine("}");

    var dir = Path.Combine(outputRoot, "rust", domain);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{ToSnakeCase(type.Name)}.rs"), sb.ToString());
    Console.WriteLine($"  Generated Rust: {domain}/{ToSnakeCase(type.Name)}.rs");
}

static void GeneratePython(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    sb.AppendLine("from dataclasses import dataclass");
    sb.AppendLine("from datetime import datetime");
    sb.AppendLine("from typing import Optional");
    sb.AppendLine("from uuid import UUID");
    sb.AppendLine();
    sb.AppendLine("@dataclass");
    sb.AppendLine($"class {type.Name}:");

    foreach (var prop in type.GetProperties().Where(p => !IsNavigationProperty(p)))
    {
        var pyType = CSharpToPython(prop.PropertyType);
        var snakeName = ToSnakeCase(prop.Name);
        sb.AppendLine($"    {snakeName}: {pyType}");
    }

    var dir = Path.Combine(outputRoot, "python", domain);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{ToSnakeCase(type.Name)}.py"), sb.ToString());
    Console.WriteLine($"  Generated Python: {domain}/{ToSnakeCase(type.Name)}.py");
}

static void GenerateCSharp(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    var ns = char.ToUpper(domain[0]) + domain[1..];
    sb.AppendLine($"namespace HellArch.Libs.DataTypes.{ns};");
    sb.AppendLine();
    sb.AppendLine($"public sealed record {type.Name}(");

    var props = type.GetProperties().Where(p => !IsNavigationProperty(p)).ToList();
    for (int i = 0; i < props.Count; i++)
    {
        var prop = props[i];
        var csType = GetCSharpTypeName(prop.PropertyType);
        var comma = i < props.Count - 1 ? "," : "";
        sb.AppendLine($"    {csType} {prop.Name}{comma}");
    }

    sb.AppendLine(");");

    var dir = Path.Combine(outputRoot, "csharp", ns);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{type.Name}.cs"), sb.ToString());
    Console.WriteLine($"  Generated C#: {ns}/{type.Name}.cs");
}

static void GenerateGoEnum(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    sb.AppendLine($"package {domain}");
    sb.AppendLine();
    sb.AppendLine($"type {type.Name} int");
    sb.AppendLine();
    sb.AppendLine("const (");

    var values = Enum.GetValues(type);
    for (int i = 0; i < values.Length; i++)
    {
        var val = values.GetValue(i)!;
        var iota = i == 0 ? $" {type.Name} = iota" : "";
        sb.AppendLine($"\t{type.Name}{val}{iota}");
    }

    sb.AppendLine(")");

    var dir = Path.Combine(outputRoot, "go", domain);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{ToSnakeCase(type.Name)}.go"), sb.ToString());
    Console.WriteLine($"  Generated Go enum: {domain}/{ToSnakeCase(type.Name)}.go");
}

static void GenerateFSharpEnum(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    var moduleName = char.ToUpper(domain[0]) + domain[1..];
    sb.AppendLine($"namespace HellArch.Libs.DataTypes.{moduleName}");
    sb.AppendLine();
    sb.AppendLine($"type {type.Name} =");

    foreach (var val in Enum.GetValues(type))
    {
        sb.AppendLine($"    | {val} = {(int)val}");
    }

    var dir = Path.Combine(outputRoot, "fsharp", moduleName);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{type.Name}.fs"), sb.ToString());
    Console.WriteLine($"  Generated F# enum: {moduleName}/{type.Name}.fs");
}

static void GenerateRustEnum(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    sb.AppendLine("use serde::{Deserialize, Serialize};");
    sb.AppendLine();
    sb.AppendLine("#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]");
    sb.AppendLine($"pub enum {type.Name} {{");

    foreach (var val in Enum.GetValues(type))
    {
        sb.AppendLine($"    {val} = {(int)val},");
    }

    sb.AppendLine("}");

    var dir = Path.Combine(outputRoot, "rust", domain);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{ToSnakeCase(type.Name)}.rs"), sb.ToString());
    Console.WriteLine($"  Generated Rust enum: {domain}/{ToSnakeCase(type.Name)}.rs");
}

static void GeneratePythonEnum(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    sb.AppendLine("from enum import IntEnum");
    sb.AppendLine();
    sb.AppendLine($"class {type.Name}(IntEnum):");

    foreach (var val in Enum.GetValues(type))
    {
        sb.AppendLine($"    {val.ToString()!.ToUpper()} = {(int)val}");
    }

    var dir = Path.Combine(outputRoot, "python", domain);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{ToSnakeCase(type.Name)}.py"), sb.ToString());
    Console.WriteLine($"  Generated Python enum: {domain}/{ToSnakeCase(type.Name)}.py");
}

static void GenerateCSharpEnum(Type type, string domain, string outputRoot)
{
    var sb = new StringBuilder();
    var ns = char.ToUpper(domain[0]) + domain[1..];
    sb.AppendLine($"namespace HellArch.Libs.DataTypes.{ns};");
    sb.AppendLine();
    sb.AppendLine($"public enum {type.Name}");
    sb.AppendLine("{");

    foreach (var val in Enum.GetValues(type))
    {
        sb.AppendLine($"    {val} = {(int)val},");
    }

    sb.AppendLine("}");

    var dir = Path.Combine(outputRoot, "csharp", ns);
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, $"{type.Name}.cs"), sb.ToString());
    Console.WriteLine($"  Generated C# enum: {ns}/{type.Name}.cs");
}

static bool IsNavigationProperty(PropertyInfo prop)
{
    var type = prop.PropertyType;
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
        return true;
    if (type.IsClass && type.Namespace?.StartsWith("Schema.Entities") == true)
        return true;
    return false;
}

static string ToSnakeCase(string name)
{
    var sb = new StringBuilder();
    for (int i = 0; i < name.Length; i++)
    {
        var c = name[i];
        if (char.IsUpper(c) && i > 0)
            sb.Append('_');
        sb.Append(char.ToLower(c));
    }
    return sb.ToString();
}

static string CSharpToGo(Type type)
{
    if (type == typeof(Guid)) return "string";
    if (type == typeof(string)) return "string";
    if (type == typeof(int)) return "int";
    if (type == typeof(long)) return "int64";
    if (type == typeof(bool)) return "bool";
    if (type == typeof(DateTime)) return "time.Time";
    if (Nullable.GetUnderlyingType(type) is { } underlying)
        return "*" + CSharpToGo(underlying);
    if (type.IsEnum) return type.Name;
    return "interface{}";
}

static string CSharpToFSharp(Type type)
{
    if (type == typeof(Guid)) return "Guid";
    if (type == typeof(string)) return "string";
    if (type == typeof(int)) return "int";
    if (type == typeof(long)) return "int64";
    if (type == typeof(bool)) return "bool";
    if (type == typeof(DateTime)) return "DateTime";
    if (Nullable.GetUnderlyingType(type) is { } underlying)
        return CSharpToFSharp(underlying) + " option";
    if (type.IsEnum) return type.Name;
    return "obj";
}

static string CSharpToRust(Type type)
{
    if (type == typeof(Guid)) return "Uuid";
    if (type == typeof(string)) return "String";
    if (type == typeof(int)) return "i32";
    if (type == typeof(long)) return "i64";
    if (type == typeof(bool)) return "bool";
    if (type == typeof(DateTime)) return "DateTime<Utc>";
    if (Nullable.GetUnderlyingType(type) is { } underlying)
        return "Option<" + CSharpToRust(underlying) + ">";
    if (type.IsEnum) return type.Name;
    return "()";
}

static string CSharpToPython(Type type)
{
    if (type == typeof(Guid)) return "UUID";
    if (type == typeof(string)) return "str";
    if (type == typeof(int)) return "int";
    if (type == typeof(long)) return "int";
    if (type == typeof(bool)) return "bool";
    if (type == typeof(DateTime)) return "datetime";
    if (Nullable.GetUnderlyingType(type) is { } underlying)
        return "Optional[" + CSharpToPython(underlying) + "]";
    if (type.IsEnum) return type.Name;
    return "object";
}

static string GetCSharpTypeName(Type type)
{
    if (type == typeof(Guid)) return "Guid";
    if (type == typeof(string)) return "string";
    if (type == typeof(int)) return "int";
    if (type == typeof(long)) return "long";
    if (type == typeof(bool)) return "bool";
    if (type == typeof(DateTime)) return "DateTime";
    if (Nullable.GetUnderlyingType(type) is { } underlying)
        return GetCSharpTypeName(underlying) + "?";
    if (type.IsEnum) return type.Name;
    return type.Name;
}
