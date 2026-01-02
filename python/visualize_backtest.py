"""
Simple backtest visualization script for Lean results
Generates an HTML report with equity curve, drawdown, and key statistics

Usage:
    python visualize_backtest.py                    # Visualize most recent backtest
    python visualize_backtest.py <folder_name>      # Visualize specific backtest folder
"""

import json
import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.dates as mdates
from datetime import datetime
from base64 import b64encode
import io
import sys
import os
from pathlib import Path

def load_results(json_file):
    """Load backtest results from JSON file"""
    with open(json_file, 'r') as f:
        return json.load(f)

def load_order_events(folder_path):
    """Load order events (trades) from order-events.json file"""
    folder = Path(folder_path)
    
    # Extract strategy name from folder name
    folder_name = folder.name
    import re
    strategy_name = re.sub(r'-\d{8}-\d{6}$', '', folder_name)
    
    # Look for {StrategyName}-order-events.json
    order_events_file = folder / f"{strategy_name}-order-events.json"
    
    if not order_events_file.exists():
        # Fallback: look for any order-events.json file
        order_events_files = list(folder.glob("*-order-events.json"))
        if not order_events_files:
            print(f"⚠️  Warning: No order events file found")
            return []
        order_events_file = order_events_files[0]
    
    try:
        with open(order_events_file, 'r') as f:
            events = json.load(f)
            # Filter to only filled orders (actual trades)
            trades = [e for e in events if e.get('status') == 'filled']
            return trades
    except Exception as e:
        print(f"⚠️  Warning: Could not load order events: {e}")
        return []

def extract_equity_series(data):
    """Extract equity time series from charts data"""
    equity_data = data['charts']['Strategy Equity']['series']['Equity']['values']
    
    # Extract timestamps and equity values (using close price)
    df = pd.DataFrame(equity_data, columns=['timestamp', 'open', 'high', 'low', 'close'])
    df['date'] = pd.to_datetime(df['timestamp'], unit='s')
    df = df[['date', 'close']]
    df.columns = ['date', 'equity']
    df.set_index('date', inplace=True)
    
    return df

def extract_drawdown_series(data):
    """Extract drawdown time series from charts data"""
    drawdown_data = data['charts']['Drawdown']['series']['Equity Drawdown']['values']
    
    df = pd.DataFrame(drawdown_data, columns=['timestamp', 'drawdown'])
    df['date'] = pd.to_datetime(df['timestamp'], unit='s')
    df.set_index('date', inplace=True)
    
    return df

def fig_to_base64(fig):
    """Convert matplotlib figure to base64 string"""
    buf = io.BytesIO()
    fig.savefig(buf, format='png', dpi=150, bbox_inches='tight')
    buf.seek(0)
    img_str = b64encode(buf.read()).decode('utf-8')
    buf.close()
    return f'data:image/png;base64,{img_str}'

def create_equity_chart(equity_df):
    """Create equity curve chart"""
    fig, ax = plt.subplots(figsize=(12, 6))
    
    ax.plot(equity_df.index, equity_df['equity'], color='#ff9914', linewidth=2, label='Portfolio Value')
    ax.fill_between(equity_df.index, equity_df['equity'], alpha=0.3, color='#ff9914')
    
    ax.set_title('Equity Curve', fontsize=16, fontweight='bold', pad=20)
    ax.set_xlabel('Date', fontsize=12)
    ax.set_ylabel('Portfolio Value ($)', fontsize=12)
    ax.grid(True, alpha=0.3, linestyle='--')
    ax.legend(loc='upper left')
    
    # Format y-axis as currency
    ax.yaxis.set_major_formatter(plt.FuncFormatter(lambda x, p: f'${x:,.0f}'))
    
    # Format x-axis dates
    ax.xaxis.set_major_formatter(mdates.DateFormatter('%Y-%m-%d'))
    plt.xticks(rotation=45)
    
    plt.tight_layout()
    return fig

def create_drawdown_chart(drawdown_df):
    """Create drawdown chart"""
    fig, ax = plt.subplots(figsize=(12, 4))
    
    ax.fill_between(drawdown_df.index, drawdown_df['drawdown'], 0, 
                     color='#e74c3c', alpha=0.6, label='Drawdown')
    ax.plot(drawdown_df.index, drawdown_df['drawdown'], color='#c0392b', linewidth=1.5)
    
    ax.set_title('Drawdown', fontsize=16, fontweight='bold', pad=20)
    ax.set_xlabel('Date', fontsize=12)
    ax.set_ylabel('Drawdown (%)', fontsize=12)
    ax.grid(True, alpha=0.3, linestyle='--')
    ax.legend(loc='lower left')
    
    # Format x-axis dates
    ax.xaxis.set_major_formatter(mdates.DateFormatter('%Y-%m-%d'))
    plt.xticks(rotation=45)
    
    plt.tight_layout()
    return fig

