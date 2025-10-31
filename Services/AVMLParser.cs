namespace Avalised.Services;

/// <summary>
/// Main AVML Parser - orchestrates tokenization, parsing, validation, and import
/// This is your one-stop shop for loading AVML files!
/// </summary>
public class AVMLParser
{
    private readonly string _databasePath;
    private readonly AVMLSchemaValidator.ValidationMode _mode;
    
    public List<string> Warnings { get; private set; } = new();
    public List<string> Corrections { get; private set; } = new();
    public string GeneratedSQL { get; private set; } = "";
    
    public AVMLParser(string databasePath, 
        AVMLSchemaValidator.ValidationMode mode = AVMLSchemaValidator.ValidationMode.Forgiving)
    {
        _databasePath = databasePath;
        _mode = mode;
    }
    
    /// <summary>
    /// Parse AVML text and import into database
    /// Returns true if successful
    /// </summary>
    public bool ParseAndImport(string avmlText, bool executeImport = true)
    {
        try
        {
            Warnings.Clear();
            Corrections.Clear();
            
            Console.WriteLine("🔍 Tokenizing AVML...");
            
            // Step 1: Tokenize
            var tokenizer = new AVMLTokenizer();
            var tokens = tokenizer.Tokenize(avmlText);
            Warnings.AddRange(tokenizer.Warnings);
            
            Console.WriteLine($"   Found {tokens.Count} tokens");
            
            // Step 2: Build AST
            Console.WriteLine("🌳 Building syntax tree...");
            var builder = new AVMLASTBuilder();
            var tree = builder.BuildTree(tokens);
            Warnings.AddRange(builder.Warnings);
            
            Console.WriteLine($"   Built {CountNodes(tree)} nodes");
            
            // Step 3: Validate and correct
            Console.WriteLine("✨ Validating against schema...");
            var validator = new AVMLSchemaValidator(_databasePath);
            tree = validator.Validate(tree, _mode);
            Warnings.AddRange(validator.Warnings);
            Corrections.AddRange(validator.Corrections);
            
            if (Corrections.Count > 0)
            {
                Console.WriteLine($"   Made {Corrections.Count} auto-corrections");
            }
            
            // Step 4: Import to database
            Console.WriteLine("💾 Generating SQL...");
            var importer = new AVMLDatabaseImporter(_databasePath);
            GeneratedSQL = importer.Import(tree, executeImport);
            
            if (executeImport)
            {
                Console.WriteLine("✅ Imported to database!");
            }
            else
            {
                Console.WriteLine("✅ SQL generated (not executed)");
            }
            
            // Print summary
            PrintSummary(tree);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Warnings.Add($"FATAL: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Parse AVML file and import
    /// </summary>
    public bool ParseFileAndImport(string filePath, bool executeImport = true)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"❌ File not found: {filePath}");
            return false;
        }
        
        Console.WriteLine($"📂 Reading {Path.GetFileName(filePath)}...");
        var avmlText = File.ReadAllText(filePath);
        
        return ParseAndImport(avmlText, executeImport);
    }
    
    /// <summary>
    /// Export database to AVML format
    /// (Future feature - round-trip capability!)
    /// </summary>
    public string ExportToAVML()
    {
        // TODO: Query database and generate AVML
        throw new NotImplementedException("Export coming soon!");
    }
    
    /// <summary>
    /// Print a nice summary
    /// </summary>
    private void PrintSummary(AVMLNode tree)
    {
        int nodeCount = CountNodes(tree) - 1; // Exclude root
        int propCount = CountProperties(tree);
        
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine($"📊 Summary:");
        Console.WriteLine($"   Controls: {nodeCount}");
        Console.WriteLine($"   Properties: {propCount}");
        Console.WriteLine($"   Warnings: {Warnings.Count}");
        Console.WriteLine($"   Auto-corrections: {Corrections.Count}");
        Console.WriteLine("═══════════════════════════════════════");
        
        if (Warnings.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("⚠️  Warnings:");
            foreach (var warning in Warnings.Take(5))  // Show first 5
            {
                Console.WriteLine($"   {warning}");
            }
            if (Warnings.Count > 5)
            {
                Console.WriteLine($"   ... and {Warnings.Count - 5} more");
            }
        }
        
        if (Corrections.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("✏️  Auto-corrections made:");
            foreach (var correction in Corrections.Take(5))
            {
                Console.WriteLine($"   {correction}");
            }
            if (Corrections.Count > 5)
            {
                Console.WriteLine($"   ... and {Corrections.Count - 5} more");
            }
        }
        
        Console.WriteLine();
    }
    
    private int CountNodes(AVMLNode node)
    {
        int count = 1;
        foreach (var child in node.Children)
        {
            count += CountNodes(child);
        }
        return count;
    }
    
    private int CountProperties(AVMLNode node)
    {
        int count = node.Properties.Count;
        foreach (var child in node.Children)
        {
            count += CountProperties(child);
        }
        return count;
    }
}
