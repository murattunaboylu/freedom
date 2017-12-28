reader = open('../output/output.parent.dnn.txt')
writer = open('../data/titanic.prediction.12.csv', 'w')

writer.write('PassengerId,Survived\n')

header = reader.readline()
line = reader.readline()
i = 892
while line:  # and i < 900:
    prob = line[13:].strip()
    prob = prob[1:-1].split(', ')
    survived = 1 if prob[1] > prob[0] else 0
    writer.write("{},{}\n".format(i, survived))
    line = reader.readline()
    i += 1

reader.close()
writer.close()
