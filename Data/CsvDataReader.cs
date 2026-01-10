/*
 * Custom CSV Data Readers
 * Reusable data classes for reading CSV data in Lean algorithms
 */

using QuantConnect;
using QuantConnect.Data;
using System;
using System.Globalization;

namespace Bot.Data;

/// <summary>
/// Custom data type for IGE data from epchan dataset.
/// Filters data to only include dates within the backtest range.
/// </summary>
public class IGEData : BaseData
{
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal AdjClose { get; set; }

    /// <summary>
    /// Return the URL string source of the file. This will be converted to a stream
    /// </summary>
    public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
    {
        // Point to the local CSV file
        var source = "Data/epchan/IGE.lean.csv";
        return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
    }

    /// <summary>
    /// Reader converts each line of the data source into BaseData objects.
    /// Only returns data within the subscription's date range.
    /// </summary>
    public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
    {
        // Skip header line or empty lines
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Date"))
        {
            return null;
        }

        try
        {
            // Parse CSV line
            // Format: "Date","Open","High","Low","Close","Volume","Adj Close"
            // Example: "20011126 00:00:00","91.01","91.01","91.01","91.01","0","42.09"
            var csv = line.Split(',');
            
            // Remove quotes from date string and parse
            var dateString = csv[0].Trim('"');
            var parsedDate = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);

            var data = new IGEData
            {
                Symbol = config.Symbol,
                Time = parsedDate,
                Open = decimal.Parse(csv[1].Trim('"')),
                High = decimal.Parse(csv[2].Trim('"')),
                Low = decimal.Parse(csv[3].Trim('"')),
                Close = decimal.Parse(csv[4].Trim('"')),
                Volume = decimal.Parse(csv[5].Trim('"')),
                AdjClose = decimal.Parse(csv[6].Trim('"'))
            };

            // Use adjusted close as the value
            data.Value = data.AdjClose;

            return data;
        }
        catch (Exception ex)
        {
            // Return null if parsing fails
            return null;
        }
    }
}

/// <summary>
/// Custom data type for GLD data from epchan dataset.
/// Filters data to only include dates within the backtest range.
/// </summary>
public class GLDData : BaseData
{
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal AdjClose { get; set; }

    /// <summary>
    /// Return the URL string source of the file. This will be converted to a stream
    /// </summary>
    public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
    {
        // Point to the local CSV file
        var source = "Data/epchan/GLD.lean.csv";
        return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
    }

    /// <summary>
    /// Reader converts each line of the data source into BaseData objects.
    /// Only returns data within the subscription's date range.
    /// </summary>
    public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
    {
        // Skip header line or empty lines
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Date"))
        {
            return null;
        }

        try
        {
            // Parse CSV line
            // Format: "Date","Open","High","Low","Close","Volume","Adj Close"
            var csv = line.Split(',');
            
            // Remove quotes from date string and parse
            var dateString = csv[0].Trim('"');
            var parsedDate = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);

            var data = new GLDData
            {
                Symbol = config.Symbol,
                Time = parsedDate,
                Open = decimal.Parse(csv[1].Trim('"')),
                High = decimal.Parse(csv[2].Trim('"')),
                Low = decimal.Parse(csv[3].Trim('"')),
                Close = decimal.Parse(csv[4].Trim('"')),
                Volume = decimal.Parse(csv[5].Trim('"')),
                AdjClose = decimal.Parse(csv[6].Trim('"'))
            };

            // Use adjusted close as the value
            data.Value = data.AdjClose;

            return data;
        }
        catch (Exception ex)
        {
            // Return null if parsing fails
            return null;
        }
    }
}

/// <summary>
/// Custom data type for GDX data from epchan dataset.
/// Filters data to only include dates within the backtest range.
/// </summary>
public class GDXData : BaseData
{
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal AdjClose { get; set; }

    /// <summary>
    /// Return the URL string source of the file. This will be converted to a stream
    /// </summary>
    public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
    {
        // Point to the local CSV file
        var source = "Data/epchan/GDX.lean.csv";
        return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
    }

    /// <summary>
    /// Reader converts each line of the data source into BaseData objects.
    /// Only returns data within the subscription's date range.
    /// </summary>
    public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
    {
        // Skip header line or empty lines
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Date"))
        {
            return null;
        }

        try
        {
            // Parse CSV line
            // Format: "Date","Open","High","Low","Close","Volume","Adj Close"
            var csv = line.Split(',');
            
            // Remove quotes from date string and parse
            var dateString = csv[0].Trim('"');
            var parsedDate = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);

            var data = new GDXData
            {
                Symbol = config.Symbol,
                Time = parsedDate,
                Open = decimal.Parse(csv[1].Trim('"')),
                High = decimal.Parse(csv[2].Trim('"')),
                Low = decimal.Parse(csv[3].Trim('"')),
                Close = decimal.Parse(csv[4].Trim('"')),
                Volume = decimal.Parse(csv[5].Trim('"')),
                AdjClose = decimal.Parse(csv[6].Trim('"'))
            };

            // Use adjusted close as the value
            data.Value = data.AdjClose;

            return data;
        }
        catch (Exception ex)
        {
            // Return null if parsing fails
            return null;
        }
    }
}
/// <summary>
/// Custom data type for SPY data from epchan dataset.
/// Filters data to only include dates within the backtest range.
/// </summary>
public class SPYData : BaseData
{
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal AdjClose { get; set; }

    /// <summary>
    /// Return the URL string source of the file. This will be converted to a stream
    /// </summary>
    public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
    {
        // Point to the local CSV file
        var source = "Data/epchan/SPY.lean.csv";
        return new SubscriptionDataSource(source, SubscriptionTransportMedium.LocalFile);
    }

    /// <summary>
    /// Reader converts each line of the data source into BaseData objects.
    /// Only returns data within the subscription's date range.
    /// </summary>
    public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
    {
        // Skip header line or empty lines
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Date"))
        {
            return null;
        }

        try
        {
            // Parse CSV line
            // Format: "Date","Open","High","Low","Close","Volume","Adj Close"
            // Example: "20011126 00:00:00","91.01","91.01","91.01","91.01","0","42.09"
            var csv = line.Split(',');
            
            // Remove quotes from date string and parse
            var dateString = csv[0].Trim('"');
            var parsedDate = DateTime.ParseExact(dateString, "yyyyMMdd HH:mm:ss", CultureInfo.InvariantCulture);

            var data = new SPYData
            {
                Symbol = config.Symbol,
                Time = parsedDate,
                Open = decimal.Parse(csv[1].Trim('"')),
                High = decimal.Parse(csv[2].Trim('"')),
                Low = decimal.Parse(csv[3].Trim('"')),
                Close = decimal.Parse(csv[4].Trim('"')),
                Volume = decimal.Parse(csv[5].Trim('"')),
                AdjClose = decimal.Parse(csv[6].Trim('"'))
            };

            // Use adjusted close as the value
            data.Value = data.AdjClose;

            return data;
        }
        catch (Exception ex)
        {
            // Return null if parsing fails
            return null;
        }
    }
}
