using Microsoft.Data.Sqlite;
using System.Linq;

namespace Avalised.Services;

/// <summary>
/// Validates and auto-corrects AVML tree using database schema
/// This is where the magic happens!
/// </summary>
public class AVMLSchemaValidator
{
    private readonly string _dbPath;
    private List<string> _warnings = new();
    private List<string> _corrections = new();
    private Dictionary<string, ControlSchema> _schemaCache = new();
    
    public List<string> Warnings => _warnings;
    public List<string> Corrections => _corrections;
    
    public enum ValidationMode
    {
        Strict,      // Fail on any error
        Forgiving,   // Auto-fix with warnings (default)
        Interactive  // Ask user (future)
    }
    
    public AVMLSchemaValidator(string databasePath)
    {
        _dbPath = databasePath;
        LoadSchema();
    }
    
    /// <summary>
    /// Validate and correct the tree
    /// </summary>
    public AVMLNode Validate(AVMLNode root, ValidationMode mode = ValidationMode.Forgiving)
    {
        return ValidateNode(root, null, mode);
    }
    
    /// <summary>
    /// Validate and correct a single node recursively
    /// </summary>
    private AVMLNode ValidateNode(AVMLNode node, AVMLNode parent, ValidationMode mode)
    {
        // Skip root
        if (node.ControlType == "Root")
        {
            foreach (var child in node.Children)
                ValidateNode(child, node, mode);
            return node;
        }
        
        // 1. Validate control type exists
        var schema = GetControlSchema(node.ControlType);
        
        if (schema == null && mode == ValidationMode.Forgiving)
        {
            // Try fuzzy match
            var corrected = FuzzyMatchControlType(node.ControlType);
            if (corrected != null)
            {
                _corrections.Add($"Line {node.LineNumber}: '{node.ControlType}' → '{corrected}'");
                node.ControlType = corrected;
                node.WasCorrected = true;
                schema = GetControlSchema(corrected);
            }
        }
        
        if (schema == null)
        {
            _warnings.Add($"Line {node.LineNumber}: Unknown control type '{node.ControlType}'");
            return node;
        }
        
        // 2. Validate and correct properties
        ValidateProperties(node, schema, mode);
        
        // 3. Check if children are in the right place
        if (node.Children.Count > 0)
        {
            // Does this control support children?
            if (!schema.CanHaveChildren)
            {
                _warnings.Add($"Line {node.LineNumber}: {node.ControlType} cannot have children");
            }
            else
            {
                // Validate each child
                for (int i = 0; i < node.Children.Count; i++)
                {
                    node.Children[i] = ValidateNode(node.Children[i], node, mode);
                }
                
                // Check if children should be wrapped in a collection
                if (mode == ValidationMode.Forgiving)
                {
                    CheckChildrenStructure(node);
                }
            }
        }
        
        // 4. Look for misplaced properties (the context-aware bit!)
        if (mode == ValidationMode.Forgiving)
        {
            FixMisplacedProperties(node, parent);
        }
        
        return node;
    }
    
    /// <summary>
    /// Validate properties against schema
    /// </summary>
    private void ValidateProperties(AVMLNode node, ControlSchema schema, ValidationMode mode)
    {
        var correctedProps = new Dictionary<string, string>();
        
        foreach (var prop in node.Properties)
        {
            var validProp = schema.ValidProperties
                .FirstOrDefault(p => p.Equals(prop.Key, StringComparison.OrdinalIgnoreCase));
            
            if (validProp == null)
            {
                if (mode == ValidationMode.Forgiving)
                {
                    // Try to find a close match
                    validProp = FindClosestProperty(prop.Key, schema.ValidProperties);
                    
                    if (validProp != null)
                    {
                        _corrections.Add($"Line {node.LineNumber}: Property '{prop.Key}' → '{validProp}'");
                        correctedProps[validProp] = prop.Value;
                        node.WasCorrected = true;
                        continue;
                    }
                }
                
                _warnings.Add($"Line {node.LineNumber}: Unknown property '{prop.Key}' for {node.ControlType}");
            }
            else if (validProp != prop.Key)
            {
                // Casing fix
                correctedProps[validProp] = prop.Value;
                node.WasCorrected = true;
            }
        }
        
        // Apply corrections
        foreach (var correction in correctedProps)
        {
            node.Properties.Remove(correction.Key);
            node.Properties[correction.Key] = correction.Value;
        }
    }
    
