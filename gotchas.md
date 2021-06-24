## Gotchas

This document lists known coding mistakes and other gotchas.

1. Lazy loading error. Please refer [this](https://docs.microsoft.com/en-us/ef/ef6/querying/related-data) to know the best practices on loading related entities of a model.
2. A bad update from Microsoft prevents EntityFrameworkCore's Add-Migration from completing. Solution: 
In ClassTranscribeDatabase.csproj remove "\<PrivateAssets\>all\</PrivateAssets\>" from \<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="3.1.1"\>
      
