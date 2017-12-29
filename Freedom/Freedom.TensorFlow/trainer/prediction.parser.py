reader = open('../output/output.model-4.txt')
writer = open('../output/output.model-4-results.txt', 'w')

writer.write('Action\n')

header = reader.readline()
line = reader.readline()
i = 0
last_action = 'S'
while line:  # and i < 900:
    prob = line[20:].strip()
    prob = prob[1:-1].split(', ')
    if last_action == 'S':
        action = 'B' if max(prob) == prob[0] else 'H'
    else:
        action = 'S' if max(prob) == prob[2] else 'H'

    if action != 'H':
        last_action = action

    writer.write(action + '\n')
    line = reader.readline()
    i += 1

reader.close()
writer.close()
