using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace DsxKline_WinForm.dsxkline;

internal class DsxKline : Control
{
    public ChromiumWebBrowser browser;

    public delegate void OnLoading();

    public delegate void NextPage();

    public delegate void UpdateComplate();

    public delegate void OnCrossing(string data, int index);



    // 图表类型
    public enum ChartType
    {
        timeSharing,    // 分时图
        timeSharing5,   // 五日分时图
        candle,         // K线图
    };

    // 蜡烛图实心空心
    public enum CandleType
    {
        hollow, // 空心
        solid   // 实心
    };

    // 缩放K线锁定类型
    public enum ZoomLockType
    {
        none,       // 无
        left,       // 锁定左边进行缩放
        middle,     // 锁定中间进行缩放
        right,      // 锁定右边进行缩放
        follow,     // 跟随鼠标位置进行缩放，web版效果比较好
    };

    private static readonly string homeUrl = $@"{AppDomain.CurrentDomain.BaseDirectory}dsxkline\index.html";

    public List<string> Datas;

    // 主题 white dark 等
    public string theme = "dark";

    // 图表类型 1=分时图 2=k线图
    public ChartType chartType = ChartType.timeSharing;

    // 蜡烛图k线样式 1=空心 2=实心
    public CandleType candleType = CandleType.hollow;

    // 缩放类型 1=左 2=中 3=右 4=跟随
    public ZoomLockType zoomLockType = ZoomLockType.left;

    // 每次缩放大小
    public double zoomStep = 1;

    // k线默认宽度
    public double klineWidth = 5;

    // 是否显示默认k线提示
    public bool isShowKlineTipPannel = true;

    // 副图高度
    public double sideHeight = 60;

    // 高度
    public double height;

    // 宽度
    public double width;

    // 默认主图指标 ["MA"]
    public string[] main = ["MA"];

    // 默认副图指标 副图数组代表副图数量 ["VOL","MACD"]
    public string[] sides = ["VOL", "MACD"];  //, "RSI"

    // 昨日收盘价
    public double lastClose = 0;

    // 首次加载回调
    public OnLoading onLoading;

    // 完成加载回调
    public UpdateComplate updateComplate;

    // 滚动到左边尽头回调 通常用来加载下一页数据
    public NextPage nextPage;

    // 提示数据返回
    public OnCrossing onCrossing;

    // 右边空出k线数量
    public int rightEmptyKlineAmount = 2;

    // 当前页码
    public int Page = 1;

    // 开启调试
    public bool debug = false;

    public double paddingBottom = 0;

    public DsxKline()
    {
        InitializeComponent();
        InitBrowser(homeUrl);
    }

    public void InitBrowser(string url)
    {
        Cef.Initialize(new CefSharp.WinForms.CefSettings());
        browser = new ChromiumWebBrowser(url)
        {
            BackColor = this.BackColor,
            Dock = DockStyle.None
        };
        browser.SetBounds(0, 0, this.Bounds.Width, this.Bounds.Height);
        //绑定：
        browser.FrameLoadEnd += webview_FrameLoadEnd;
        browser.ConsoleMessage += webview_ConsoleMessage;
        this.Controls.Add(browser);

        browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
        browser.JavascriptObjectRepository.Register("DsxKlineJSEvent", new DsxKlineJSEvent(this), options: BindingOptions.DefaultBinder);
    }

    private void CreateKline()
    {
        string js = "console.log('create kline js');" +
            "var keyCandles=[];" +
            "var c = document.getElementById(\"kline\");" +
            "dsxConfig.theme.white.klineWidth=" + klineWidth + ";" +
            "dsxConfig.theme.dark.klineWidth=" + klineWidth + ";" +
            "var kline = new dsxKline({" +
                "element:c," +
                "chartType:" + (int)chartType + "," +
                "theme:\"" + theme + "\"," +
                "candleType:" + (int)candleType + "," +
                "zoomLockType: " + (int)zoomLockType + "," +
                "isShowKlineTipPannel:" + (isShowKlineTipPannel ? "true" : "false") + "," +
                (lastClose > 0 ? "lastClose: " + lastClose + "," : "") +
                "sideHeight: " + sideHeight + "," +
                "paddingBottom: " + paddingBottom + "," +
                "autoSize: true," +
                "crossing: true," +
                "debug:" + (debug ? "true" : "false") + "," +
                "main:" + Newtonsoft.Json.JsonConvert.SerializeObject(main) + "," +
                "sides:" + Newtonsoft.Json.JsonConvert.SerializeObject(sides) + ", " +
                "onLoading: function(o){" +
                "    DsxKlineJSEvent.onLoading();" +
                "}," +
                "nextPage:function(data, index){" +
                "    DsxKlineJSEvent.nextPage();" +
                "}," +
                "onCrossing:function(data, index){" +
                "    DsxKlineJSEvent.onCrossing(JSON.stringify(data),index);" +
                "}," +
                "updateComplate: function(){" +
                "    DsxKlineJSEvent.updateComplate();" +
                "}," +
                "drawEvent:function(self){" +
                    "keyCandles.forEach((e) => {self.drawCircleWithDate(e, '▲', 'cyan', '#FFEA00');});" +
            "}});";
        try
        {
            browser.ExecuteScriptAsync(js);
        }
        catch (Exception e) { }

        //Console.WriteLine(js);
    }

