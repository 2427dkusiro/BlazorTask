# Blazor Task
A high-performance multi-threading Library for blazor webassembly supporting .NET 6

## Targets
+ To focusing on only blazor webassembly model and .NET 6(or later), provide higher performance than other library.
+ Provide task-like api include blocking-wait, re-throw exception.

## Progress
This project is in developing. Now I'm working on implementing core functions.

## Performance

### boot worker

|Environment|Time|
|----------:|---:|
|Blazor Task|235.7ms|
|Blazor worker|1199.3ms|

This library is 4x faster than existing library at booting worker.

### simple method call

|Environment|First|N=100|N=256|
|----------:|----:|----:|----:|
|Blazor Task|88.9ms|69.9ms|174.2ms|
|Blazor worker|644.0ms|1953.3ms|4951.3ms|

In this test, measures time to call simple method(add two integer).
This library is 7x faster at first call, and 20x faster at multiple calls.

### Implemented
+ Boot worker
+ Faster boot using cache
+ Fetch compressed(.br) assemblies
+ Load globalization data
+ Call Library implemeted function
+ Serialization-based binary argument passing (transfer json encoded arguments as UTF-8 binary)