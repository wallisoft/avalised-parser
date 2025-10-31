namespace Avalised.Services;

/// <summary>
/// Builds an Abstract Syntax Tree from AVML tokens
/// Creates the hierarchical structure but doesn't validate yet
/// </summary>
public class AVMLASTBuilder
{
    private List<AVMLToken> _tokens;
    private int _currentIndex;
    private List<string> _warnings = new();
    
    public List<string> Warnings => _warnings;
    
    /// <summary>
    /// Build tree from tokens
    /// </summary>
    public AVMLNode BuildTree(List<AVMLToken> tokens)
    {
        _tokens = tokens;
        _currentIndex = 0;
        
        // Create a root node to hold everything
        var root = new AVMLNode
        {
            ControlType = "Root",
            Name = "Root",
            LineNumber = 0
        };
        
        // Build children recursively
        root.Children = BuildChildren(0);
        
        return root;
    }
    
    /// <summary>
    /// Build all children at a given indent level
    /// </summary>
    private List<AVMLNode> BuildChildren(int parentIndent)
    {
        var children = new List<AVMLNode>();
        
        while (_currentIndex < _tokens.Count)
        {
            var token = _tokens[_currentIndex];
            
            // Skip comments and blank lines
            if (token.Type == TokenType.Comment || token.Type == TokenType.BlankLine)
            {
                _currentIndex++;
                continue;
            }
            
            // If we're back at or before parent level, we're done with this group
            if (token.IndentLevel <= parentIndent && children.Count > 0)
                break;
            
            // Skip list markers (we handle them implicitly)
            if (token.Type == TokenType.ListMarker)
            {
                _currentIndex++;
                continue;
            }
            
            // Is this a control definition?
            if (token.Type == TokenType.ControlType)
            {
                var node = BuildNode(token.IndentLevel);
                if (node != null)
                    children.Add(node);
            }
            // Or a property at wrong level?
            else if (token.Type == TokenType.PropertyName)
            {
                // Property without a parent control - might need to move up
                _warnings.Add($"Line {token.LineNumber}: Property '{token.Value}' without parent control");
                _currentIndex++;
            }
            else
            {
                _currentIndex++;
            }
        }
        
        return children;
    }
    
    /// <summary>
    /// Build a single node (control with properties and children)
    /// </summary>
    private AVMLNode BuildNode(int nodeIndent)
    {
        var token = _tokens[_currentIndex];
        
        if (token.Type != TokenType.ControlType)
            return null;
        
        var node = new AVMLNode
        {
            ControlType = FixCasing(token.Value),  // "menuitem" → "MenuItem"
            LineNumber = token.LineNumber
        };
        
        _currentIndex++;
        
        // Next token might be the name
        if (_currentIndex < _tokens.Count && 
            _tokens[_currentIndex].Type == TokenType.ControlName &&
            _tokens[_currentIndex].IndentLevel == nodeIndent)
        {
            node.Name = _tokens[_currentIndex].Value;
            _currentIndex++;
        }
        else
        {
            // Generate a name if none provided
            node.Name = $"{node.ControlType}{node.LineNumber}";
            _warnings.Add($"Line {node.LineNumber}: Auto-generated name '{node.Name}'");
        }
        
        // Collect properties and children at next indent level
        var expectedIndent = nodeIndent + 1;
        
        while (_currentIndex < _tokens.Count)
        {
            var next = _tokens[_currentIndex];
            
            // Skip comments/blanks
            if (next.Type == TokenType.Comment || next.Type == TokenType.BlankLine)
            {
                _currentIndex++;
                continue;
            }
            
            // Back to same or lower level? Done with this node
            if (next.IndentLevel < expectedIndent)
                break;
            
            // Skip list markers
            if (next.Type == TokenType.ListMarker)
            {
                _currentIndex++;
                continue;
            }
            
            // Property?
            if (next.Type == TokenType.PropertyName)
            {
                var propName = FixCasing(next.Value);
                _currentIndex++;
                
                // Get value
                string propValue = "";
                if (_currentIndex < _tokens.Count && 
                    _tokens[_currentIndex].Type == TokenType.PropertyValue)
                {
                    propValue = _tokens[_currentIndex].Value;
                    _currentIndex++;
                }
                
                // Special case: "Children:" starts a list
                if (propName.Equals("Children", StringComparison.OrdinalIgnoreCase))
                {
                    node.Children.AddRange(BuildChildren(next.IndentLevel));
                }
                else
                {
                    node.Properties[propName] = propValue;
                }
            }
            // Nested control (child)?
            else if (next.Type == TokenType.ControlType)
            {
                // Check indent - might be at wrong level
                if (next.IndentLevel > expectedIndent)
                {
                    _warnings.Add($"Line {next.LineNumber}: Over-indented, adjusting");
                }
                
                var child = BuildNode(next.IndentLevel);
                if (child != null)
                    node.Children.Add(child);
            }
            else
            {
                _currentIndex++;
            }
        }
        
        return node;
    }
    
    /// <summary>
    /// Fix common casing issues: "menuitem" → "MenuItem", "inputgesture" → "InputGesture"
    /// </summary>
    private string FixCasing(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        
        // Simple fix: capitalize first letter
        // TODO: Use database schema for proper casing later
        return char.ToUpper(value[0]) + value.Substring(1);
    }
}
