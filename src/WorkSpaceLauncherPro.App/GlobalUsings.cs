// Project-level global usings + aliases
// Force WPF types to win over WinForms when both are in scope
// (we add <UseWindowsForms>true</UseWindowsForms> for NotifyIcon support).

global using Application = System.Windows.Application;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;
global using Button = System.Windows.Controls.Button;
global using UserControl = System.Windows.Controls.UserControl;
global using Window = System.Windows.Window;
global using MessageBox = System.Windows.MessageBox;
global using MessageBoxButton = System.Windows.MessageBoxButton;
global using MessageBoxImage = System.Windows.MessageBoxImage;
global using MessageBoxResult = System.Windows.MessageBoxResult;
global using Color = System.Windows.Media.Color;
global using Cursors = System.Windows.Input.Cursors;
global using Rectangle = System.Windows.Shapes.Rectangle;
global using NotifyIcon = System.Windows.Forms.NotifyIcon;
global using ContextMenuStrip = System.Windows.Forms.ContextMenuStrip;
global using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;
global using ToolStripSeparator = System.Windows.Forms.ToolStripSeparator;
global using Icon = System.Drawing.Icon;
global using SystemIcons = System.Drawing.SystemIcons;
