docker-compose --profile transfer-db-test up --exit-code-from mongo-transfer
docker-compose --profile migrate-database up