    public List<CandleData> CandleSet { get; set; }

    public List<CandleData> KeyCandles => [.. CandleSet.Where(i => i.IsKey)];

    public int AnalysisRadiusPrevMax { get; set; } = 15;
    public int AnalysisRadiusPrevMin { get; set; } = 3;
    public int AnalysisRadiusNext { get; set; } = 5;

    public void Update(List<string> datas, int page)
    {
        if (browser == null) return;
        if (!browser.IsBrowserInitialized) return;
        string data = datas != null ? Newtonsoft.Json.JsonConvert.SerializeObject(datas) : "[]";
        this.Datas = datas;
        this.Page = page;

        CandleSet = Datas.Select(i =>
        {
            var item = i.Split(',');
            var r = item.Length switch
            {
                7 => new CandleData(item[0],
                                    null,
                                    float.TryParse(item[1], out var f) ? f : 0,
                                    float.TryParse(item[2], out f) ? f : 0,
                                    float.TryParse(item[3], out f) ? f : 0,
                                    float.TryParse(item[4], out f) ? f : 0,
                                    float.TryParse(item[5], out f) ? f : 0,
                                    float.TryParse(item[6], out f) ? f : 0),
                8 => new CandleData(item[0],
                                    item[1],
                                    float.TryParse(item[2], out var f) ? f : 0,
                                    float.TryParse(item[3], out f) ? f : 0,
                                    float.TryParse(item[4], out f) ? f : 0,
                                    float.TryParse(item[5], out f) ? f : 0,
                                    float.TryParse(item[6], out f) ? f : 0,
                                    float.TryParse(item[7], out f) ? f : 0),
                _ => null,
            };
            return r;
        }).Where(i => i != null).ToList();




        var safeRadius = Math.Max(AnalysisRadiusPrevMax, AnalysisRadiusNext);
        if (CandleSet.Count > safeRadius * 2)
            for (int k = CandleSet.Count - AnalysisRadiusNext - 1; k > AnalysisRadiusPrevMax; k--)
            {
                var item = CandleSet[k];

                int p = 0;
                for (p = k - 1; k - p < AnalysisRadiusPrevMax; p--)
                {
                    var candleP = CandleSet[p];
                    if (k - p > AnalysisRadiusPrevMin)       //在配置的前溯区间外
                    {
                        if (candleP.Close > item.Close)      //收盘价高于当前
                            if (candleP.IsRed)               //红盘日
                                break;
                    }
                    else                                    //在配置的前溯区间内
                    {
                        if (candleP.Close > item.Close)
                        {
                            p = 0;   //前溯最小区间内收盘价高于当前，放弃
                            break;
                        }
                    }
                }
                int prevCloseHigh = p > 0 ? p + 1 : 0;          //前溯区间内收盘价高于当前的红盘日位置（不包含）,或没有找到
                if (prevCloseHigh == 0) continue;           //前溯区间内没有找到收盘价高于当前的红盘日

                CandleData[] prevCandles = TakeCandles(k, -1 * prevCloseHigh);
                CandleData[] nextCandles = TakeCandles(k, AnalysisRadiusNext);
                CandleData[] allCandles = [.. prevCandles, CandleSet[k], .. nextCandles];
                var maxVol = allCandles.Max(i => i.Vol);
                var maxGain = allCandles.Max(i => i.Gain);
                var avgVolPrev = prevCandles.Average(i => i.Vol);
                var avgGainPrev = prevCandles.Average(i => i.Gain);

                if (item.IsRed //当日红盘
                    && item.Vol == maxVol   //局域最大成交量
                    && item.Gain == maxGain //局域最大涨幅
                    && item.Vol > avgVolPrev * 2  //成交量是前5日均量2倍以上
                    && item.Gain > avgGainPrev * 1.5 //涨幅是前5日均涨幅1.5倍以上
                   )
                {
                    item.Score = 0;
                    for (int n = 1; n <= 5; n++)
                    {
                        var nextN = CandleSet[k + n];       //下n日K线数据
                        if (nextN.IsGreen
                            && nextN.Close < item.Close     //收盘价低于当前 
                            && nextN.Close > item.Open)     //收盘价高于当前开盘价
                        {
                            item.Score++;
                            if (nextN.Low > item.Low) item.Score += 0.5f;
                            if (nextN.Low > item.Close) item.Score += 0.5f;
                        }
                        else if (n == 1) break;
                    }
                    item.IsKey = item.Score > 5;
                    if (item.IsKey) Debugger.Break();
                }
            }

        string js = "if(kline){" +
            "kline.update({" +
            $"datas:{data}," +
            $"page:'{page}'," +
            $"chartType:{(int)chartType}," +
            (lastClose > 0 ? $"lastClose: {lastClose}," : "") +
            $"main:{Newtonsoft.Json.JsonConvert.SerializeObject(main)}," +
            $"sides:{Newtonsoft.Json.JsonConvert.SerializeObject(sides)}," +
        "})};" +
        $"keyCandles={Newtonsoft.Json.JsonConvert.SerializeObject(KeyCandles.Select(i => i.Date + i.Time).ToArray())};";
        browser.ExecuteScriptAsync(js);
    }

