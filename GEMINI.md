# Project Overview

This project is a C# .NET console application that functions as a server. It communicates using the JSON-RPC protocol over standard input/output. The server exposes a simple tool named `saludar` which returns a personalized greeting.

## Building and Running

To build and publish the application, run the `publish.bat` script. This will create a self-contained executable for Windows (win-x64) in the `bin\Release\net8.0\win-x64\publish` directory.

```bash
.\publish.bat
```

The main executable is `TonyMcpServer.exe`.

## Development Conventions

*   The project follows standard C# and .NET conventions.
*   The server logic is contained within `Program.cs`.
*   The project uses the .NET 8.0 framework.
*   Logging is implemented to a file named `mcp_debug.log` in the same directory as the executable.
