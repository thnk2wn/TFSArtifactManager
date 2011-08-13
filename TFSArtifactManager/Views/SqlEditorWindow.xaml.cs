using System.Windows;
using System.Windows.Input;

namespace TFSArtifactManager.Views
{
    /// <summary>
    /// Interaction logic for OracleSqlEditorWindow.xaml
    /// </summary>
    public partial class SqlEditorWindow //: Window
    {
        public SqlEditorWindow()
        {
            InitializeComponent();
            this.Loaded += SqlEditorWindow_Loaded;
        }

        private void SqlEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            uxSqlEditor.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
                this.Close();
        }

        public SqlEditor Editor
        {
            get { return uxSqlEditor; }
        }
    }
}