def format_stat_value(key, value):
    """Format statistics values for display"""
    # Percentage metrics
    if any(x in key.lower() for x in ['rate', 'return', 'ratio', 'drawdown', 'variance', 'turnover', 'deviation']):
        try:
            return f"{float(value):.2%}" if value else "0.00%"
        except:
            return value
    
    # Currency values
    if any(x in key.lower() for x in ['equity', 'fees', 'capacity']):
        try:
            return f"${float(value):,.2f}"
        except:
            return value
    
    # Regular numbers
    try:
        num = float(value)
        if abs(num) < 100:
            return f"{num:.4f}"
        else:
            return f"{num:,.2f}"
    except:
        return value

def generate_html_report(data, equity_chart_b64, drawdown_chart_b64, trades, output_file, results_folder=None):
    """Generate HTML report with charts and statistics"""
    stats = data['statistics']
    algo_config = data['algorithmConfiguration']
    
    # Extract strategy name from multiple possible sources (in priority order)
    strategy_name = None
    
    # First, check if there's a strategy-name in the raw data (custom property we might have added)
    if 'strategyName' in data:
        strategy_name = data['strategyName']
    elif 'strategy-name' in data:
        strategy_name = data['strategy-name']
    
    # If not found and we have a results folder, extract from folder name
    if not strategy_name and results_folder:
        folder = Path(results_folder)
        folder_name = folder.name
        # Extract strategy name (everything before the timestamp pattern)
        import re
        strategy_name = re.sub(r'-\d{8}-\d{6}$', '', folder_name)
    
    # Fallback to algorithmConfiguration name
    if not strategy_name:
        strategy_name = algo_config.get('name', 'N/A')
    
    # Extract run timestamp from folder name if available
    run_time_str = "N/A"
    if results_folder:
        folder = Path(results_folder)
        folder_name = folder.name
        # Extract timestamp from folder name (e.g., "BuyAndHoldXOM-20260101-220149")
        import re
        match = re.search(r'-(\d{8})-(\d{6})$', folder_name)
        if match:
            date_str = match.group(1)
            time_str = match.group(2)
            try:
                dt = datetime.strptime(f"{date_str}{time_str}", "%Y%m%d%H%M%S")
                run_time_str = dt.strftime("%Y-%m-%d %H:%M:%S")
            except:
                pass
    
    # Organize stats into categories
    performance_stats = {}
    risk_stats = {}
    trade_stats = {}
    
    for key, value in stats.items():
        key_lower = key.lower()
        if any(x in key_lower for x in ['return', 'profit', 'alpha', 'beta', 'information']):
            performance_stats[key] = format_stat_value(key, value)
        elif any(x in key_lower for x in ['sharpe', 'sortino', 'drawdown', 'variance', 'deviation', 'risk']):
            risk_stats[key] = format_stat_value(key, value)
        else:
            trade_stats[key] = format_stat_value(key, value)
    
    # Generate trades table HTML
    trades_html = ""
    if trades:
        trades_html = """
        <h2>📋 All Trades</h2>
        <table>
            <thead>
                <tr>
                    <th>Order ID</th>
                    <th>Time</th>
                    <th>Symbol</th>
                    <th>Direction</th>
                    <th>Quantity</th>
                    <th>Fill Price</th>
                    <th>Total Value</th>
                    <th>Fee</th>
                </tr>
            </thead>
            <tbody>
"""
        for trade in trades:
            trade_time = datetime.fromtimestamp(trade['time']).strftime('%Y-%m-%d %H:%M:%S')
            symbol = trade.get('symbolValue', trade.get('symbol', 'N/A'))
            direction = trade['direction'].upper()
            quantity = trade['fillQuantity']
            fill_price = trade['fillPrice']
            total_value = abs(quantity * fill_price)
            fee = trade.get('orderFeeAmount', 0)
            order_id = trade['orderId']
            
            direction_class = 'buy' if direction == 'BUY' else 'sell'
            
            trades_html += f"""
                <tr class="{direction_class}">
                    <td>{order_id}</td>
                    <td>{trade_time}</td>
                    <td>{symbol}</td>
                    <td><strong>{direction}</strong></td>
                    <td>{quantity:,.0f}</td>
                    <td>${fill_price:,.2f}</td>
                    <td>${total_value:,.2f}</td>
                    <td>${fee:,.2f}</td>
                </tr>
"""
        
        trades_html += """
            </tbody>
        </table>
"""
    else:
        trades_html = """
        <h2>📋 All Trades</h2>
        <p style="color: #7f8c8d; text-align: center; padding: 20px;">No trade data available</p>
"""
    
    html_content = f"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Backtest Report - {strategy_name}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{
            max-width: 1400px;
            margin: 0 auto;
            background-color: white;
            padding: 30px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            border-radius: 8px;
        }}
        h1 {{
            color: #2c3e50;
            border-bottom: 3px solid #ff9914;
            padding-bottom: 10px;
            margin-bottom: 30px;
        }}
        h2 {{
            color: #34495e;
            margin-top: 40px;
            margin-bottom: 20px;
            border-left: 4px solid #ff9914;
            padding-left: 15px;
        }}
        .info-box {{
            background-color: #ecf0f1;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 30px;
        }}
        .info-box p {{
            margin: 5px 0;
            color: #555;
        }}
        .chart-container {{
            margin: 30px 0;
            text-align: center;
        }}
        .chart-container img {{
            max-width: 100%;
            height: auto;
            border: 1px solid #ddd;
            border-radius: 5px;
        }}
        .stats-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
            gap: 20px;
            margin: 20px 0;
        }}
        .stat-card {{
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 5px;
            border-left: 4px solid #ff9914;
        }}
        .stat-card h3 {{
            margin-top: 0;
            color: #2c3e50;
            font-size: 16px;
            margin-bottom: 15px;
        }}
        .stat-item {{
            display: flex;
            justify-content: space-between;
            padding: 8px 0;
            border-bottom: 1px solid #e0e0e0;
        }}
        .stat-item:last-child {{
            border-bottom: none;
        }}
        .stat-label {{
            color: #555;
            font-weight: 500;
        }}
        .stat-value {{
            color: #2c3e50;
            font-weight: 600;
        }}
        .highlight {{
            font-size: 24px;
            color: #27ae60;
        }}
        .highlight.negative {{
            color: #e74c3c;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        th, td {{
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #ddd;
        }}
        th {{
            background-color: #34495e;
            color: white;
            font-weight: 600;
        }}
        tr:hover {{
            background-color: #f5f5f5;
        }}
        tr.buy {{
            border-left: 3px solid #27ae60;
        }}
        tr.sell {{
            border-left: 3px solid #e74c3c;
        }}
        .footer {{
            margin-top: 50px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            text-align: center;
            color: #7f8c8d;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class="container">
        <h1>📊 Backtest Report</h1>
        
        <div class="info-box">
            <p><strong>Strategy:</strong> {strategy_name}</p>
            <p><strong>Period:</strong> 2016-01-02 to 2021-01-02</p>
            <p><strong>Run Time:</strong> {run_time_str}</p>
            <p><strong>Status:</strong> Completed</p>
        </div>
        
        <h2>📈 Equity Curve</h2>
        <div class="chart-container">
            <img src="{equity_chart_b64}" alt="Equity Curve">
        </div>
        
        <h2>📉 Drawdown</h2>
        <div class="chart-container">
            <img src="{drawdown_chart_b64}" alt="Drawdown">
        </div>
        
        <h2>📊 Key Performance Metrics</h2>
        <div class="stats-grid">
            <div class="stat-card">
                <h3>Performance Statistics</h3>
                {''.join([f'<div class="stat-item"><span class="stat-label">{k}</span><span class="stat-value">{v}</span></div>' for k, v in performance_stats.items()])}
            </div>
            
            <div class="stat-card">
                <h3>Risk Statistics</h3>
                {''.join([f'<div class="stat-item"><span class="stat-label">{k}</span><span class="stat-value">{v}</span></div>' for k, v in risk_stats.items()])}
            </div>
            
            <div class="stat-card">
                <h3>Trade Statistics</h3>
                {''.join([f'<div class="stat-item"><span class="stat-label">{k}</span><span class="stat-value">{v}</span></div>' for k, v in trade_stats.items()])}
            </div>
        </div>
        
        <h2>📋 All Statistics</h2>
        <table>
            <thead>
                <tr>
                    <th>Metric</th>
                    <th>Value</th>
                </tr>
            </thead>
            <tbody>
                {''.join([f'<tr><td>{k}</td><td>{v}</td></tr>' for k, v in stats.items()])}
            </tbody>
        </table>
        
        {trades_html}
        
        <div class="footer">
            <p>Generated with Lean Algorithmic Trading Engine | {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
        </div>
    </div>
</body>
</html>
"""
    
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(html_content)
    
    print(f"Report generated successfully: {output_file}")

def get_most_recent_results_folder(results_dir="Results"):
    """Find the most recent backtest folder based on timestamp"""
    results_path = Path(results_dir)
    
    if not results_path.exists():
        raise FileNotFoundError(f"Results directory not found: {results_dir}")
    
    # Get all subdirectories
    folders = [f for f in results_path.iterdir() if f.is_dir()]
    
    if not folders:
        raise FileNotFoundError(f"No backtest folders found in {results_dir}")
    
    # Sort by modification time (most recent first)
    folders.sort(key=lambda x: x.stat().st_mtime, reverse=True)
    
    return folders[0]

def find_json_file(folder_path):
    """Find the JSON results file in the folder"""
    folder = Path(folder_path)
    
    # Extract strategy name from folder name (e.g., "BuyAndHoldXOM-20260101-220149" -> "BuyAndHoldXOM")
    folder_name = folder.name
    # Remove timestamp pattern (anything after last occurrence of -{date})
    import re
    strategy_name = re.sub(r'-\d{8}-\d{6}$', '', folder_name)
    
    # Look for {StrategyName}.json
    json_file = folder / f"{strategy_name}.json"
    
    if json_file.exists():
        return json_file
    
    # Fallback: look for any .json file
    json_files = list(folder.glob("*.json"))
    
    if not json_files:
        raise FileNotFoundError(f"No JSON file found in {folder_path}")
    
    print(f"Warning: Expected {strategy_name}.json not found, using: {json_files[0].name}")
    return json_files[0]

def main():
    # Parse command-line arguments
    if len(sys.argv) > 1:
        # User specified a folder name
        folder_name = sys.argv[1]
        results_folder = Path("Results") / folder_name
        
        if not results_folder.exists():
            print(f"❌ Error: Folder not found: {results_folder}")
            print(f"\nAvailable folders in Results/:")
            results_path = Path("Results")
            if results_path.exists():
                for folder in sorted(results_path.iterdir()):
                    if folder.is_dir():
                        print(f"  - {folder.name}")
            sys.exit(1)
        
        print(f"📁 Using specified folder: {folder_name}")
    else:
        # Find most recent backtest
        print("🔍 Finding most recent backtest...")
        results_folder = get_most_recent_results_folder()
        print(f"📁 Found: {results_folder.name}")
    
    # Find JSON file in the folder
    try:
        json_file = find_json_file(results_folder)
        print(f"📄 Using results file: {json_file.name}")
    except FileNotFoundError as e:
        print(f"❌ Error: {e}")
        sys.exit(1)
    
    # Set output file in the same folder
    output_file = results_folder / "report.html"
    
    print("Loading backtest results...")
    data = load_results(json_file)
    
    print("Extracting equity curve...")
    equity_df = extract_equity_series(data)
    
    print("Extracting drawdown data...")
    drawdown_df = extract_drawdown_series(data)
    
    print("Generating charts...")
    equity_fig = create_equity_chart(equity_df)
    equity_chart_b64 = fig_to_base64(equity_fig)
    plt.close(equity_fig)
    
    drawdown_fig = create_drawdown_chart(drawdown_df)
    drawdown_chart_b64 = fig_to_base64(drawdown_fig)
    plt.close(drawdown_fig)
    
    print("Loading trade data...")
    trades = load_order_events(results_folder)
    print(f"Found {len(trades)} trades")
    
    print("Generating HTML report...")
    generate_html_report(data, equity_chart_b64, drawdown_chart_b64, trades, output_file, results_folder)
    
    print("\nReport Summary:")
    print(f"  Start Equity: ${float(data['statistics']['Start Equity']):,.2f}")
    print(f"  End Equity: ${float(data['statistics']['End Equity']):,.2f}")
    print(f"  Net Profit: {data['statistics']['Net Profit']}")
    print(f"  Sharpe Ratio: {data['statistics']['Sharpe Ratio']}")
    print(f"  Max Drawdown: {data['statistics']['Drawdown']}")
    print(f"\n✅ Report saved to: {output_file}")
    print(f"   Open this file in your browser to view the full report!")

if __name__ == "__main__":
    main()
