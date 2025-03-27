- **Prepare**
  - Install .Net 8 SDK and Runtime
  - Install Docker Desktop

- **Run docker compose up for setup Elasticsearch**
```
docker-compose -f docker-compose.yml up -d
```

- **Instal Elasticsearch phonetics plugin**
Exec into lab-elastic-search container
```
bin/elasticsearch-plugin install analysis-phonetic
```

- **Run project**
```
dotnet run
```