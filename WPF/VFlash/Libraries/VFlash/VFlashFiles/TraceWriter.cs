using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text;
using VFlash.ViewModel;

namespace VFlashFiles {
    internal class TraceWriter {
        private string filePath;

        public TraceWriter(string filePath) {
            this.filePath = filePath;
        }

        public bool Save(ObservableCollection<TraceViewModel> traceList) {
            return SaveXlsxFile(traceList);
        }

        private string TimeStampStringFormat(long ms) {
            long s = ms / 1000;
            StringBuilder timeStamp = new StringBuilder();
            timeStamp.Append((s / 60 % 60).ToString("00"));
            timeStamp.Append(":");
            timeStamp.Append((s % 60).ToString("00"));
            timeStamp.Append(".");
            timeStamp.Append((ms % 1000).ToString("000"));
            return timeStamp.ToString();
        }

        private string DeltaTimeStringFormat(long value) {
            if(value < 0)
                return "";
            string str;
            if(value < 10 * 1000)
                str = ((double)value / 1000).ToString("0.000") + "s";
            else if(value < 60 * 1000)
                str = ((double)value / 1000).ToString("0.00") + "s";
            else if(value < 10 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.000") + "p";
            else if(value < 100 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.00") + "p";
            else if(value < 1000 * 60 * 1000)
                str = ((double)value / 1000 / 60).ToString("0.0") + "p";
            else
                str = (value / 1000 / 60).ToString() + "p";
            return str;
        }

        private string ArrayToString(byte[] data) {
            if(data == null)
                return "";
            StringBuilder str = new StringBuilder();
            for(int i = 0; i < data.Length; i++) {
                str.Append(data[i].ToString("X2"));
                str.Append(" ");
            }
            return str.ToString().Trim();
        }

        private Color GetResponseColor(byte[] rxData) {
            if(rxData[0] == 0x7F) {
                if(rxData.Length == 3 && rxData[2] == 0x78)
                    return Color.Orange;
                return Color.DarkRed;
            }
            return Color.DarkGreen;
        }

        private void SetBorderColor(ExcelRange range, Color color) {
            Border border = range.Style.Border;

            border.Left.Style = ExcelBorderStyle.Thin;
            border.Left.Color.SetColor(color);

            border.Top.Style = ExcelBorderStyle.Thin;
            border.Top.Color.SetColor(color);

            border.Right.Style = ExcelBorderStyle.Thin;
            border.Right.Color.SetColor(color);

            border.Bottom.Style = ExcelBorderStyle.Thin;
            border.Bottom.Color.SetColor(color);
        }

        private bool SaveXlsxFile(ObservableCollection<TraceViewModel> traceList) {
            FileStream stream = File.Create(filePath);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            ExcelPackage package = new ExcelPackage(stream);
            ExcelWorksheet workSheet = package.Workbook.Worksheets.Add("VFlash Trace");
            workSheet.Columns[1].Width = 12;
            workSheet.Columns[2].Width = 6;
            workSheet.Columns[3].Width = 6;
            workSheet.Columns[4].Width = 100;
            workSheet.Columns[5].Width = 100;
            workSheet.Columns[6].Width = 12;
            workSheet.Rows[1].Height = 30;

            workSheet.Cells[1, 1].Value = "TimeStamp";
            workSheet.Cells[1, 2].Value = "TxId";
            workSheet.Cells[1, 3].Value = "RxId";
            workSheet.Cells[1, 4].Value = "Request Data";
            workSheet.Cells[1, 5].Value = "Response Data";
            workSheet.Cells[1, 6].Value = "Delta Time";
            workSheet.Cells["A1:F1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            workSheet.Cells["A1:F1"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            workSheet.Cells["A1:F1"].Style.Font.Bold = true;
            workSheet.Cells["A1:F1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            workSheet.Cells["A1:F1"].Style.Fill.BackgroundColor.SetColor(Color.Orange);

            for(int i = 0; i < traceList.Count; i++) {
                workSheet.Cells[i + 2, 1].Value = TimeStampStringFormat(traceList[i].TimeStamp);
                workSheet.Cells[i + 2, 2].Value = traceList[i].TxId.ToString("X3");
                workSheet.Cells[i + 2, 3].Value = traceList[i].RxId.ToString("X3");
                workSheet.Cells[i + 2, 4].Value = ArrayToString(traceList[i].RequestData);
                if(traceList[i].SendSuccessfull == false)
                    workSheet.Cells[i + 2, 4].Style.Font.Color.SetColor(Color.DarkRed);
                workSheet.Cells[i + 2, 5].Value = ArrayToString(traceList[i].ResponseData);
                if(traceList[i].ResponseData != null)
                    workSheet.Cells[i + 2, 5].Style.Font.Color.SetColor(GetResponseColor(traceList[i].ResponseData));
                workSheet.Cells[i + 2, 6].Value = DeltaTimeStringFormat(traceList[i].DeltaTime);
            }
            int rowIndex = traceList.Count + 1;
            workSheet.Cells["A2:C" + rowIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            workSheet.Cells["F2:F" + rowIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            workSheet.Cells["D2:E" + rowIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            SetBorderColor(workSheet.Cells["A1:F" + rowIndex], Color.Blue);

            package.Save();
            stream.Close();

            return true;
        }
    }
}
