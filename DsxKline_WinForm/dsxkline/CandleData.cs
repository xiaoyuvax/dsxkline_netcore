namespace DsxKline_WinForm.dsxkline;

public class CandleData
{
    public CandleData(string date, string time, float open, float high, float low, float close, float vol, float amount)
    {
        Date = date;
        Time = time;
        Open = open;
        Close = close;
        High = high;
        Low = low;
        Vol = vol;
        Amount = amount;
    }

    public float Amount { get; set; }
    public float Amp => High - Low;
    public float Close { get; set; }
    public string Date { get; set; }

    public float Gain => Close - Open;
    public float High { get; set; }
    public bool IsGreen => Close <= Open;
    public bool IsKey { get; set; }
    public bool IsRed => Close > Open;
    public float Low { get; set; }
    public float Open { get; set; }
    public float Score { get; set; }
    public string Time { get; set; }
    public float Vol { get; set; }


    public float BodyTop { get; set; }
    public float BodyBottom { get; set; }
    public float UpperShadow { get; set; }
    public float LowerShadow { get; set; }
}