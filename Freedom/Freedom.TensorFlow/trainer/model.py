import tensorflow as tf

# Define the format of your input data including unused columns
CSV_COLUMNS = ['Id', 'Date', 'Open', 'High', 'Low', 'Close', 'Volume', 'Mva10', 'Mva200', 'Rsi2', 'Rsi14', 'PercentB',
               'Bandwidth']

CSV_COLUMN_DEFAULTS = [[0], [''], [0.], [0.], [0.], [0.], [0.], [0.], [0.], [0.], [0.], [.0], [.0]]
LABEL_COLUMN = 'Action'
LABELS = ['B', 'H', 'S']

# Define the initial ingestion of each feature used by your model.
# Additionally, provide metadata about the feature.
INPUT_COLUMNS = [
    # Categorical base columns

    # For categorical columns with known values we can provide lists
    # of values ahead of time.
    # tf.feature_column.categorical_column_with_vocabulary_list(
    #     'Pclass', [1, 2, 3]),
    # tf.feature_column.categorical_column_with_vocabulary_list(
    #     'Sex',
    #     ['male', 'female']),

    # Feature for cabin
    # Screening the values
    # cat data/train.data.csv | cut -d, -f12 | sort | uniq
    # tf.feature_column.categorical_column_with_vocabulary_list(
    #     'CabinClass',
    #     ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'T']),
    # tf.feature_column.categorical_column_with_vocabulary_list(
    #     'Embarked',
    #     ['S', 'C', ' Q']),
    # tf.feature_column.categorical_column_with_vocabulary_list(
    #     'Title',
    #     ['Mr.', 'Mrs.', 'Miss.', 'Master.', 'Dr.', 'Captain.', 'Mlle.', 'Col.', 'Rev.']),

    # Continuous base columns.
    tf.feature_column.numeric_column('Open'),
    tf.feature_column.numeric_column('High'),
    tf.feature_column.numeric_column('Low'),
    tf.feature_column.numeric_column('Close'),
    tf.feature_column.numeric_column('Volume'),
    tf.feature_column.numeric_column('Mva10'),
    tf.feature_column.numeric_column('Mva200'),
    tf.feature_column.numeric_column('Rsi2'),
    tf.feature_column.numeric_column('Rsi14'),
    tf.feature_column.numeric_column('PercentB'),
    tf.feature_column.numeric_column('Bandwidth'),
]

UNUSED_COLUMNS = set(CSV_COLUMNS) - {col.name for col in INPUT_COLUMNS} - {LABEL_COLUMN}


def build_estimator(config, embedding_size=8, hidden_units=None):
    """Build a wide and deep model for predicting survival rates of Titanic disaster.

    Wide and deep models use deep neural nets to learn high level abstractions
    about complex features or interactions between such features.
    These models then combined the outputs from the DNN with a linear regression
    performed on simpler features. This provides a balance between power and
    speed that is effective on many structured data problems.

    You can read more about wide and deep models here:
    https://research.googleblog.com/2016/06/wide-deep-learning-better-together-with.html

    To define model we can use the prebuilt DNNCombinedLinearClassifier class,
    and need only define the data transformations particular to our dataset, and
    then
    assign these (potentially) transformed features to either the DNN, or linear
    regression portion of the model.

    Args:
    config: tf.contrib.learn.RunConfig defining the runtime environment for the
      estimator (including model_dir).
    embedding_size: int, the number of dimensions used to represent categorical
      features when providing them as inputs to the DNN.
    hidden_units: [int], the layer sizes of the DNN (input layer first)
    learning_rate: float, the learning rate for the optimizer.
    Returns:
    A DNNCombinedLinearClassifier
    """
    (open, high, low, close, volume, mva10, mva200, rsi2, rsi14, percentB, bandwidth) = INPUT_COLUMNS
    """Build an estimator."""

    # Reused Transformations.
    # Continuous columns can be converted to categorical via bucketization
    # age_buckets = tf.feature_column.bucketized_column(
    #     age, boundaries=[10, 20, 30, 35, 40, 45, 50, 55, 60, 65])

    # Wide columns and deep columns.
    wide_columns = [
        # Interactions between different categorical features can also
        # be added as new virtual features.
        # tf.feature_column.crossed_column(
        #     ['Pclass', 'Embarked'], hash_bucket_size=int(1e4)),
        # tf.feature_column.crossed_column(
        #     [age_buckets, sex], hash_bucket_size=int(1e4)),
        # p_class,
        # sex,
        # embarked,
        # age_buckets,
        # cabin_class,
        # title
    ]

    deep_columns = [
        # Use indicator columns for low dimensional vocabularies
        # tf.feature_column.indicator_column(p_class),
        # tf.feature_column.indicator_column(sex),
        # tf.feature_column.indicator_column(embarked),
        # tf.feature_column.indicator_column(title),

        # Use embedding columns for high dimensional vocabularies
        # tf.feature_column.embedding_column(
        #    native_country, dimension=embedding_size),
        # tf.feature_column.embedding_column(occupation, dimension=embedding_size),
        open, high, low, close, volume, mva10, mva200, rsi2, rsi14, percentB, bandwidth
    ]

    '''return tf.contrib.learn.DNNLinearCombinedClassifier(
      config=config,
      linear_feature_columns=wide_columns,
      dnn_feature_columns=deep_columns,
      dnn_hidden_units=hidden_units or [50, 150, 100, 15],
      fix_global_step_increment_bug=True
    )'''

    return tf.contrib.learn.DNNClassifier(
        config=config,
        feature_columns=deep_columns,
        hidden_units=[1024, 512, 256],
        optimizer=tf.train.ProximalAdagradOptimizer(
            learning_rate=0.1,
            l1_regularization_strength=0.001
        )
    )


