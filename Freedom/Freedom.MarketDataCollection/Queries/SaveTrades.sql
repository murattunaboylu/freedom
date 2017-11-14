INSERT INTO dbo.Trades (Exchange, Market, TradeID, Price, Quantity, Total, [Time], [Type])
VALUES ('CEX.IO', 'BTC/EUR', '1000', 5806.00000000, 0.02182569, 0.02182569*5806.0, getdate(), 'Buy' )

--15:31:12 buy BTC/EUR 0.02182569 @ 5806.00000000 id:1480146
select * from Trades

--truncate table trades