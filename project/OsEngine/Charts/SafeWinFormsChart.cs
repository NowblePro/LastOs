using System;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace OsEngine.Charts
{
    /// <summary>
    /// Workaround for a WinForms Chart Selection tooltip NullReferenceException
    /// that can occur on mouse move while the control is alive and interactive.
    /// </summary>
    public class SafeWinFormsChart : Chart
    {
        private const int WmMouseMove = 0x0200;

        protected override void WndProc(ref Message m)
        {
            try
            {
                base.WndProc(ref m);
            }
            catch (NullReferenceException exception)
                when (m.Msg == WmMouseMove &&
                      exception.StackTrace != null &&
                      exception.StackTrace.Contains("System.Windows.Forms.DataVisualization.Charting.Selection"))
            {
                // Ignore known library issue inside WinForms chart selection tooltip handling.
            }
        }
    }
}
