docker-compose `
	--profile dashboard `
	--profile ingestion `
	--profile dashboard-test `
	--profile ingestion-test `
	--profile ingestion-dev `
	--profile migrate-database `
	--profile transfer-db-test `
	pull
