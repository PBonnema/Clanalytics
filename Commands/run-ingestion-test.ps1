docker-compose --profile transfer-db-test up --exit-code-from mongo-transfer
docker-compose --profile ingestion-test up --exit-code-from ingestion-test