using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DsxKline_WinForm.dsxkline;

namespace DsxKline_WinForm
{
    public partial class Form1 : Form
    {
        private string code = "sz300502";
        private string cycle = "day";
        private List<string> datas;
        private DsxKline dsxkline;
        private List<HqModel> hqModels = [];
        int page = 1;
        public Form1()
        {
            InitializeComponent();
            dsxkline = new DsxKline();
            this.splitContainer1.Panel2.Controls.Add(dsxkline);
            dsxkline.Dock = DockStyle.Fill;
            dsxkline.onLoading = (() =>
            {
                Console.WriteLine("onLoading");
                page = 1;
                datas = [];
                if (dsxkline.chartType == DsxKline.ChartType.timeSharing) getQuote(code);
                if (dsxkline.chartType == DsxKline.ChartType.timeSharing5) getTimeLine5();
                if (dsxkline.chartType == DsxKline.ChartType.candle) getDay();
            });
            dsxkline.nextPage = (() =>
            {
                // 继续请求下一页
                // .....

                // 完成后执行
                dsxkline.FinishLoading();
            });
            dsxkline.onCrossing = ((data, index) =>
            {
                // 十字线滑动数据
                Console.WriteLine(data);
            });
        }

        public void getQuote(string code)
        {
            hqModels = QqHq.GetQuote(code);
            if (hqModels?.Count > 0)
            {
                HqModel d = hqModels[0];
                dsxkline.lastClose = double.Parse(d.lastClose);
                if (cycle.Equals("timeline")) getTimeLine();
                if (cycle.Equals("timeline5")) getTimeLine5();
            }
            else MessageBox.Show($"{code}代码错误！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        public void getQuoteRefresh(string code)
        {
            if (dsxkline != null) return;
            hqModels = QqHq.GetQuote(code);
            HqModel d = hqModels[0];

            var item = $"{d.date.Replace("-", "").Replace("-", "")},{d.time.Replace(":", "")[..4]},{d.price},{d.vol},{d.volAmount}";
            if (dsxkline.chartType == DsxKline.ChartType.candle)
            {
                if (cycle.StartsWith("m1"))
                {
                    item = $"{d.date.Replace("-", "").Replace("-", "")},{d.time.Replace(":", "")[..4]},{d.price},{d.price},{d.price},{d.price},{d.vol},{d.volAmount}";
                }
                else
                {
                    item = $"{d.date.Replace("-", "").Replace("-", "")},{d.open},{d.high},{d.low},{d.price},{d.vol},{d.volAmount}";
                }
            }
            //console.log(item);
            var c = "t";
            if (cycle == "day") c = "d";
            if (cycle == "week") c = "w";
            if (cycle == "month") c = "m";
            if (cycle == "year") c = "y";
            if (cycle == "min1") c = "m1";
            if (cycle == "timeline") c = "t";
            if (cycle == "timeline5") c = "t5";
            //console.log(cycle+"_"+item);
            dsxkline.RefreshLastOneData(item, c);




        }

        public void getTimeLine()
        {
            List<string> data = QqHq.GetTimeLine(code);
            if (data.Count > 0)
            {
                //d.data = [];
                datas = data;

                dsxkline.Update(datas, page);
                page++;
            }
            dsxkline.FinishLoading();

        }

        public void getTimeLine5()
        {
            Dictionary<string, dynamic> data = QqHq.GetFdayLine(code);
            if (data.Count > 0)
            {
                //d.data = [];
                datas = data["data"];
                dsxkline.lastClose = data["lastClose"];
                dsxkline.Update(datas, page);
                page++;
            }
            dsxkline.FinishLoading();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            tab(0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            tab(1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            tab(2);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            tab(3);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            tab(4);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            tab(5);
        }

        private void cboCode_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    code = cboMarket.Items.OfType<string>().Any(i => cboCode.Text.StartsWith(i.ToLower())) ? cboCode.Text : uint.TryParse(cboCode.Text, out _) ? $"{cboMarket.SelectedItem.ToString().ToLower()}{cboCode.Text}" : "sh0000001";
                    dsxkline.StartLoading();
                    getQuote(code);
                    Text = hqModels.Count > 0 ? $"{hqModels[0].name} {hqModels[0].code}" : "股票K线";

                    break;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tab(2);
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
        }



        private void getDay()
        {
            if (cycle.StartsWith('m') && !cycle.StartsWith("month"))
            {
                if (code.StartsWith("hk") || code.StartsWith("us"))
                {
                    dsxkline.FinishLoading();
                    return;
                }
                List<string> data = QqHq.GetMinLine(code, cycle, 320);
                if (data.Count > 0)
                {
                    //d.data = [];
                    if (page <= 1) datas = data;

                    dsxkline.Update(datas, page);
                    page++;
                }
                dsxkline.FinishLoading();

            }
            else
            {
                List<string> data = QqHq.GetKLine(code, cycle, "", "", 320, "qfqday");
                if (data.Count > 0)
                {
                    //d.data = [];
                    if (page <= 1) datas = data;

                    dsxkline.Update(datas, page);
                    page++;
                }
                dsxkline.FinishLoading();
            }
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tab(int i)
        {
            if (i == 0) dsxkline.chartType = DsxKline.ChartType.timeSharing;
            if (i == 1) dsxkline.chartType = DsxKline.ChartType.timeSharing5;
            if (i >= 2) dsxkline.chartType = DsxKline.ChartType.candle;
            if (i == 0) cycle = "timeline";
            if (i == 1) cycle = "timeline5";
            if (i == 2) cycle = "day";
            if (i == 3) cycle = "week";
            if (i == 4) cycle = "month";
            if (i == 5) cycle = "m1";

            dsxkline.StartLoading();
        }
    }
}
