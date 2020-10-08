# ClassTranscribeServer Unit Tests
Our unit tests use the xUnit testing framework (https://xunit.net/).

To remove dependencies on a real database, we use Entity Framework's In-Memory database that they offer for unit testing. You can read more about the in-memory DB (and other options for unit testing) here: https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/.

## Running

From the `WebAPI` directory, you can run these tests on the command line with:
```
dotnet test UnitTests
```

You can also use the CLI to run specific unit tests (https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=xunit).

You can also run the tests in Visual Studio.

## Unit Tests vs Integration Tests
Unit tests aim to test small parts of business logic (BL) without any dependencies on other parts of the system (such as a database or data access layer). This is why we use an in-memory DB for mocking the real database and data access layer.

To learn more about unit testing compared to integration testing, look here: https://stackoverflow.com/a/5357837.

Look here for EF core unit testing guidelines: https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/#unit-testing.

## Architecture

Each controller has its own test class which is a subclass of the `BaseControllerTest`. This base class sets up a new context and In-Memory DB in its constructor.

Following xUnit's built-in parallelism (https://xunit.net/docs/running-tests-in-parallel.html), test cases are run sequentially within any given test class but all test classes are run in parallel. Following xUnit's shared contexts (https://xunit.net/docs/shared-context), the parent test class's constructor is run before every single test case, which ensures that every single test case gets a completely fresh DB (so make sure to not rely on previous test cases when writing a new test).