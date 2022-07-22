## How to create migrations in EF Core
1. Install dotnet with version 3.1.201. Please be advised that ClassTranscribe Database may not run properly on newer version. Install instruction can be found at https://github.com/dotnet/core/blob/main/release-notes/3.1/3.1.3/3.1.201-download.md

2. Install dotnet-ef 3.1.4 by running
```
dotnet tool install --global dotnet-ef --version 3.1.4
``` 

3. Go to the ClassTranscribeDatabase directory
```
cd WebAPI/ClassTranscribeDatabase
``` 

4. Create a new migration by running
```
dotnet ef migrations add <name of migration>
```

## How to apply migration in the local database
```
dotnet ef database update
```

## How to apply migration in Docker container
1. Rebuild the solution
```
dotnet build --no-restore
```

2. Rebuild API image
```
docker build -t api -f API.Dockerfile .
```

3. Run Docker compose to see the changes
```
docker compose up api
```