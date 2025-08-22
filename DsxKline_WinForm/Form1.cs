using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DsxKline_WinForm.dsxkline;

namespace DsxKline_WinForm
{
    public partial class Form1 : Form
    {
        DsxKline dsxkline;
        int page = 1;
        List<string> datas;
        string cycle = "day";
        string code = "sz300502";
        int y = 50;
        int x = 5;
        public Form1()
        {
            InitializeComponent();
            dsxkline = new DsxKline();
            this.Controls.Add(dsxkline);
            dsxkline.SetBounds(x, y, this.ClientRectangle.Width - 2 * x, this.ClientRectangle.Height-y);
            dsxkline.onLoading = (() => {
                Console.WriteLine("onLoading");
                page = 1;
                datas = [];
                if (dsxkline.chartType == DsxKline.ChartType.timeSharing) getQuote(code);
                if (dsxkline.chartType == DsxKline.ChartType.timeSharing5) getTimeLine5();
                if (dsxkline.chartType == DsxKline.ChartType.candle) getDay();
            });
            dsxkline.nextPage = (() => {
                // 继续请求下一页
                // .....

                // 完成后执行
                dsxkline.FinishLoading();
            });
            dsxkline.onCrossing = ((data,index) => {
                // 十字线滑动数据
                Console.WriteLine(data);
            });
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

                    dsxkline.Update(datas,page);
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

        public void getQuote(string code)
        {
            List<HqModel> hqModels = QqHq.GetQuote(code);
                HqModel d = hqModels[0];
            dsxkline.lastClose = double.Parse(d.lastClose);
            if (cycle.Equals("timeline")) getTimeLine();
            if (cycle.Equals("timeline5")) getTimeLine5();
           
        }

        public void getQuoteRefresh(string code)
        {
            if (dsxkline!=null) return;
            List<HqModel> data = QqHq.GetQuote(code);
                HqModel d = data[0];
  
                var item = d.date.Replace("-", "").Replace("-", "") + "," + d.time.Replace(":", "")[..4] + "," + d.price + "," + d.vol + "," + d.volAmount;
                if (dsxkline.chartType == DsxKline.ChartType.candle)
                {
                    if (cycle.StartsWith("m1"))
                    {
                        item = d.date.Replace("-", "").Replace("-", "") + "," + d.time.Replace(":", "")[..4] + "," + d.price + "," + d.price + "," + d.price + "," + d.price + "," +d.vol + "," + d.volAmount;
                    }
                    else
                    {
                        item = d.date.Replace("-", "").Replace("-", "") + "," + d.open + "," + d.high + "," + d.low + "," + d.price + "," + d.vol + "," + d.volAmount;
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


        private void Form1_Load(object sender, EventArgs e)
        {
            tab(2);
           // tab(2);
        }
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            dsxkline?.SetBounds(x, y, this.ClientRectangle.Width - 2 * x, this.ClientRectangle.Height-y);
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
    }
}
