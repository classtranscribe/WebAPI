## Gotchas

This document lists known coding mistakes and other gotchas.

1. Lazy loading error. Please refer [this](https://docs.microsoft.com/en-us/ef/ef6/querying/related-data) to know the best practices on loading related entities of a model.
2. An update from Microsoft prevents EntityFrameworkCore's Add-Migration from completing. Solution: 
In ClassTranscribeDatabase.csproj remove or xml-comment "\<PrivateAssets\>all\</PrivateAssets\>" from \<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="3.1.1"\>
See [this](https://stackoverflow.com/questions/52536588/your-startup-project-doesnt-reference-microsoft-entityframeworkcore-design)
      
