using Newtonsoft.Json;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.OsOptimizer.OptEntity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;

namespace OsEngine.Common.UI
{
    /// <summary>
    /// Логика взаимодействия для SessionEditor.xaml
    /// </summary>
    public partial class SessionEditor : Window
    {
        private DataGridView _gridSessions;
        private static List<Period> _sessions;
        private static string path = @"Engine\Sessions.json";

        public SessionEditor()
        {
            InitializeComponent();
            _gridSessions = GetTable();
            HostSessionTable.Child = _gridSessions;
        }

        public static List<Period> Sessions
        {
            get
            {
                if (_sessions == null)
                {
                    LoadSessions();
                }
                return _sessions;
            }
        }

        private DataGridView GetTable()
        {
            DataGridView result;
            result = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.ColumnHeaderSelect,
                DataGridViewAutoSizeRowsMode.None, false);
            result.CellClick += DynamicTable_CellClick;
            result.Click += GridTable_Click;
            result.CellValueChanged += _dgv_CellValueChanged;
            ContextMenu menu = new ContextMenu();
            MenuItem deleteMenuItem = new MenuItem("Удалить", (sender, e) =>
            {
                if (sender is MenuItem item && item.Parent.Tag is Period p)
                {
                    Sessions.Remove(p);
                    UpdateTable(result);
                }
            });
            menu.MenuItems.Add(deleteMenuItem);

            result.ContextMenu = menu;

            result.ScrollBars = ScrollBars.Vertical;

            result.Columns.Add(DataGridFactory.GetColumn("Period name", readOnly: false));
            result.Columns.Add(DataGridFactory.GetColumn("Start", readOnly: false));
            result.Columns.Add(DataGridFactory.GetColumn("End", readOnly: false));

            result.Rows.Add(null, null);
            UpdateTable(result);
            return result;
        }

