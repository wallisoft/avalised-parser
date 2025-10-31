using System;
using System.IO;
using System.Linq;
using Avalised.Services;

namespace Avalised;

class Program
{
    static void Main(string[] args)
    {
        // Demo mode
        if (args.Length == 0)
        {
            RunDemo();
            return;
        }

        if (args.Length < 2)
        {
            ShowUsage();
            return;
        }

        string avmlFile = args[0];
        string dbPath = args[1];
        bool dryRun = args.Contains("--dry-run");
        int? parentId = null;

        // Check for --parent parameter
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--parent" && int.TryParse(args[i + 1], out int pid))
            {
                parentId = pid;
                break;
            }
        }

        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║   AVML Parser - Avalised 1.0           ║");
        Console.WriteLine("║   Steve Wallis & Claude (Anthropic)    ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();

        if (!File.Exists(avmlFile))
        {
            Console.WriteLine($"❌ Error: File not found: {avmlFile}");
            return;
        }

        try
        {
            Console.WriteLine($"📂 Reading {Path.GetFileName(avmlFile)}...");
            string avmlContent = File.ReadAllText(avmlFile);

            Console.WriteLine("🔍 Tokenizing AVML...");
            var tokenizer = new AVMLTokenizer();
            var tokens = tokenizer.Tokenize(avmlContent);
            Console.WriteLine($"   Found {tokens.Count} tokens");

            Console.WriteLine("🌳 Building syntax tree...");
            var builder = new AVMLASTBuilder();
            var tree = builder.BuildTree(tokens);
            Console.WriteLine($"   Built {CountNodes(tree)} nodes");

            Console.WriteLine("✨ Validating against schema...");
            var validator = new AVMLSchemaValidator(dbPath);
            validator.Validate(tree);

            Console.WriteLine("💾 Generating SQL...");
            var importer = new AVMLDatabaseImporter(dbPath);
            
            string sql;
            if (parentId.HasValue)
            {
                sql = importer.ImportAsChildren(tree, parentId.Value, !dryRun);
                Console.WriteLine(dryRun 
                    ? $"✅ SQL generated (parent_id={parentId.Value}, not executed)" 
                    : $"✅ Imported as children of parent {parentId.Value}!");
            }
            else
            {
                sql = importer.Import(tree, !dryRun);
                Console.WriteLine(dryRun 
                    ? "✅ SQL generated (not executed)" 
                    : "✅ Imported to database!");
            }

            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine("📊 Summary:");
            Console.WriteLine($"   Controls: {CountNodes(tree) - 1}");
            Console.WriteLine($"   Properties: {CountProperties(tree)}");
            Console.WriteLine("═══════════════════════════════════════");

            if (dryRun)
            {
                Console.WriteLine("📄 Generated SQL:");
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine(sql);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    static void RunDemo()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("    AVML Parser Demo");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("To parse AVML files:");
        Console.WriteLine("  dotnet run menu.avml designer.db");
        Console.WriteLine();
        Console.WriteLine("To import as children of MainMenu (id=3):");
        Console.WriteLine("  dotnet run menu.avml designer.db --parent 3");
    }

    static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run <file.avml> <db> --parent <id>");
        Console.WriteLine("  dotnet run <file.avml> <db> --dry-run");
    }

    static int CountNodes(AVMLNode node)
    {
        int count = 1;
        foreach (var child in node.Children)
            count += CountNodes(child);
        return count;
    }

    static int CountProperties(AVMLNode node)
    {
        int count = node.Properties.Count;
        foreach (var child in node.Children)
            count += CountProperties(child);
        return count;
    }
}
