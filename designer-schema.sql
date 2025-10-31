-- Avalised Designer Database Schema - EXTENDED
-- Now supports full form controls, not just menus!

CREATE TABLE IF NOT EXISTS control_types (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    can_have_children INTEGER NOT NULL DEFAULT 0,
    description TEXT
);

CREATE TABLE IF NOT EXISTS control_properties (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    control_type TEXT NOT NULL,
    property_name TEXT NOT NULL,
    property_type TEXT,
    default_value TEXT,
    UNIQUE(control_type, property_name)
);

CREATE TABLE IF NOT EXISTS ui_tree (
    id INTEGER PRIMARY KEY,
    parent_id INTEGER,
    control_type TEXT NOT NULL,
    name TEXT NOT NULL,
    display_order INTEGER NOT NULL DEFAULT 0,
    is_root INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS ui_properties (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ui_tree_id INTEGER NOT NULL,
    property_name TEXT NOT NULL,
    property_value TEXT
);

-- ========== CONTROL TYPES ==========

-- Menu Controls
INSERT OR IGNORE INTO control_types (name, can_have_children, description) VALUES
('MenuItem', 1, 'Menu item with optional submenu'),
('Separator', 0, 'Menu separator'),
('Menu', 1, 'Top-level menu bar');

-- Layout Containers
INSERT OR IGNORE INTO control_types (name, can_have_children, description) VALUES
('Window', 1, 'Top-level window'),
('DockPanel', 1, 'Dock-based layout panel'),
('StackPanel', 1, 'Vertical or horizontal stack'),
('Panel', 1, 'Generic panel'),
('Canvas', 1, 'Absolute positioning canvas'),
('Grid', 1, 'Grid-based layout'),
('Border', 1, 'Border with single child'),
('ScrollViewer', 1, 'Scrollable container'),
('TabControl', 1, 'Tab container'),
('TabItem', 1, 'Single tab page');

-- Basic Controls
INSERT OR IGNORE INTO control_types (name, can_have_children, description) VALUES
('Button', 0, 'Clickable button'),
('TextBox', 0, 'Text input field'),
('TextBlock', 0, 'Read-only text display'),
('Label', 0, 'Text label'),
('CheckBox', 0, 'Checkbox control'),
('RadioButton', 0, 'Radio button control'),
('ComboBox', 0, 'Dropdown selection');

-- ========== COMMON PROPERTIES ==========

-- Window Properties
INSERT OR IGNORE INTO control_properties (control_type, property_name, property_type, default_value) VALUES
('Window', 'Title', 'string', 'Avalised Window'),
('Window', 'Width', 'double', '800'),
('Window', 'Height', 'double', '600'),
('Window', 'MinWidth', 'double', '0'),
('Window', 'MinHeight', 'double', '0'),
('Window', 'CanResize', 'bool', 'true'),
('Window', 'ShowInTaskbar', 'bool', 'true'),
('Window', 'WindowStartupLocation', 'string', 'CenterScreen');

-- Layout Properties (apply to many controls)
INSERT OR IGNORE INTO control_properties (control_type, property_name, property_type, default_value) VALUES
('DockPanel', 'LastChildFill', 'bool', 'true'),
('DockPanel', 'Background', 'string', '#FFFFFF'),
('StackPanel', 'Orientation', 'string', 'Vertical'),
('StackPanel', 'Spacing', 'double', '0'),
('StackPanel', 'Background', 'string', '#FFFFFF'),
('Canvas', 'Width', 'double', '800'),
('Canvas', 'Height', 'double', '600'),
('Canvas', 'Background', 'string', '#F5F5F5'),
('Border', 'Background', 'string', '#FFFFFF'),
('Border', 'BorderThickness', 'double', '1'),
('Border', 'BorderBrush', 'string', '#CCCCCC'),
('Border', 'CornerRadius', 'double', '0'),
('Border', 'Padding', 'string', '0');

-- Common Control Properties
INSERT OR IGNORE INTO control_properties (control_type, property_name, property_type, default_value) VALUES
('Button', 'Content', 'string', 'Button'),
('Button', 'Width', 'double', '100'),
('Button', 'Height', 'double', '30'),
('Button', 'Margin', 'string', '0'),
('Button', 'HorizontalAlignment', 'string', 'Left'),
('Button', 'VerticalAlignment', 'string', 'Top'),
('TextBox', 'Text', 'string', ''),
('TextBox', 'Width', 'double', '200'),
('TextBox', 'Height', 'double', '30'),
('TextBox', 'Watermark', 'string', ''),
('TextBlock', 'Text', 'string', 'TextBlock'),
('TextBlock', 'FontSize', 'double', '12'),
('TextBlock', 'FontWeight', 'string', 'Normal'),
('TextBlock', 'Foreground', 'string', '#000000'),
('Label', 'Content', 'string', 'Label'),
('Label', 'FontSize', 'double', '12'),
('CheckBox', 'Content', 'string', 'CheckBox'),
('CheckBox', 'IsChecked', 'bool', 'false');

-- MenuItem Properties (already there, but let's ensure)
INSERT OR IGNORE INTO control_properties (control_type, property_name, property_type, default_value) VALUES
('MenuItem', 'Header', 'string', 'Menu Item'),
('MenuItem', 'InputGesture', 'string', ''),
('MenuItem', 'ToolTip', 'string', ''),
('MenuItem', 'IsEnabled', 'bool', 'true');

-- Canvas-specific positioning
INSERT OR IGNORE INTO control_properties (control_type, property_name, property_type, default_value) VALUES
('Button', 'Canvas.Left', 'double', '0'),
('Button', 'Canvas.Top', 'double', '0'),
('TextBox', 'Canvas.Left', 'double', '0'),
('TextBox', 'Canvas.Top', 'double', '0'),
('Label', 'Canvas.Left', 'double', '0'),
('Label', 'Canvas.Top', 'double', '0'),
('TextBlock', 'Canvas.Left', 'double', '0'),
('TextBlock', 'Canvas.Top', 'double', '0');

-- Dock properties
INSERT OR IGNORE INTO control_properties (control_type, property_name, property_type, default_value) VALUES
('Menu', 'DockPanel.Dock', 'string', 'Top'),
('Border', 'DockPanel.Dock', 'string', 'Bottom'),
('Panel', 'DockPanel.Dock', 'string', 'Left'),
('StackPanel', 'DockPanel.Dock', 'string', 'Right');



-- Attached Properties (for layout positioning)
CREATE TABLE IF NOT EXISTS ui_attached_properties (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ui_tree_id INTEGER NOT NULL,
    attached_property_name TEXT NOT NULL,
    property_value TEXT,
    FOREIGN KEY (ui_tree_id) REFERENCES ui_tree(id) ON DELETE CASCADE
);


-- ========== ACTION SYSTEM ==========
-- Soft-coded behavior definitions for menu items and controls

CREATE TABLE IF NOT EXISTS actions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    category TEXT NOT NULL,
    description TEXT,
    requires_params INTEGER NOT NULL DEFAULT 0,
    is_async INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS action_parameters (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    action_name TEXT NOT NULL,
    param_name TEXT NOT NULL,
    param_type TEXT NOT NULL,
    required INTEGER NOT NULL DEFAULT 0,
    default_value TEXT,
    description TEXT,
    UNIQUE(action_name, param_name),
    FOREIGN KEY (action_name) REFERENCES actions(name) ON DELETE CASCADE
);

-- Dialog Actions
INSERT OR IGNORE INTO actions (name, category, description, requires_params, is_async) VALUES
('dialog.info', 'dialog', 'Show information dialog', 1, 1),
('dialog.warning', 'dialog', 'Show warning dialog', 1, 1),
('dialog.error', 'dialog', 'Show error dialog', 1, 1),
('dialog.confirm', 'dialog', 'Show confirmation dialog (Yes/No)', 1, 1),
('dialog.input', 'dialog', 'Show input dialog', 1, 1),
('dialog.open', 'dialog', 'Show file open dialog', 0, 1),
('dialog.save', 'dialog', 'Show file save dialog', 0, 1),
('dialog.folder', 'dialog', 'Show folder selection dialog', 0, 1);

-- File Actions
INSERT OR IGNORE INTO actions (name, category, description, requires_params, is_async) VALUES
('file.new', 'file', 'Create new form', 0, 0),
('file.open', 'file', 'Open AVML file', 0, 1),
('file.save', 'file', 'Save current form', 0, 1),
('file.reload', 'file', 'Reload form from AVML', 0, 0),
('file.export', 'file', 'Export to AVML', 0, 1),
('file.exit', 'file', 'Exit application', 0, 0);

-- Status Actions
INSERT OR IGNORE INTO actions (name, category, description, requires_params, is_async) VALUES
('status.update', 'status', 'Update status bar text', 1, 0),
('status.clear', 'status', 'Clear status bar', 0, 0);

-- Application Actions
INSERT OR IGNORE INTO actions (name, category, description, requires_params, is_async) VALUES
('app.about', 'app', 'Show about dialog', 0, 1),
('app.help', 'app', 'Show help', 0, 1),
('app.options', 'app', 'Show options dialog', 0, 1);

-- Script Actions (future)
INSERT OR IGNORE INTO actions (name, category, description, requires_params, is_async) VALUES
('script.run', 'script', 'Run user script', 1, 1),
('script.eval', 'script', 'Evaluate expression', 1, 0);

-- Canvas Actions
INSERT OR IGNORE INTO actions (name, category, description, requires_params, is_async) VALUES
('canvas.addcontrol', 'canvas', 'Add control to design canvas', 1, 0);

-- ========== ACTION PARAMETERS ==========

-- dialog.info parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('dialog.info', 'title', 'string', 1, 'Information', 'Dialog title'),
('dialog.info', 'message', 'string', 1, NULL, 'Dialog message text');

-- dialog.warning parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('dialog.warning', 'title', 'string', 1, 'Warning', 'Dialog title'),
('dialog.warning', 'message', 'string', 1, NULL, 'Dialog message text');

-- dialog.error parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('dialog.error', 'title', 'string', 1, 'Error', 'Dialog title'),
('dialog.error', 'message', 'string', 1, NULL, 'Dialog message text');

-- dialog.confirm parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('dialog.confirm', 'title', 'string', 1, 'Confirm', 'Dialog title'),
('dialog.confirm', 'message', 'string', 1, NULL, 'Confirmation question'),
('dialog.confirm', 'on_yes', 'action', 0, NULL, 'Action to execute if Yes clicked'),
('dialog.confirm', 'on_no', 'action', 0, NULL, 'Action to execute if No clicked');

-- dialog.input parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('dialog.input', 'title', 'string', 1, 'Input', 'Dialog title'),
('dialog.input', 'message', 'string', 1, NULL, 'Input prompt text'),
('dialog.input', 'default', 'string', 0, '', 'Default value'),
('dialog.input', 'target', 'string', 0, NULL, 'Target property to set with result');

-- dialog.open parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('dialog.open', 'title', 'string', 0, 'Open File', 'Dialog title'),
('dialog.open', 'filter', 'string', 0, '*.*', 'File filter'),
('dialog.open', 'directory', 'string', 0, NULL, 'Initial directory'),
('dialog.open', 'target', 'string', 0, NULL, 'Action to execute with selected file');

-- dialog.save parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('dialog.save', 'title', 'string', 0, 'Save File', 'Dialog title'),
('dialog.save', 'extension', 'string', 0, 'txt', 'Default file extension'),
('dialog.save', 'directory', 'string', 0, NULL, 'Initial directory'),
('dialog.save', 'target', 'string', 0, NULL, 'Action to execute with selected file');

-- dialog.folder parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('dialog.folder', 'title', 'string', 0, 'Select Folder', 'Dialog title'),
('dialog.folder', 'directory', 'string', 0, NULL, 'Initial directory'),
('dialog.folder', 'target', 'string', 0, NULL, 'Action to execute with selected folder');

-- status.update parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('status.update', 'message', 'string', 1, NULL, 'Status message text'),
('status.update', 'timeout', 'int', 0, '0', 'Auto-clear timeout in ms (0=never)');

-- script.run parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('script.run', 'script', 'string', 1, NULL, 'Script path or code to execute'),
('script.run', 'language', 'string', 0, 'csharp', 'Script language (csharp, python, bash)');

-- script.eval parameters
INSERT OR IGNORE INTO action_parameters (action_name, param_name, param_type, required, default_value, description) VALUES
('script.eval', 'expression', 'string', 1, NULL, 'Expression to evaluate'),
('script.eval', 'context', 'string', 0, NULL, 'Evaluation context');


