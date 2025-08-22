using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace DsxKline_WinForm.dsxkline
{
    class QqHq
    {
        /**
         * 请求第三方实时行情
         * @param {string} code 股票代码
         * @param {object} success
         * @param {object} fail
         * 0: 未知 1: 名字 2: 代码 3: 当前价格 4: 昨收 5: 今开 6: 成交量（手） 7: 外盘 8: 内盘 9: 买一 10: 买一量（手） 11-18: 买二 买五 19: 卖一 20: 卖一量 21-28: 卖二 卖五 29: 最近逐笔成交 30: 时间 31: 涨跌 32: 涨跌% 33: 最高 34: 最低 35: 价格/成交量（手）/成交额 36: 成交量（手） 37: 成交额（万） 38: 换手率 39: 市盈率 40: 41: 最高 42: 最低 43: 振幅 44: 流通市值 45: 总市值 46: 市净率 47: 涨停价 48: 跌停价
         */
        public static List<HqModel> GetQuote(string code)
        {
            string api = "http://qt.gtimg.cn/q=" + code;
            string resultStart = "v_{code}=";
            string resultEnd = ";";
            string data = Get(api);
            //console.log(response);
            List<HqModel> list = null;
            if (data != null)
            {
                list = [];
                string[] dataList = data.Split(resultEnd.ToCharArray());
                string[] codes = code.Split(',');
                for (int i = 0; i < dataList.Length; i++)
                {
                    string item = dataList[i];
                    string rss = item.Replace(resultStart.Replace("{code}", code.ToLower()), "");
                    string[] rs = rss.Split('~');
                    if (rs[1] != "")
                    {
                        HqModel obj = new()
                        {
                            name = rs[1],
                            code = codes[i],
                            price = rs[3],
                            lastClose = rs[4],
                            open = rs[5],
                            high = rs[33],
                            low = rs[34],
                            vol = rs[6],
                            volAmount = rs[37],
                            date = rs[30][..8],
                            time = rs[30][8..],
                            change = rs[31],
                            changeRatio = rs[32]
                        };

                        list.Add(obj);
                        //console.log(obj);
                        i++;
                    }
                }
            }
            return list;
        }

        public static List<string> GetTimeLine(string code)
        {
            string api = $"https://web.ifzq.gtimg.cn/appstock/app/minute/query?_var=min_data_{code}&code={code}&r=0.{new DateTime().Millisecond}";
            if (code.StartsWith("us"))
            {
                api = $"https://web.ifzq.gtimg.cn/appstock/app/UsMinute/query?_var=min_data_{code}&code={code[..2]}.{code[2..]}&r=0.{new DateTime().Millisecond}";
                code = $"{code[..2]}.{code[2..]}";
            }
            List<string> list = null;
            string result = Get(api);
            if (result != null)
            {
                result = result.Replace($"min_data_{code.Replace(".", "")}=", "");
                Dictionary<string, dynamic> rss = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
                JArray data = rss["data"][code]["data"]["data"];
                string date = rss["data"][code]["data"]["date"];
                list = [];
                for (int i = 0; i < data.Count; i++)
                {
                    string item = data[i].ToString();
                    //string[] r = item.Split(' ');
                    string low = $"{date},{item.Replace(" ", ",")}";
                    list.Add(low);
                }
            }
            return list;
        }

        public static Dictionary<string, object> GetFdayLine(string code)
        {
            string api = $"https://web.ifzq.gtimg.cn/appstock/app/day/query?_var=fdays_data_{code}&code={code}&r=0.{new DateTime().Millisecond}";
            if (code.StartsWith("us"))
            {
                api = $"https://web.ifzq.gtimg.cn/appstock/app/dayus/query?_var=fdays_data_{code}&code={code[..2]}.{code[2..]}&r=0.{new DateTime().Millisecond}";
                code = code[..2] + "." + code[2..];
            }
            Dictionary<string, dynamic> map = [];
            List<string> list = null;
            string result = Get(api);
            double prec = 0;
            if (result != null)
            {
                result = result.Replace("fdays_data_" + code.Replace(".", "") + "=", "");
                Dictionary<string, dynamic> rss = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
                JArray data = rss["data"][code]["data"];
                list = [];
                for (int i = data.Count - 1; i >= 0; i--)
                {
                    Dictionary<string, dynamic> item = data[i].ToObject<Dictionary<string, dynamic>>();
                    string date = item["date"];
                    prec = double.Parse(item["prec"]);
                    JArray d = item["data"];
                    for (int j = 0; j < d.Count; j++)
                    {
                        string subitem = d[j].ToString();
                        string low = date + "," + subitem.Replace(" ", ",");
                        list.Add(low);
                    }
                }
            }
            map.Add("lastClose", prec);
            map.Add("data", list);
            return map;
        }

        /**
         * 获取K线图历史数据
         * @param {string} code 股票代码
         * @param {string} cycle 周期 day，week，month
         * @param {string} startDate 开始日期 默认 空
         * @param {string} endDate 结束日期 默认空
         * @param {int} pageSize 每页大小 默认 320
         * @param {string} fqType 复权类型 前复权=qfq，后复权=hfq
         * @param {*} success
         * @param {*} fail
         */
        public static List<string> GetKLine(string code, string cycle, string startDate, string endDate, int pageSize, string fqType)
        {
            string api = $"https://proxy.finance.qq.com/ifzqgtimg/appstock/app/newfqkline/get?_var=kline_{cycle}{fqType}&param={code},{cycle},{startDate},{endDate},{pageSize},{fqType}&r=0.36592503777267116{new DateTime().Millisecond}";
            if (code.StartsWith("us"))
            {
                code = code[..2] + "." + code[2..];
                api = $"https://proxy.finance.qq.com/ifzqgtimg/appstock/app/newfqkline/get?_var=kline_{cycle}{fqType}&param={code},{cycle},{startDate},{endDate},{pageSize},{fqType}&r=0.36592503777267116{new DateTime().Millisecond}";
            }

            List<string> list = null;
            string result = Get(api);
            if (result != null)
            {
                result = result.Replace($"kline_{cycle}{fqType}=", "");
                Dictionary<string, dynamic> rss = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
                JArray data = rss["data"][code][cycle];
                list = [];
                for (int i = 0; i < data.Count; i++)
                {
                    JToken[] d = [.. data[i]];
                    string r = $"{d[0]},{d[1]},{d[3]},{d[4]},{d[2]},{d[5]},{d[8]}";
                    r = r.Replace("-", "");
                    list.Add(r);
                }
            }
            return list;
        }

        public static List<string> GetMinLine(string code, string cycle, int pageSize)
        {

            if (code.StartsWith("us"))
            {
                code = code[..2] + "." + code[2..];
            }
            string api = $"https://ifzq.gtimg.cn/appstock/app/kline/mkline?param={code},{cycle},,{pageSize}&_var={cycle}_today&r=0.36592503777267116{new DateTime().Millisecond}";

            List<string> list = null;
            string result = Get(api);
            if (result != null)
            {
                result = result.Replace($"{cycle}_today=", "");
                Dictionary<string, dynamic> rss = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
                JArray data = rss["data"][code][cycle];
                list = [];
                for (int i = 0; i < data.Count; i++)
                {
                    JToken[] d = [.. data[i]];
                    string date = d[0].ToString()[..8];
                    string time = d[0].ToString()[8..];
                    string r = $"{date},{time},{d[1]},{d[3]},{d[4]},{d[2]},{d[5]},{d[7]}";
                    r = r.Replace("-", "");
                    list.Add(r);
                }
            }
            return list;
        }



        public static string Get(string url)
        {
            try
            {
                string result = "";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 20000;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                //结果
                using StreamReader reader = new(stream, Encoding.UTF8);
                result = reader.ReadToEnd();
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GET请求错误：{ex}");
                return null;
            }
        }
    }
}
