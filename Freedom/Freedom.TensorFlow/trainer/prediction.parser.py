reader = open('../output/output.model-13.txt')
writer = open('../output/output.model-13-results.txt', 'w')

writer.write('Action\n')

header = reader.readline()
line = reader.readline()
i = 0
actions = []
lastAction = 'S'
while line:  # and i < 900:
    prob = line[20:].strip()
    prob = prob[1:-1].split(', ')
    if lastAction == 'S':
        action = 'B' if max(prob) == prob[0] else 'H'
    else:
        action = 'S' if max(prob) == prob[2] else 'H'

    if action == 'B' or action == 'S':
        lastAction = action

    actions.append(action)

    writer.write(action + '\n')
    line = reader.readline()
    i += 1

writer.write("B: " + str(sum(1 for a in actions if a == 'B')) + '\n')
writer.write("S: " + str(sum(1 for a in actions if a == 'S')) + '\n')

reader.close()
writer.close()
