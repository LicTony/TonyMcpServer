# Resumen del Proyecto

Este proyecto es una aplicación de consola C# .NET que funciona como un servidor. Se comunica utilizando el protocolo JSON-RPC sobre la entrada/salida estándar. El servidor expone una herramienta simple llamada `saludar` que devuelve un saludo personalizado.

## Compilación y Ejecución

Para compilar y publicar la aplicación, ejecuta el script `publish.bat`. Esto creará un ejecutable auto-contenido para Windows (win-x64) en el directorio `bin\Release\net8.0\win-x64\publish`.

```bash
.\publish.bat
```

El ejecutable principal es `TonyMcpServer.exe`.

## Integración con Gemini CLI

Para agregar este servidor a Gemini CLI, agrega la siguiente configuración a tu archivo `settings.json`:

```json
"mcpServers": {
    "tonysutils": {
        "command": "C:\\_Tony\\TonyMcpServer\\bin\\Release\\net8.0\\win-x64\\publish\\TonyMcpServer.exe",
        "args": []
    }
}
```

## Convenciones de Desarrollo

*   El proyecto sigue las convenciones estándar de C# y .NET.
*   La lógica del servidor se encuentra en `Program.cs`.
*   El proyecto utiliza el framework .NET 8.0.
*   El registro de eventos (logging) se implementa en un archivo llamado `mcp_debug.log` en el mismo directorio que el ejecutable.