    /// <summary>
    /// Check if children are properly structured
    /// Example: MenuItems should be in a Children collection
    /// </summary>
    private void CheckChildrenStructure(AVMLNode node)
    {
        // This is context-aware logic
        // For menus: If we have MenuItem children, they're probably meant to be in a list
        
        if (node.ControlType == "MenuItem" && node.Children.Count > 0)
        {
            // Menu items containing menu items - this is normal for submenus
            // Just validate they're all MenuItems or Separators
            foreach (var child in node.Children)
            {
                if (child.ControlType != "MenuItem" && child.ControlType != "Separator")
                {
                    _warnings.Add($"Line {child.LineNumber}: Unexpected {child.ControlType} in menu");
                }
            }
        }
    }
    
    /// <summary>
    /// Look for properties that should belong to parent or sibling
    /// This is the "put it where it should be" logic!
    /// </summary>
    private void FixMisplacedProperties(AVMLNode node, AVMLNode parent)
    {
        // Example: If a "Header" property is at same level as MenuItem instead of inside it
        // We already handle this in AST building, but this is a safety net
        
        // Check if any children look like they should be properties
        var childrenToRemove = new List<AVMLNode>();
        
        foreach (var child in node.Children)
        {
            // Does this child have no children and only one property?
            // Might be a misplaced property
            if (child.Children.Count == 0 && child.Properties.Count == 1)
            {
                var singleProp = child.Properties.First();
                var schema = GetControlSchema(node.ControlType);
                
                if (schema != null && schema.ValidProperties.Contains(singleProp.Key))
                {
                    // This child is actually a property!
                    _corrections.Add($"Line {child.LineNumber}: Moving '{singleProp.Key}' to parent properties");
                    node.Properties[singleProp.Key] = singleProp.Value;
                    node.WasCorrected = true;
                    childrenToRemove.Add(child);
                }
            }
        }
        
        // Remove misplaced properties
        foreach (var child in childrenToRemove)
        {
            node.Children.Remove(child);
        }
    }
    
    /// <summary>
    /// Load control schema from database
    /// </summary>
    private void LoadSchema()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();
        
        // Load all control types
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT name, can_have_children FROM control_types";
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(0);
            var canHaveChildren = reader.GetInt32(1) == 1;
            
            _schemaCache[name] = new ControlSchema
            {
                Name = name,
                CanHaveChildren = canHaveChildren,
                ValidProperties = LoadPropertiesForControl(connection, name)
            };
        }
    }
    
    /// <summary>
    /// Load valid properties for a control type
    /// </summary>
    private List<string> LoadPropertiesForControl(SqliteConnection connection, string controlType)
    {
        var properties = new List<string>();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT DISTINCT property_name 
            FROM control_properties 
            WHERE control_type = @type";
        cmd.Parameters.AddWithValue("@type", controlType);
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            properties.Add(reader.GetString(0));
        }
        
        return properties;
    }
    
    private ControlSchema GetControlSchema(string controlType)
    {
        _schemaCache.TryGetValue(controlType, out var schema);
        return schema;
    }
    
    /// <summary>
    /// Fuzzy match control type name (handle typos)
    /// </summary>
    private string FuzzyMatchControlType(string input)
    {
        var candidates = _schemaCache.Keys.ToList();
        
        // Simple Levenshtein distance check
        var closest = candidates
            .Select(c => new { Name = c, Distance = LevenshteinDistance(input, c) })
            .Where(x => x.Distance <= 2)  // Max 2 character difference
            .OrderBy(x => x.Distance)
            .FirstOrDefault();
        
        return closest?.Name;
    }
    
    /// <summary>
    /// Find closest matching property name
    /// </summary>
    private string FindClosestProperty(string input, List<string> validProperties)
    {
        return validProperties
            .Select(p => new { Name = p, Distance = LevenshteinDistance(input.ToLower(), p.ToLower()) })
            .Where(x => x.Distance <= 2)
            .OrderBy(x => x.Distance)
            .FirstOrDefault()?.Name;
    }
    
    /// <summary>
    /// Calculate edit distance between two strings
    /// </summary>
    private int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        
        if (n == 0) return m;
        if (m == 0) return n;
        
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        
        return d[n, m];
    }
}

/// <summary>
/// Schema definition for a control type
/// </summary>
public class ControlSchema
{
    public string Name { get; set; }
    public bool CanHaveChildren { get; set; }
    public List<string> ValidProperties { get; set; } = new();
}
