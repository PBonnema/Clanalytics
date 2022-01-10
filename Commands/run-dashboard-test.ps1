docker-compose --profile transfer-db-test up --exit-code-from mongo-transfer
docker-compose --profile dashboard-test up --exit-code-from blocktanksstats-test