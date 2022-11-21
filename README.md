## WebAPI

This repository provides the source code primary API endpoint of the ClassTranscribe Server. Copyright (C) University of Illinois, USA. 2019-2020

The source code in this repository is licensed here under the GNU Public License 3.0 (https://www.gnu.org/licenses/gpl-3.0.en.html). Please email angrave at Illinois if you are interested in alternative licenses of this code and related intellectual property.

## Build Status

| Branch | Status |
|-------:|--------|
| Production | ![Production](https://github.com/classtranscribe/WebAPI/actions/workflows/docker.yml/badge.svg?event=push&branch=main) |
| Staging | ![Staging](https://github.com/classtranscribe/WebAPI/actions/workflows/docker.yml/badge.svg?event=push&branch=staging) |
| Experiment | ![Experiment](https://github.com/classtranscribe/WebAPI/actions/workflows/docker.yml/badge.svg?event=push&branch=expt) |

# Pull requests, Submitting code and copyright.

In submitting code to this repository  - for example by issuing a git pull-request, or working directly with ClassTranscribe developers to merge or add code - you agree to re-assign copyright of the code to the University of Illinois.

# Build Instructions

This repository is in the form of 3 docker services,
1. The ClassTranscribeServer (service name - classtranscribeserver, accessible on port 8080) 
2. A postgresQL database server (service name - db, accessible on port 5432)
3. A pgadmin server, which allows to interact with the db using a GUI interface (service name - pgadmin, accessible on port 5050)

To build this repository,
1. Install docker 
2. Obtain all the environment variable files from an admin
3. To start all the services
    ```
    docker-compose up
    ```
    
    To start a single service
    ```
    docker-compose up [service_name]
    ```
    Eg.
    ```
    docker-compose up -d classtranscribeserver
    ```
    "-d" option allows running the service in a detached mode
   
 4. That's it.
 
 ## Building Docker Images Manually
 You can also build any of the three services individually for testing using one or more of the following commands:
 ```
 docker build -t classtranscribe/api:latest -f API.Dockerfile .
 docker build -t classtranscribe/pythonrpcserver -f pythonrpcserver.Dockerfile .
 docker build -t classtranscribe/taskengine -f TaskEngine.Dockerfile .
 ```
 
 #### Overriding PYTUBE_VERSION
 By default, the `pythonrpcserver` is built with the latest version of pytube available from pypi.
 
 To build `pythonrpcserver` with a specific version of [pytube](https://pypi.org/project/pytube/) (for testing purposes):
 ```bash
 docker build -t classtranscribe/pythonrpcserver -f pythonrpcserver.Dockerfile . --build-arg=PYTUBE_VERSION=12.1.0
 ```
 
 This will run an optional build step to override the installed version of `pytube` with the target version downloaded directly from Github.
 
# Notes
1. If there are dependency changes made to classtranscribeserver, you are required to rebuild using docker-compose
  ```docker-compose up --build classtranscribeserver```
2. After making any code changes to rebuild code, just re-run the service,
  ```docker-compose up classtranscribeserver```
3. Refer [gotchas.md](./gotchas.md) for known coding blunders.
    