def parse_label_column(label_string_tensor):
    """Parses a string tensor into the label tensor
    Args:
    label_string_tensor: Tensor of dtype string. Result of parsing the
    CSV column specified by LABEL_COLUMN
    Returns:
    A Tensor of the same shape as label_string_tensor, should return
    an int64 Tensor representing the label index for classification tasks,
    and a float32 Tensor representing the value for a regression task.
    """
    # Build a Hash Table inside the graph
    table = tf.contrib.lookup.index_table_from_tensor(tf.constant(LABELS))

    # Use the hash table to convert string labels to ints and one-hot encode
    return table.lookup(label_string_tensor)


# ************************************************************************
# YOU NEED NOT MODIFY ANYTHING BELOW HERE TO ADAPT THIS MODEL TO YOUR DATA
# ************************************************************************


def csv_serving_input_fn():
    """Build the serving inputs."""
    csv_row = tf.placeholder(
        shape=[None],
        dtype=tf.string
    )
    features = parse_csv(csv_row)
    features.pop(LABEL_COLUMN)
    return tf.contrib.learn.InputFnOps(features, None, {'csv_row': csv_row})


def example_serving_input_fn():
    """Build the serving inputs."""
    example_bytestring = tf.placeholder(
        shape=[None],
        dtype=tf.string,
    )
    feature_scalars = tf.parse_example(
        example_bytestring,
        tf.feature_column.make_parse_example_spec(INPUT_COLUMNS)
    )
    features = {
        key: tf.expand_dims(tensor, -1)
        for key, tensor in feature_scalars.iteritems()
    }
    return tf.contrib.learn.InputFnOps(
        features,
        None,  # labels
        {'example_proto': example_bytestring}
    )


def json_serving_input_fn():
    """Build the serving inputs."""
    inputs = {}
    for feat in INPUT_COLUMNS:
        inputs[feat.name] = tf.placeholder(shape=[None], dtype=feat.dtype)

    features = {
        key: tf.expand_dims(tensor, -1)
        for key, tensor in inputs.iteritems()
    }
    return tf.contrib.learn.InputFnOps(features, None, inputs)


SERVING_FUNCTIONS = {
    'JSON': json_serving_input_fn,
    'EXAMPLE': example_serving_input_fn,
    'CSV': csv_serving_input_fn
}


def parse_csv(rows_string_tensor):
    """Takes the string input tensor and returns a dict of rank-2 tensors."""

    # Takes a rank-1 tensor and converts it into rank-2 tensor
    # Example if the data is ['csv,line,1', 'csv,line,2', ..] to
    # [['csv,line,1'], ['csv,line,2']] which after parsing will result in a
    # tuple of tensors: [['csv'], ['csv']], [['line'], ['line']], [[1], [2]]
    row_columns = tf.expand_dims(rows_string_tensor, -1)
    columns = tf.decode_csv(row_columns, record_defaults=CSV_COLUMN_DEFAULTS)
    features = dict(zip(CSV_COLUMNS, columns))

    # Remove unused columns
    for col in UNUSED_COLUMNS:
        features.pop(col)
    return features


def generate_input_fn(filenames,
                      num_epochs=None,
                      shuffle=True,
                      skip_header_lines=0,
                      batch_size=200):
    """Generates an input function for training or evaluation.
    This uses the input pipeline based approach using file name queue
    to read data so that entire data is not loaded in memory.

    Args:
      filenames: [str] list of CSV files to read data from.
      num_epochs: int how many times through to read the data.
        If None will loop through data indefinitely
      shuffle: bool, whether or not to randomize the order of data.
        Controls randomization of both file order and line order within
        files.
      skip_header_lines: int set to non-zero in order to skip header lines
        in CSV files.
      batch_size: int First dimension size of the Tensors returned by
        input_fn
    Returns:
      A function () -> (features, indices) where features is a dictionary of
        Tensors, and indices is a single Tensor of label indices.
    """
    filename_queue = tf.train.string_input_producer(
        filenames, num_epochs=num_epochs, shuffle=shuffle)
    reader = tf.TextLineReader(skip_header_lines=skip_header_lines)

    _, rows = reader.read_up_to(filename_queue, num_records=batch_size)

    # Parse the CSV File
    features = parse_csv(rows)

    # This operation builds up a buffer of parsed tensors, so that parsing
    # input data doesn't block training
    # If requested it will also shuffle
    if shuffle:
        features = tf.train.shuffle_batch(
            features,
            batch_size,
            min_after_dequeue=2 * batch_size + 1,
            capacity=batch_size * 10,
            num_threads=4,
            enqueue_many=True,
            allow_smaller_final_batch=True
        )
    else:
        features = tf.train.batch(
            features,
            batch_size,
            capacity=batch_size * 10,
            num_threads=4,
            enqueue_many=True,
            allow_smaller_final_batch=True
        )

    return features, parse_label_column(features.pop(LABEL_COLUMN))