    private CandleData[] TakeCandles(int i, int count)
    {
        CandleData[] neighbours = null;

        if (count != 0)
        {
            var abs = Math.Abs(count);
            neighbours = new CandleData[Math.Abs(abs)];
            var sign = count / abs;
            if ((sign > 0 & sign * abs + i < CandleSet.Count) || (sign < 0 & sign * abs + i >= 0))
                for (int n = 1; n <= abs; n++)
                {
                    var n1 = sign * n;
                    neighbours[n - 1] = CandleSet[i + n1];
                }
        }

        return neighbours;
    }

    /**
     * 加载数据前调用
     * @throws JSONException
     */

    public void StartLoading()
    {
        if (browser == null) return;
        if (!browser.IsBrowserInitialized) return;
        string js = $"kline.chartType={(int)chartType};kline.startLoading();";
        browser.ExecuteScriptAsync(js);
    }

    /**
     * 更新完K线图后调用
     */

    public void FinishLoading()
    {
        string js = "kline.finishLoading();";
        browser.ExecuteScriptAsync(js);
    }

    /// <summary>
    /// 刷新最后一个K线
    /// </summary>
    /// <param name="lastData"></param>
    /// <param name="cycle">t,t5,d,w,m,m1,m5,m30</param>
    public void RefreshLastOneData(string item, string cycle)
    {
        string js = $"kline.refreshLastOneData('{item}','{cycle}');";
        browser.ExecuteScriptAsync(js);
    }

    private void webview_FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
    {
        CreateKline();
    }

    private void webview_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
    {
        Console.WriteLine(e.Message);
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        //
        // DsxKline
        //
        this.SizeChanged += new System.EventHandler(this.DsxKline_SizeChanged);
        this.ResumeLayout(false);
    }

    private void DsxKline_SizeChanged(object sender, EventArgs e)
    {
        browser.SetBounds(0, 0, this.Bounds.Width, this.Bounds.Height);
        Update(Datas, Page);
    }

    private class DsxKlineJSEvent
    {
        private DsxKline dsxkline;

        public DsxKlineJSEvent(DsxKline dsx)
        {
            dsxkline = dsx;
        }

        public void onLoading()
        {
            dsxkline.onLoading?.Invoke();
        }

        public void nextPage()
        {
            dsxkline.nextPage?.Invoke();
        }

        public void onCrossing(string data, int index)
        {
            dsxkline.onCrossing?.Invoke(data, index);
        }

        public void updateComplate()
        {
            dsxkline.updateComplate?.Invoke();
        }
    }
}

