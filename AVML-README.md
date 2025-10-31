# AVML Parser - Avalised Markup Language

**Forgiving, intelligent YAML-like parser for Avalised UI definitions**

## What Is This?

Instead of writing SQL INSERT statements by hand, you write your UI in AVML (a forgiving YAML-ish format), and the parser:

1. ✅ **Tokenizes** - Reads your file, accepts tabs OR spaces
2. ✅ **Builds tree** - Creates the hierarchical structure
3. ✅ **Validates** - Checks against your database schema
4. ✅ **Auto-corrects** - Fixes common mistakes intelligently
5. ✅ **Imports** - Generates and executes SQL INSERT statements

## The Magic: Intelligence

The parser uses your **database schema** to be smart:

- **Case-insensitive**: `menuitem` → `MenuItem`, `header` → `Header`
- **Fuzzy matching**: `MenuIten` → `MenuItem` (typo correction)
- **Context-aware placement**: Properties at wrong indent? We fix it!
- **Tab/space friendly**: Mix tabs and spaces? No problem!
- **Helpful errors**: "Line 23: expected 'Header' property for MenuItem"

## Quick Start

### 1. Write AVML

```yaml
# File: menu.avml
MenuItem: FileMenu
  Header: _File
  
  Children:
    - MenuItem: FileNew
      Header: _New
      InputGesture: Ctrl+N
    
    - Separator: FileSep1
    
    - MenuItem: FileExit
      Header: E_xit
      InputGesture: Alt+F4
```

### 2. Parse and Import

```csharp
using Avalised.Services;

var parser = new AVMLParser("designer.db");
parser.ParseFileAndImport("menu.avml");
```

### 3. Run Your App

Your menu is now in the database and will load automatically!

## Command Line Usage

```bash
# Import menu
dotnet run AVMLParser menu.avml designer.db

# Dry run (show SQL but don't execute)
dotnet run AVMLParser menu.avml designer.db --dry-run

# Strict mode (no auto-corrections)
dotnet run AVMLParser menu.avml designer.db --strict
```

## Examples

### Example 1: Simple Menu

```yaml
MenuItem: FileMenu
  Header: _File
  
  Children:
    - MenuItem: FileNew
      Header: _New
    - MenuItem: FileOpen
      Header: _Open
```

### Example 2: Messy But Works!

```yaml
# This will auto-correct:
menuitem: FileMenu          # ✏️ Fixed to MenuItem
header: _File              # ✏️ Moved under FileMenu

  menuitem: FileNew        # ✏️ Fixed casing
    Header: _New
    inputgesture: Ctrl+N   # ✏️ Fixed to InputGesture
```

The parser says:
```
⚠ Line 2: 'menuitem' → 'MenuItem'
⚠ Line 3: 'header' moved under FileMenu (indent corrected)
⚠ Line 5: 'menuitem' → 'MenuItem'
⚠ Line 7: 'inputgesture' → 'InputGesture'

✓ Imported: 2 controls, 3 properties, 4 auto-corrections
```

## Architecture

```
AVML File
    ↓
AVMLTokenizer (forgiving)
    ↓
AVMLASTBuilder (builds tree)
    ↓
AVMLSchemaValidator (uses database to validate & correct)
    ↓
AVMLDatabaseImporter (generates SQL)
    ↓
Database
```

## Files Created

- **AVMLToken.cs** - Token and node definitions
- **AVMLTokenizer.cs** - Forgiving tokenizer
- **AVMLASTBuilder.cs** - Tree builder
- **AVMLSchemaValidator.cs** - Intelligent validator
- **AVMLDatabaseImporter.cs** - SQL generator
- **AVMLParser.cs** - Main orchestrator
- **menu.avml** - Sample menu structure
- **AVMLParserTest.cs** - Test program

## Validation Modes

```csharp
// Forgiving (default) - auto-fix with warnings
var parser = new AVMLParser(dbPath, ValidationMode.Forgiving);

// Strict - fail on any error
var parser = new AVMLParser(dbPath, ValidationMode.Strict);

// Interactive - ask user (future)
var parser = new AVMLParser(dbPath, ValidationMode.Interactive);
```

## What Gets Auto-Corrected?

✅ **Control type casing**: `menuitem` → `MenuItem`  
✅ **Property casing**: `header` → `Header`  
✅ **Wrong indentation**: Moves properties to correct parent  
✅ **Typos**: `MenuIten` → `MenuItem` (Levenshtein distance ≤ 2)  
✅ **Tab/space mixing**: Normalized automatically  
✅ **Missing names**: Auto-generates unique names  
✅ **Misplaced properties**: Context-aware repositioning  

## Testing

Run the test program:

```bash
cd Testing
dotnet run AVMLParserTest.cs
```

This will:
1. Import the complete Avalised menu
2. Test messy AVML with auto-corrections
3. Generate SQL files for inspection

## Future Features

- 🔄 **Round-trip**: Export database back to AVML
- 🎯 **Interactive mode**: Dialog for ambiguous corrections
- 📊 **Diff view**: Compare AVML changes before import
- 🔍 **Validation preview**: See what will be corrected
- 🎨 **Syntax highlighting**: VI/VS Code extensions

## Why AVML?

**Before** (SQL hell):
```sql
INSERT INTO ui_tree (id, parent_id, control_type, name, display_order, is_root)
VALUES (10, 1, 'MenuItem', 'FileMenu', 0, 0);
INSERT INTO ui_properties (ui_tree_id, property_name, property_value)
VALUES (10, 'Header', '_File');
INSERT INTO ui_tree (id, parent_id, control_type, name, display_order, is_root)
VALUES (11, 10, 'MenuItem', 'FileNew', 0, 0);
-- ... 50 more lines ...
```

**After** (AVML bliss):
```yaml
MenuItem: FileMenu
  Header: _File
  Children:
    - MenuItem: FileNew
      Header: _New
```

## The Vision

You're building Avalised WITH Avalised. The designer's menu is defined in AVML. As you add features to the designer, you use AVML to define them. **Dogfooding from day one!**

When users see your designer menu working beautifully and learn it was defined in AVML, they'll say: "I want that!"

---

**Built by:** Steve "recursion hurts my head" Wallis & Claude "set: paste" (Anthropic)

**Status:** Ready for testing! 🚀
