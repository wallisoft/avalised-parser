namespace Avalised.Services;

/// <summary>
/// A single piece of AVML content - like a word in a sentence
/// </summary>
public class AVMLToken
{
    public TokenType Type { get; set; }
    public string Value { get; set; }
    public int LineNumber { get; set; }
    public int IndentLevel { get; set; }
    
    public AVMLToken(TokenType type, string value, int lineNumber, int indent)
    {
        Type = type;
        Value = value;
        LineNumber = lineNumber;
        IndentLevel = indent;
    }
    
    public override string ToString() => $"[Line {LineNumber}] {Type}: {Value}";
}

/// <summary>
/// What kind of token is this?
/// </summary>
public enum TokenType
{
    ControlType,    // "MenuItem", "Separator"
    ControlName,    // "FileMenu", "FileSep1"
    PropertyName,   // "Header", "InputGesture"
    PropertyValue,  // "_File", "Ctrl+N"
    ListMarker,     // "-" for YAML lists
    Comment,        // # or //
    BlankLine       // Empty line (ignored but tracked)
}

/// <summary>
/// A node in the UI tree - represents one control with its properties and children
/// </summary>
public class AVMLNode
{
    public string ControlType { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    public List<AVMLNode> Children { get; set; }
    public int LineNumber { get; set; }
    public bool WasCorrected { get; set; }  // Track if we auto-fixed this
    public string CorrectionNote { get; set; }  // What we fixed
    
    public AVMLNode()
    {
        Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Children = new List<AVMLNode>();
    }
    
    public override string ToString() => 
        $"{ControlType}:{Name} ({Properties.Count} props, {Children.Count} children)";
}
