using FastDB.NET;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FastDB.NET_Browser
{
    public partial class MainWindow : Window
    {
        private FastDatabase _database;
        private Table _selectedTable;
        private DataTable _pageTable;
        private int _pageIndex;
        private int _pageSize = 100;
        private string _tableFilter = string.Empty;
        private bool _uiReady;

        public MainWindow()
        {
            InitializeComponent();
            _uiReady = true;
            NewFieldTypeBox.ItemsSource = Enum.GetValues(typeof(FastDBType)).Cast<FastDBType>().Where(type => type != FastDBType.Null).ToArray();
            NewFieldTypeBox.SelectedItem = FastDBType.String;
            RefreshAll();
        }

        private void NewDatabase_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "FastDB database (*.FastDB)|*.FastDB",
                DefaultExt = ".FastDB",
                AddExtension = true
            };

            if (dialog.ShowDialog(this) != true)
                return;

            string name = Path.GetFileNameWithoutExtension(dialog.FileName);
            string folder = Path.GetDirectoryName(dialog.FileName) ?? Environment.CurrentDirectory;
            _database = new FastDatabase(name, folder);
            _database.Save();
            _selectedTable = null;
            _pageIndex = 0;
            RefreshAll();
            SetStatus("Created " + dialog.FileName);
        }

        private void OpenDatabase_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "FastDB database (*.FastDB)|*.FastDB|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            string password = Prompt("Open database", "Password, leave empty when not encrypted", string.Empty, allowEmpty: true);
            try
            {
                string name = Path.GetFileNameWithoutExtension(dialog.FileName);
                string folder = Path.GetDirectoryName(dialog.FileName) ?? Environment.CurrentDirectory;
                _database = new FastDatabase(name, folder).Connect(string.IsNullOrEmpty(password) ? null : password);
                _selectedTable = _database.Tables.Values.FirstOrDefault();
                _pageIndex = 0;
                RefreshAll();
                SetStatus("Opened " + dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void SaveDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureDatabase())
                return;

            try
            {
                ApplyPageEdits();
                _database.Save();
                RefreshHeader();
                SetStatus("Saved " + _database.FullPath);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void SaveAsDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureDatabase())
                return;

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "FastDB database (*.FastDB)|*.FastDB",
                DefaultExt = ".FastDB",
                AddExtension = true,
                FileName = _database.DatabaseName + ".FastDB"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            string password = Prompt("Save as", "Password for saved copy, leave empty for plain file", string.Empty, allowEmpty: true);
            try
            {
                ApplyPageEdits();
                _database.SaveAs(Path.GetFileNameWithoutExtension(dialog.FileName), Path.GetDirectoryName(dialog.FileName), string.IsNullOrEmpty(password) ? null : password);
                SetStatus("Saved copy " + dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LockDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureDatabase())
                return;

            string password = Prompt("Lock database", "Password", string.Empty, allowEmpty: false);
            if (password == null)
                return;

            try
            {
                _database.Lock(password).Save();
                RefreshHeader();
                SetStatus("Database locked and saved");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void UnlockDatabase_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureDatabase())
                return;

            string password = Prompt("Unlock database", "Current password", string.Empty, allowEmpty: false);
            if (password == null)
                return;

            try
            {
                _database.UnLock(password).Save();
                RefreshHeader();
                SetStatus("Database unlocked and saved");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void AddTable_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureDatabase())
                return;

            string name = Prompt("Create table", "Table name", "NewTable", allowEmpty: false);
            if (name == null)
                return;

            try
            {
                _selectedTable = _database.CreateTable(name);
                _pageIndex = 0;
                RefreshAll();
                SetStatus("Created table " + name);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void RenameTable_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable())
                return;

            string name = Prompt("Rename table", "New name", _selectedTable.Name, allowEmpty: false);
            if (name == null || name == _selectedTable.Name)
                return;

            try
            {
                string oldName = _selectedTable.Name;
                _database.RenameTable(oldName, name);
                _selectedTable = _database.GetTable(name);
                RefreshAll();
                SetStatus("Renamed " + oldName + " to " + name);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void DeleteTable_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable())
                return;

            if (MessageBox.Show(this, "Delete table '" + _selectedTable.Name + "'?", "Delete table", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                string name = _selectedTable.Name;
                _database.RemoveTable(name);
                _selectedTable = _database.Tables.Values.FirstOrDefault();
                _pageIndex = 0;
                RefreshAll();
                SetStatus("Deleted table " + name);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem is TableItem item && _database != null)
            {
                _selectedTable = _database.GetTable(item.Name);
                _pageIndex = 0;
                RefreshTableViews();
            }
        }

        private void TableSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _tableFilter = TableSearchBox.Text ?? string.Empty;
            RefreshTableList();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAll();
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            _pageIndex = 0;
            LoadPage();
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (_pageIndex > 0)
                _pageIndex--;
            LoadPage();
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTable != null && _pageIndex < PageCount() - 1)
                _pageIndex++;
            LoadPage();
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTable != null)
                _pageIndex = Math.Max(0, PageCount() - 1);
            LoadPage();
        }

        private void PageSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_uiReady)
                return;

            if (!(PageSizeBox.SelectedItem is ComboBoxItem item))
                return;

            if (int.TryParse(Convert.ToString(item.Content, CultureInfo.InvariantCulture), out int value))
            {
                _pageSize = value;
                _pageIndex = 0;
                LoadPage();
            }
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable())
                return;

            try
            {
                object[] values = _selectedTable.FieldDefinitions.Select(field => CopyDefault(field.DefaultValue)).ToArray();
                if (!_selectedTable.Insert(values))
                    throw new InvalidOperationException("Insert failed. Row does not match schema.");
                _pageIndex = Math.Max(0, PageCount() - 1);
                LoadPage();
                RefreshHeader();
                SetStatus("Added row to " + _selectedTable.Name);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void DeleteRows_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable())
                return;

            List<int> indexes = RowsGrid.SelectedItems
                .OfType<DataRowView>()
                .Select(view => Convert.ToInt32(view.Row["__RowIndex"], CultureInfo.InvariantCulture))
                .Where(index => index >= 0)
                .Distinct()
                .OrderByDescending(index => index)
                .ToList();

            if (indexes.Count == 0)
                return;

            if (MessageBox.Show(this, "Delete " + indexes.Count + " row(s)?", "Delete rows", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                foreach (int index in indexes)
                    _selectedTable.RemoveAt(index);
                _pageIndex = Math.Min(_pageIndex, Math.Max(0, PageCount() - 1));
                LoadPage();
                RefreshHeader();
                SetStatus("Deleted " + indexes.Count + " row(s)");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ApplyPageEdits_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplyPageEdits();
                LoadPage();
                RefreshHeader();
                SetStatus("Applied page edits");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void AddField_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable())
                return;

            string fieldName = NewFieldNameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(fieldName))
                return;

            FastDBType type = NewFieldTypeBox.SelectedItem is FastDBType selectedType ? selectedType : FastDBType.String;
            object defaultValue = string.IsNullOrWhiteSpace(NewFieldDefaultBox.Text) ? null : NewFieldDefaultBox.Text;

            try
            {
                _selectedTable.AddField(fieldName, type, defaultValue);
                NewFieldNameBox.Text = string.Empty;
                NewFieldDefaultBox.Text = string.Empty;
                RefreshTableViews();
                SetStatus("Added field " + fieldName);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void RemoveField_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable() || !(FieldsGrid.SelectedItem is FieldView field))
                return;

            if (MessageBox.Show(this, "Remove field '" + field.Name + "'?", "Remove field", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                _selectedTable.RemoveField(field.Name);
                RefreshTableViews();
                SetStatus("Removed field " + field.Name);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void CreateIndex_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable() || !(IndexFieldBox.SelectedItem is string fieldName))
                return;

            try
            {
                _selectedTable.CreateIndex(fieldName, UniqueIndexBox.IsChecked == true);
                RefreshIndexes();
                SetStatus("Created index on " + fieldName);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void DropIndex_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable() || !(IndexesGrid.SelectedItem is IndexView index))
                return;

            try
            {
                _selectedTable.DropIndex(index.FieldName);
                RefreshIndexes();
                SetStatus("Dropped index on " + index.FieldName);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ExportJson_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureDatabase())
                return;

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = _database.DatabaseName + ".json"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                ApplyPageEdits();
                _database.ExportJson(dialog.FileName);
                SetStatus("Exported JSON " + dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ImportJson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog { Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*" };
            if (open.ShowDialog(this) != true)
                return;

            try
            {
                if (_database != null)
                {
                    if (MessageBox.Show(this, "Replace current database content with JSON?", "Import JSON", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        return;
                    _database.ImportJsonInto(open.FileName, clearExisting: true);
                }
                else
                {
                    SaveFileDialog save = new SaveFileDialog
                    {
                        Filter = "FastDB database (*.FastDB)|*.FastDB",
                        DefaultExt = ".FastDB",
                        AddExtension = true,
                        FileName = Path.GetFileNameWithoutExtension(open.FileName) + ".FastDB"
                    };
                    if (save.ShowDialog(this) != true)
                        return;
                    _database = FastDatabase.ImportJson(open.FileName, Path.GetFileNameWithoutExtension(save.FileName), Path.GetDirectoryName(save.FileName));
                    _database.Save();
                }

                _selectedTable = _database.Tables.Values.FirstOrDefault();
                _pageIndex = 0;
                RefreshAll();
                SetStatus("Imported JSON " + open.FileName);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable())
                return;

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = _selectedTable.Name + ".csv"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                ApplyPageEdits();
                _selectedTable.ExportCsv(dialog.FileName);
                SetStatus("Exported CSV " + dialog.FileName);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureTable())
                return;

            OpenFileDialog dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" };
            if (dialog.ShowDialog(this) != true)
                return;

            if (MessageBox.Show(this, "Replace rows in '" + _selectedTable.Name + "' from CSV?", "Import CSV", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                int imported = _selectedTable.ImportCsv(dialog.FileName, clearExisting: true);
                _pageIndex = 0;
                RefreshTableViews();
                SetStatus("Imported " + imported + " CSV row(s)");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void RowsGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "__RowIndex")
            {
                e.Cancel = true;
                return;
            }

            if (e.Column is DataGridTextColumn textColumn)
            {
                textColumn.ElementStyle = new Style(typeof(TextBlock));
                textColumn.ElementStyle.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, System.Windows.TextTrimming.CharacterEllipsis));
                textColumn.EditingElementStyle = new Style(typeof(TextBox));
                textColumn.EditingElementStyle.Setters.Add(new Setter(TextBox.PaddingProperty, new Thickness(6, 3, 6, 3)));
            }
        }

        private void RefreshAll()
        {
            RefreshHeader();
            RefreshTableList();
            RefreshTableViews();
        }

        private void RefreshHeader()
        {
            if (_database == null)
            {
                DatabaseBadge.Text = "No database";
                DatabasePathText.Text = string.Empty;
                StatsText.Text = "0 tables, 0 rows";
                return;
            }

            DatabaseBadge.Text = _database.DatabaseName + (_database.IsLocked ? " locked" : string.Empty) + (_database.IsDirty ? " dirty" : string.Empty);
            DatabasePathText.Text = _database.FullPath;
            StatsText.Text = _database.Tables.Count + " tables, " + _database.Tables.Values.Sum(table => table.NbRows).ToString("N0", CultureInfo.InvariantCulture) + " rows";
        }

        private void RefreshTableList()
        {
            if (_database == null)
            {
                TablesList.ItemsSource = null;
                return;
            }

            List<TableItem> items = _database.Tables.Values
                .Where(table => string.IsNullOrWhiteSpace(_tableFilter) || table.Name.IndexOf(_tableFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(table => table.Name, StringComparer.Ordinal)
                .Select(table => new TableItem(table.Name, table.NbRows, table.NbFields))
                .ToList();

            TablesList.ItemsSource = items;
            if (_selectedTable != null)
                TablesList.SelectedItem = items.FirstOrDefault(item => item.Name == _selectedTable.Name);
        }

        private void RefreshTableViews()
        {
            if (_selectedTable == null)
            {
                SelectedTableTitle.Text = "Select a table";
                SelectedTableSubtitle.Text = _database == null ? "Open or create a database to begin." : "Create or select a table.";
                FieldsGrid.ItemsSource = null;
                IndexesGrid.ItemsSource = null;
                IndexFieldBox.ItemsSource = null;
                RowsGrid.ItemsSource = null;
                PageText.Text = string.Empty;
                return;
            }

            SelectedTableTitle.Text = _selectedTable.Name;
            SelectedTableSubtitle.Text = _selectedTable.NbRows.ToString("N0", CultureInfo.InvariantCulture) + " rows, " + _selectedTable.NbFields + " fields";
            FieldsGrid.ItemsSource = _selectedTable.FieldDefinitions.Select(field => new FieldView(field)).ToList();
            IndexFieldBox.ItemsSource = _selectedTable.FieldDefinitions.Where(field => field.Type != FastDBType.ByteArray).Select(field => field.Name).ToList();
            RefreshIndexes();
            LoadPage();
        }

        private void RefreshIndexes()
        {
            if (_selectedTable == null)
            {
                IndexesGrid.ItemsSource = null;
                return;
            }

            IndexesGrid.ItemsSource = _selectedTable.IndexedFields.Select(fieldName => new IndexView(fieldName, _selectedTable.GetField(fieldName).Type)).ToList();
            RefreshHeader();
        }

        private void LoadPage()
        {
            if (!_uiReady || RowsGrid == null)
                return;

            if (_selectedTable == null)
            {
                RowsGrid.ItemsSource = null;
                PageText.Text = string.Empty;
                return;
            }

            _pageIndex = Math.Min(_pageIndex, Math.Max(0, PageCount() - 1));
            _pageTable = new DataTable(_selectedTable.Name);
            _pageTable.Columns.Add("__RowIndex", typeof(int));
            foreach (Field field in _selectedTable.FieldDefinitions)
                _pageTable.Columns.Add(field.Name, typeof(string));

            int start = _pageIndex * _pageSize;
            int end = Math.Min(_selectedTable.NbRows, start + _pageSize);
            for (int rowIndex = start; rowIndex < end; rowIndex++)
            {
                Row row = _selectedTable.Rows[rowIndex];
                DataRow dataRow = _pageTable.NewRow();
                dataRow["__RowIndex"] = rowIndex;
                foreach (Field field in _selectedTable.FieldDefinitions)
                    dataRow[field.Name] = FormatForGrid(row.Get(field.FieldIndex), field.Type);
                _pageTable.Rows.Add(dataRow);
            }

            RowsGrid.ItemsSource = _pageTable.DefaultView;
            PageText.Text = "Page " + (_pageIndex + 1) + " / " + PageCount() + " - rows " + (end == 0 ? 0 : start + 1).ToString("N0", CultureInfo.InvariantCulture) + "-" + end.ToString("N0", CultureInfo.InvariantCulture);
            RefreshHeader();
        }

        private void ApplyPageEdits()
        {
            if (_selectedTable == null || _pageTable == null)
                return;

            RowsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            RowsGrid.CommitEdit(DataGridEditingUnit.Row, true);

            foreach (DataRow dataRow in _pageTable.Rows)
            {
                int rowIndex = Convert.ToInt32(dataRow["__RowIndex"], CultureInfo.InvariantCulture);
                if (rowIndex < 0 || rowIndex >= _selectedTable.NbRows)
                    continue;

                Row row = _selectedTable.Rows[rowIndex];
                foreach (Field field in _selectedTable.FieldDefinitions)
                {
                    object value = dataRow[field.Name] == DBNull.Value ? null : dataRow[field.Name];
                    row.Set(field.FieldIndex, NormalizeGridValue(value, field.Type));
                }
            }
        }

        private int PageCount()
        {
            if (_selectedTable == null || _selectedTable.NbRows == 0)
                return 1;
            return (int)Math.Ceiling((double)_selectedTable.NbRows / _pageSize);
        }

        private bool EnsureDatabase()
        {
            if (_database != null)
                return true;
            MessageBox.Show(this, "Open or create a database first.", "No database", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private bool EnsureTable()
        {
            if (EnsureDatabase() && _selectedTable != null)
                return true;
            MessageBox.Show(this, "Select or create a table first.", "No table", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private static string FormatForGrid(object value, FastDBType type)
        {
            if (value == null)
                return string.Empty;
            if (type == FastDBType.ByteArray)
                return Convert.ToBase64String((byte[])value);
            if (value is DateTime dateTime)
                return dateTime.ToString("O", CultureInfo.InvariantCulture);
            if (value is IFormattable formattable)
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            return value.ToString();
        }

        private static object NormalizeGridValue(object value, FastDBType type)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            switch (type)
            {
                case FastDBType.String:
                    return text;
                case FastDBType.Integer:
                    return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case FastDBType.UnsignedInteger:
                    return uint.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case FastDBType.Float:
                    return float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                case FastDBType.Double:
                    return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
                case FastDBType.Bool:
                    return bool.Parse(text);
                case FastDBType.Date:
                    return DateTime.Parse(text, CultureInfo.InvariantCulture).Date;
                case FastDBType.DateTime:
                    return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                case FastDBType.ByteArray:
                    return Convert.FromBase64String(text);
                default:
                    return text;
            }
        }

        private static object CopyDefault(object value)
        {
            if (value is byte[] bytes)
                return bytes.ToArray();
            return value;
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text;
        }

        private void ShowError(Exception ex)
        {
            MessageBox.Show(this, ex.Message, "FastDB Browser", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Error: " + ex.Message);
        }

        private string Prompt(string title, string label, string initialValue, bool allowEmpty)
        {
            Window dialog = new Window
            {
                Owner = this,
                Title = title,
                Width = 420,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            Grid grid = new Grid { Margin = new Thickness(18) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock labelBlock = new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
            TextBox input = new TextBox { Text = initialValue ?? string.Empty, MinHeight = 32 };
            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button ok = new Button { Content = "OK", Width = 88, IsDefault = true };
            Button cancel = new Button { Content = "Cancel", Width = 88, IsCancel = true, Margin = new Thickness(8, 0, 0, 0) };

            ok.Click += (_, __) =>
            {
                if (!allowEmpty && string.IsNullOrWhiteSpace(input.Text))
                    return;
                dialog.DialogResult = true;
            };

            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            Grid.SetRow(labelBlock, 0);
            Grid.SetRow(input, 1);
            Grid.SetRow(buttons, 3);
            grid.Children.Add(labelBlock);
            grid.Children.Add(input);
            grid.Children.Add(buttons);
            dialog.Content = grid;
            input.Focus();
            input.SelectAll();

            return dialog.ShowDialog() == true ? input.Text : null;
        }

        private sealed class TableItem
        {
            public string Name { get; }
            public int Rows { get; }
            public int Fields { get; }

            public TableItem(string name, int rows, int fields)
            {
                Name = name;
                Rows = rows;
                Fields = fields;
            }
        }

        private sealed class FieldView
        {
            public string Name { get; }
            public FastDBType Type { get; }
            public object DefaultValue { get; }
            public int Index { get; }

            public FieldView(Field field)
            {
                Name = field.Name;
                Type = field.Type;
                DefaultValue = FormatForGrid(field.DefaultValue, field.Type);
                Index = field.FieldIndex;
            }
        }

        private sealed class IndexView
        {
            public string FieldName { get; }
            public FastDBType Type { get; }

            public IndexView(string fieldName, FastDBType type)
            {
                FieldName = fieldName;
                Type = type;
            }
        }
    }
}
