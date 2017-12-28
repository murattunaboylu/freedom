TRAIN_DATA=$(pwd)/input/train.csv
EVAL_DATA=$(pwd)/input/test.csv
MODEL_DIR=output

# train local
gcloud ml-engine local train \
    --module-name trainer.task \
    --package-path trainer/ \
    -- \
    --train-files $TRAIN_DATA \
    --eval-files $EVAL_DATA \
    --train-steps 1000 \
    --job-dir $MODEL_DIR \
    --eval-steps 100

# train local distributed
gcloud ml-engine local train \
    --module-name trainer.task \
    --package-path trainer/ \
    --distributed \
    -- \
    --train-files $TRAIN_DATA \
    --eval-files $EVAL_DATA \
    --train-steps 1000 \
    --job-dir $MODEL_DIR \
    --eval-steps 100

tensorboard --logdir=output --port=8080

# copy training and prediction files to GS
BUCKET_NAME=gs//titanic-model/data
TRAIN_DATA=$BUCKET_NAME/train.data.processed.csv
EVAL_DATA=$BUCKET_NAME/train.eval.processed.csv
TEST_JSON=$BUCKET_NAME/test.json

# train on cloud - submit a job
gcloud ml-engine jobs submit training $JOB_NAME --job-dir $OUTPUT_PATH --runtime-version 1.2 --module-name trainer.task --package-path trainer/ --regio
n europe-west1 -- --train-files $TRAIN_DATA --eval-files $EVAL_DATA --train-steps 1000 --verbosity DEBUG

# train on cloud - distributed
gcloud ml-engine jobs submit training $JOB_NAME --job-dir $OUTPUT_PATH --runtime-version 1.2 --module-name trainer.task --package-path trainer/ --regio
n europe-west1 --scale-tier STANDARD_1 -- --train-files $TRAIN_DATA --eval-files $EVAL_DATA --train-steps 1000 --verbosity DEBUG

# hyperparameter tuning
gcloud ml-engine jobs submit training $JOB_NAME --job-dir $OUTPUT_PATH --runtime-version 1.2 --config \
../cloudml-samples-master/census/hptuning_config.yaml --module-name trainer.task --package-path trainer/ \
--region europe-west1 --scale-tier STANDARD_1 -- --train-files $TRAIN_DATA --eval-files $EVAL_DATA --train-steps 1000 --verbosity DEBUG

# predict
gcloud ml-engine local predict \
--model-dir=$MODEL_DIR/export/Servo/1505056497/ \
--json-instances \
data/test.json

# write to a file
gcloud ml-engine local predict \
--model-dir=$MODEL_DIR/export/Servo/1505056497/ \
--json-instances \
data/test.json > output.cabin.txt

# deploy to GCP as a new version
gcloud ml-engine versions create v2 --model=titanic --origin=$MODEL_BINARIES --staging-bucket=gs://titanic-model
# Create Version failed. Bad model detected with error: "Error loading the model: Could not load model. "

# send a prediction to GCP version
gcloud ml-engine predict \
--model titanic \
--version v2 \
--json-instances \
data/test.json

