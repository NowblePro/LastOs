using Grpc.Core;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Drawing;

namespace OsEngine.OsOptimizer
{
    internal class OptimizerCustomPhazesEditor
    {
        private OptimizerMaster _master;
        private DataGridView _dgv;

        public OptimizerCustomPhazesEditor(WindowsFormsHost tableHost, OptimizerMaster master)
        {
            _master = master;
            tableHost.Child = GetDynamicDGV();
        }

        private DataGridView GetDynamicDGV()
        {
            DataGridView gridDynamicStepsTable = DataGridFactory.GetDataGridView(DataGridViewSelectionMode.ColumnHeaderSelect,
                DataGridViewAutoSizeRowsMode.None, false);
            gridDynamicStepsTable.CellClick += DynamicTable_CellClick;
            gridDynamicStepsTable.Click += GridDynamicStepsTable_Click;
            ContextMenu menu = new ContextMenu();
            MenuItem deleteMenuItem = new MenuItem("Удалить", (sender, e) =>
            {
                if (sender is MenuItem item && item.Parent.Tag is Period p)
                {
                    _master.Phazes.OutOfSamplePeriods.Remove(p);
                    UpdateDynamicTable(_dgv);
                }
            });
            menu.MenuItems.Add(deleteMenuItem);

            MenuItem reCalcMenuItem = new MenuItem("Пересчитать", (sender, e) =>
            {
                if (sender is MenuItem item && item.Parent.Tag is Period p)
                {
                    UpdateDynamicTable(_dgv);
                }
            });
            menu.MenuItems.Add(reCalcMenuItem);

            gridDynamicStepsTable.ContextMenu = menu;
            menu.Popup += DynamicTableMenu_Popup;

            gridDynamicStepsTable.ScrollBars = ScrollBars.Vertical;

            gridDynamicStepsTable.Columns.Add(GetColumn("Period"));
            gridDynamicStepsTable.Columns.Add(GetColumn("Start", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("End", readOnly: false));
            gridDynamicStepsTable.Columns.Add(GetColumn("Period name", readOnly: false));

            DataGridViewButtonColumn column11 = new DataGridViewButtonColumn();
            column11.CellTemplate = new DataGridViewButtonCell();
            column11.ReadOnly = true;
            column11.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            gridDynamicStepsTable.Columns.Add(column11);

            gridDynamicStepsTable.Rows.Add(null, null);
            return gridDynamicStepsTable;
        }

        private void DynamicTableMenu_Popup(object sender, EventArgs e)
        {
            if (sender is ContextMenu menu && menu.Tag is Period p)
            {
                menu.MenuItems[0].Visible = p != _master.Phazes.InSamplePeriod;
            }
        }

        private void UpdateDynamicTable(DataGridView table)
        {
            if (!_master.Phazes.InSamplePeriod.IsDefined) return;

            if (table.InvokeRequired)
            {
                table.Invoke(new Action<DataGridView>(UpdateDynamicTable), table);
                return;
            }
            table.Rows.Clear();

            List<Period> periods = new List<Period>() { _master.Phazes.InSamplePeriod };
            periods.AddRange(_master.Phazes.OutOfSamplePeriods);

            FillPeriod(_master.Phazes.InSamplePeriod, "In Sample");

            // Заполнение Out Of Sample периодов
            for (int i = 0; i < _master.Phazes.OutOfSamplePeriods.Count; i++)
            {
                FillPeriod(_master.Phazes.OutOfSamplePeriods[i], "Out Of Sample");
            }

            List<Period> definedPeriods = periods.Where(p => p.IsDefined).ToList();

            DataGridViewRow endRow = new DataGridViewRow() { Height = 30 };
            DataGridViewButtonCell cellEnd = new DataGridViewButtonCell();
            cellEnd.Value = "Добавить Out Of Sample";
            endRow.Cells.Add(cellEnd);
            table.Rows.Add(endRow);

            _master.OnPeriodsChanged();

            void FillPeriod(Period period, string periodName)
            {
                lock (table)
                {
                    DataGridViewRow row = new DataGridViewRow() { Height = 30 };

                    DataGridFactory.AddTextBoxCell(row, periodName, true);

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
                                DataGridViewTextBoxCell cell = DataGridFactory.AddTextBoxCell(row, period.Start.Value.ToString("dd.MM.yyyy"));
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
                                DataGridViewTextBoxCell cell = DataGridFactory.AddTextBoxCell(row, period.End.Value.ToString("dd.MM.yyyy"));
                                cell.ToolTipText = period.End.Value.ToString();
                            }
                        }
                        else
                        {
                            if (period.IsDefined)
                            {
                                if (i == 3)
                                {
                                    // Название периода (опционально)
                                    DataGridFactory.AddTextBoxCell(row, period.Name, false);
                                }
                            }
                            else
                            {
                                DataGridFactory.AddTextBoxCell(row, "-", true);
                            }
                        }
                    }
                    table.Rows.Add(row);
                }
            }
        }

        private void DynamicTable_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (sender is DataGridView dgv)
            {
                bool changed = false;

                Period prevPeriod = GetPeriod(e.RowIndex - 1);
                Period period = GetPeriod(e.RowIndex);
                Period nextPeriod = GetPeriod(e.RowIndex + 1);

                if (period == null)
                {
                    if (e.ColumnIndex == 0)
                    {
                        _master.Phazes.OutOfSamplePeriods.Add(new Period());
                        changed = true;
                    }
                }
                else
                {
                    if (e.ColumnIndex == 1)
                    {
                        DateTimeSelectionDialog dialog = new DateTimeSelectionDialog(period?.Start ?? prevPeriod?.End ?? _master.TimeStart);
                        dialog.ShowDialog();
                        if (dialog.IsSaved)
                        {
                            period.Start = dialog.Time;
                            changed = true;
                        }
                    }

                    if (e.ColumnIndex == 2)
                    {
                        DateTimeSelectionDialog dialog = new DateTimeSelectionDialog(period?.End ?? nextPeriod?.Start ?? period.Start ?? _master.TimeEnd);
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
                    UpdateDynamicTable(dgv);
                }
            }
        }

        Period GetPeriod(int rowIndex)
        {
            if (rowIndex == 0)
            {
                return _master.Phazes.InSamplePeriod;
            }
            else
            {
                if (rowIndex - 1 < _master.Phazes.OutOfSamplePeriods.Count && rowIndex > 0)
                {
                    return _master.Phazes.OutOfSamplePeriods[rowIndex - 1];
                }
                else
                {
                    return null;
                }
            }
        }

        private DataGridViewColumn GetColumn(string name, int width = 0, bool readOnly = true)
        {
            DataGridViewColumn column = new DataGridViewColumn();
            DataGridViewCell cellTemplate = new DataGridViewTextBoxCell();
            cellTemplate.Style = DataGridFactory.CellStyle;
            column.CellTemplate = cellTemplate;
            column.HeaderText = name;
            column.ReadOnly = readOnly;
            if (width > 0)
            {
                column.Width = width;
            }
            else
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
            return column;
        }

        private void GridDynamicStepsTable_Click(object sender, EventArgs e)
        {
            if (sender is DataGridView dgv && e is MouseEventArgs mouse)
            {
                var info = dgv.HitTest(mouse.X, mouse.Y);
                if (mouse.Button != MouseButtons.Right || info.Type != DataGridViewHitTestType.Cell || info.RowIndex < 0 || info.RowIndex > _master.Phazes.OutOfSamplePeriods.Count)
                {
                    return;
                }
                if (info.RowIndex == 0)
                {
                    dgv.ContextMenu.Tag = _master.Phazes.InSamplePeriod;
                }
                else
                {
                    dgv.ContextMenu.Tag = _master.Phazes.OutOfSamplePeriods[info.RowIndex - 1];
                }

                dgv.ContextMenu.Show(dgv, new Point(mouse.X, mouse.Y));
            }
        }
    }
}
