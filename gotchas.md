## Gotchas

This document lists known coding mistakes.

1. Lazy loading error. Please refer [this](https://docs.microsoft.com/en-us/ef/ef6/querying/related-data) to know the best practices on loading related entities of a model.

Building frontend in Docker and node gives you an OOM? Try pruning docker and also restarting Docker wth a larger memory e.g. 3GB
