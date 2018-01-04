import pandas as pd

full = pd.read_csv('../input/marketdata.export.20180104131044.csv')

# Split the data into train and test
# Rough 70:30 split
train = full[0:2500]
test = full[2500:]

# Save it to disk
train.to_csv('../input/train.csv', encoding='utf-8')
test.to_csv('../input/test.csv', encoding='utf-8')

# Save the test as json too
cols = ['Close', 'Volume', 'Mva10', 'Mva200', 'Rsi2', 'Rsi14', 'PercentB', 'Bandwidth']
test[cols].to_json('../input/test.json', orient='records', lines=True)
