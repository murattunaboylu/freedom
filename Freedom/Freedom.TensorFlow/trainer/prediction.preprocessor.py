def is_int(s):
    try:
        int(s)
        return True
    except ValueError:
        return False


reader = open('../data/test.csv')
writer = open('../data/test.json', 'w')

header = reader.readline()
line = reader.readline()

while line:
    col = line.rstrip().split(',')
    title = col[3].strip().split(' ')[0]
    pclass = col[1]
    sex = col[4]
    age = col[5] if col[5] != "" else 29.88
    sibsp = col[6]
    parch = col[7]
    fare = col[9] if col[9] != "" else 33.29
    cabin_class = col[10][:1]
    cabin_nos = col[10][1:]
    cabin_no = cabin_nos.split(' ')[0]
    cabin_no = cabin_no if is_int(cabin_no) else 0
    embarked = col[11]

    json = '{{"Pclass":{}, "Sex":"{}", "Age":{}, "SibSp":{}, "Parch":{}, "Fare":{}, "Embarked":"{}", ' \
           '"CabinClass":"{}", "CabinNo":{}, "Title":"{}"}}\n'\
        .format(pclass, sex, age, sibsp, parch, fare, embarked, cabin_class, cabin_no, title)

    writer.write(json)

    line = reader.readline()

reader.close()
writer.close()