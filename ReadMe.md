# Blazor Task
A high-performance multi-threading Library for blazor webassembly supporting .NET 6

## Targets
+ To focusing on only blazor webassembly model and .NET 6(or later), provide higher performance than other library.
+ Provide task-like api include blocking-wait, re-throw exception.

## Progress
This project is in developing. Now I'm working on implementing core functions.

### Implemented
+ Boot worker
+ Faster boot using cache
+ Fetch compressed(.br) assemblies
+ Load globalization data
+ Call Library implemeted function
+ Serialization-based binary argument passing (transfer json encoded arguments as UTF-8 binary)