        private void DynamicTable_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (sender is DataGridView dgv)
            {
                bool changed = false;
                Period period = GetPeriod(e.RowIndex);
                if (period == null)
                {
                    if (e.ColumnIndex == 0)
                    {
                        Sessions.Add(new Period());
                        changed = true;
                    }
                }
                else
                {
                    if (e.ColumnIndex == 1)
                    {
                        DateTime? edit = period.Start;
                        if (edit == null)
                        {
                            edit = DateTime.MinValue;
                        }

                        DateTimeSelectionDialog dialog = new DateTimeSelectionDialog((DateTime)edit);
                        dialog.ShowDialog();
                        if (dialog.IsSaved)
                        {
                            period.Start = dialog.Time;
                            changed = true;
                        }
                    }

                    if (e.ColumnIndex == 2)
                    {
                        DateTime? edit = period.End;
                        if (edit == null)
                        {
                            edit = DateTime.MinValue;
                        }

                        DateTimeSelectionDialog dialog = new DateTimeSelectionDialog((DateTime)edit);
                        dialog.ShowDialog();
                        if (dialog.IsSaved)
                        {
                            period.End = dialog.Time;
                            changed = true;
                        }
                    }
                }
                
                if (changed)
                {
                    UpdateTable(dgv);
                }
            }
        }

        private Period GetPeriod(int rowIndex)
        {
            if (rowIndex >= Sessions.Count) return null;

            Period period = Sessions[rowIndex];

            return period;
        }

        private static void SaveSessions()
        {
            using (StreamWriter writer = new StreamWriter(path, false))
            {
                string str = JsonConvert.SerializeObject(_sessions, Formatting.Indented);
                writer.WriteLine(str);
            }
        }

        private static void LoadSessions()
        {
            _sessions = new List<Period>()
            {
                new Period() { Name = "London", Start = new DateTime(1970, 1, 1, 8, 0, 0), End = new DateTime(1970, 1, 1, 17, 0, 0) },
                new Period() { Name = "Asia", Start = new DateTime(1970, 1, 1, 0, 0, 0), End = new DateTime(1970, 1, 1, 8, 0, 0) },
                new Period() { Name = "NY AM", Start = new DateTime(1970, 1, 1, 13, 0, 0), End = new DateTime(1970, 1, 1, 22, 0, 0) },
            };
            if (File.Exists(path))
            {
                try
                {
                    List<Period> sessions;
                    using (StreamReader reader = new StreamReader(path))
                    {
                        string str = reader.ReadToEnd();

                        sessions = JsonConvert.DeserializeObject(str, typeof(List<Period>)) as List<Period>;
                    }

                    foreach (Period s in sessions)
                    {
                        Period session = _sessions.Find(p => p.Name == s.Name);
                        if (session != null)
                        {
                            session.Start = s.Start;
                            session.End = s.End;
                        }
                        else
                        {
                            _sessions.Add(s);
                        }
                    }
                }
                catch { }
                if (_sessions == null) _sessions = new List<Period>();
            }
        }

        private void UpdateTable(DataGridView table)
        {
            if (table.InvokeRequired)
            {
                table.Invoke(new Action<DataGridView>(UpdateTable), table);
                return;
            }

            table.Rows.Clear();

            foreach (Period period in Sessions)
            {
                FillPeriod(period);
            }

            DataGridViewRow endRow = new DataGridViewRow() { Height = 30 };
            DataGridViewButtonCell cellEnd = new DataGridViewButtonCell();
            cellEnd.Value = "Добавить";
            endRow.Cells.Add(cellEnd);
            table.Rows.Add(endRow);

            void FillPeriod(Period period)
            {
                lock (table)
                {
                    DataGridViewRow row = new DataGridViewRow() { Height = 30 };

                    DataGridFactory.AddTextBoxCell(row, period.Name, false);

                    //TimeZoneInfo info = TimeZoneInfo.Local;
                    //TimeSpan offset = info.GetUtcOffset(DateTime.UtcNow);

                    for (int i = 1; i < table.Columns.Count; i++)
                    {
                        if (i == 1)
                        {
                            if (period.Start == null)
                            {
                                DataGridViewButtonCell cell1 = new DataGridViewButtonCell();
                                cell1.Value = "Редактировать";
                                row.Cells.Add(cell1);
                            }
                            else
                            {
                                DataGridViewTextBoxCell cell = DataGridFactory.AddTextBoxCell(row, period.Start.Value.ToString(OsLocalization.LongTimePattern));
                                cell.ToolTipText = period.Start.Value.ToString();
                            }
                        }
                        else if (i == 2)
                        {
                            if (period.End == null)
                            {
                                DataGridViewButtonCell cell1 = new DataGridViewButtonCell();
                                cell1.Value = "Редактировать";
                                row.Cells.Add(cell1);
                            }
                            else
                            {
                                DataGridViewTextBoxCell cell = DataGridFactory.AddTextBoxCell(row, period.End.Value.ToString(OsLocalization.LongTimePattern));
                                cell.ToolTipText = period.End.Value.ToString();
                            }
                        }
                    }
                    table.Rows.Add(row);
                }
            }
        }

        private void _dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (sender is DataGridView dgv)
                {
                    if (e.ColumnIndex == 0)
                    {
                        Period period = GetPeriod(e.RowIndex);
                        string value = $"{dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value}";
                        if (period != null)
                        {
                            period.Name = value;
                        }
                    }
                }
            }
            catch { }
        }

        private void GridTable_Click(object sender, EventArgs e)
        {
            if (sender is DataGridView dgv && e is MouseEventArgs mouse)
            {
                var info = dgv.HitTest(mouse.X, mouse.Y);
                if (mouse.Button != MouseButtons.Right || info.Type != DataGridViewHitTestType.Cell || info.RowIndex < 0 || info.RowIndex >= Sessions.Count)
                {
                    return;
                }
                dgv.ContextMenu.Tag = Sessions[info.RowIndex];

                dgv.ContextMenu.Show(dgv, new System.Drawing.Point(mouse.X, mouse.Y));
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSessions();
        }
    }
}
