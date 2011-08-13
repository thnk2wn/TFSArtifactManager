using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;

namespace TFSArtifactManager.Views
{
    /// <summary>
    /// Interaction logic for OracleSqlEditor.xaml
    /// </summary>
    public partial class SqlEditor  //: UserControl
    {
        public SqlEditor()
        {
            InitializeComponent();
            uxPropertyGridComboBox.SelectedIndex = 2;
            uxSqlEditor.ShowLineNumbers = true;

            _sqlViewSyntaxMap.Add(SqlViews.Oracle, LoadPlSqlSyntax);
            _sqlViewSyntaxMap.Add(SqlViews.SqlServer, LoadTSqlSyntax);
            //this.SqlView = SqlViews.Oracle;
        }

        private readonly Dictionary<SqlViews, Action> _sqlViewSyntaxMap = new Dictionary<SqlViews, Action>();

        private SqlViews? _sqlView;
        public SqlViews? SqlView
        {
            get { return _sqlView; }
            set
            {
                if (_sqlView != value)
                {
                    _sqlView = value;

                    // looks like having the same file extension (i.e. sql) for 2 syntaxes is causing avalon edit
                    // issues in cases (i.e. sql server syntax picking up oracle syntax)
                    if (_sqlView.HasValue)
                        _sqlViewSyntaxMap[_sqlView.Value]();
                }
            }
        }

        private void LoadPlSqlSyntax()
        {
            LoadSqlSyntax("PLSQL", new[] { ".sql", ".pkb", ".pks", ".fnc", ".prc", ".trg", ".vw" });
        }

        private void LoadTSqlSyntax()
        {
            LoadSqlSyntax("TSql", new[] { ".sql", ".tab", ".viw", ".trn" });
        }

        private void LoadSqlSyntax(string name, string[] extensions)
        {
            var definition = LoadHighlightingDefinition(name);
            HighlightingManager.Instance.RegisterHighlighting(name, extensions, definition);
            uxSqlEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(name);
        }

        private static IHighlightingDefinition LoadHighlightingDefinition(string name)
        {
            // Load our custom highlighting definition
            IHighlightingDefinition customHighlighting;
            var resourceName = string.Format("TFSArtifactManager.{0}.xshd", name);
            using (var s = typeof(SqlEditor).Assembly.GetManifestResourceStream(resourceName))
            {
                if (s == null)
                    throw new InvalidOperationException("Could not find embedded resource " + resourceName);
                using (XmlReader reader = new XmlTextReader(s))
                {
                    customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                        HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }

            return customHighlighting;
        }

        public new void Focus()
        {
            base.Focus();
            uxSqlEditor.Focus();
        }

        private string _currentFileName;

        private void OpenFileClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog {CheckFileExists = true};
            if (dlg.ShowDialog() ?? false)
                OpenFile(dlg.FileName);
        }

        private void EnsureSyntaxSet()
        {
            if (null == this.SqlView)
                throw new NullReferenceException("SqlView must first be set");
        }

        public void OpenFile(string filename)
        {
            EnsureSyntaxSet();
            if (!File.Exists(filename))
            {
                MessageBox.Show(string.Format("The file '{0}' does not exist.", filename), @"File Not Found");
                return;
            }

            _currentFileName = filename;
            uxSqlEditor.Load(_currentFileName);
            uxSqlEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(_currentFileName));
        }

        public bool IsReadOnly
        {
            get { return uxSqlEditor.IsReadOnly; }
            set 
            { 
                uxSqlEditor.IsReadOnly = value;
                uxSaveButton.IsEnabled = !value;
                uxCutButton.IsEnabled = !value;
                uxDeleteButton.IsEnabled = !value;
            }
        }

        private void SaveFileClick(object sender, EventArgs e)
        {
            if (_currentFileName == null)
            {
                var dlg = new SaveFileDialog {DefaultExt = ".sql"};
                if (dlg.ShowDialog() ?? false)
                {
                    _currentFileName = dlg.FileName;
                }
                else
                {
                    return;
                }
            }
            uxSqlEditor.Save(_currentFileName);
        }

        private void PropertyGridComboBoxSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (propertyGrid == null)
                return;
            switch (uxPropertyGridComboBox.SelectedIndex)
            {
                case 0:
                    propertyGrid.SelectedObject = uxSqlEditor;
                    break;
                case 1:
                    propertyGrid.SelectedObject = uxSqlEditor.TextArea;
                    break;
                case 2:
                    propertyGrid.SelectedObject = uxSqlEditor.Options;
                    break;
            }
        }

        public string SqlText
        {
            get { return uxSqlEditor.Text; }
            set
            {
                EnsureSyntaxSet();
                uxSqlEditor.Text = value;
            }
        }
    }

    
}
