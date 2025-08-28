// See https://aka.ms/new-console-template for more information
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Reflection.Metadata;

namespace TonyMcpServer
{
    public static class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            // Configurar logging a archivo
            var logFile = Path.Combine(Directory.GetCurrentDirectory(), "mcp_debug.log");
            using var logWriter = new StreamWriter(logFile, append: true);
            logWriter.AutoFlush = true;

            Log(logWriter, "=== SERVIDOR MCP INICIADO ===");

            try
            {
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line == null)
                    {
                        Log(logWriter, "ReadLine devolvió null - fin de stream");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        Log(logWriter, "Línea vacía recibida, continuando...");
                        continue;
                    }

                    Log(logWriter, $"REQUEST: {line}");

                    ProcessRequest(line, logWriter);
                }
            }
            catch (Exception ex)
            {
                Log(logWriter, $"ERROR FATAL EN MAIN: {ex}");
            }
            finally
            {
                Log(logWriter, "=== SERVIDOR MCP TERMINADO ===");
            }
        }

        static void ProcessRequest(string line, StreamWriter logWriter)
        {
            object? responseId = null;
            try
            {
                Log(logWriter, "Iniciando deserialización JSON...");
                var request = JsonSerializer.Deserialize<JsonElement>(line);
                Log(logWriter, "JSON deserializado correctamente");

                // Capturamos el id de la request si existe
                if (request.TryGetProperty("id", out var idProp))
                {
                    if (idProp.ValueKind == JsonValueKind.Number)
                        responseId = idProp.GetInt32();
                    else if (idProp.ValueKind == JsonValueKind.String)
                        responseId = idProp.GetString();
                    else
                        responseId = "";
                    Log(logWriter, $"ID extraído: {responseId} (tipo: {idProp.ValueKind})");
                }
                else
                {
                    Log(logWriter, "Request sin ID (notificación)");
                    responseId = "";
                }

                if (!request.TryGetProperty("method", out var methodProp))
                {
                    Log(logWriter, "ERROR: Request sin propiedad 'method'");
                    return;
                }

                string method = methodProp.GetString() ?? "";
                Log(logWriter, $"Método extraído: '{method}'");

                switch (method)
                {
                    case "initialize":
                        Log(logWriter, "Procesando método 'initialize'");
                        HandleInitialize(responseId, logWriter);
                        break;

                    case "notifications/initialized":
                        Log(logWriter, "Recibida notificación 'initialized' - no responder");
                        // No hacer nada - es una notificación
                        break;

                    case "tools/list":
                        Log(logWriter, "Procesando método 'tools/list'");
                        HandleToolsList(responseId, logWriter);
                        break;

                    case "tools/call":
                        Log(logWriter, "Procesando método 'tools/call'");
                        HandleToolsCall(request, responseId, logWriter);
                        break;

                    default:
                        Log(logWriter, $"Método no soportado: '{method}'");
                        HandleUnsupportedMethod(method, responseId, logWriter);
                        break;
                }
            }
            catch (JsonException ex)
            {
                Log(logWriter, $"ERROR JSON: {ex.Message}");
                SendErrorResponse(responseId, -32700, "Parse error", logWriter);
            }
            catch (Exception ex)
            {
                Log(logWriter, $"EXCEPCIÓN EN ProcessRequest: {ex}");
                SendErrorResponse(responseId, -32603, ex.Message, logWriter);
            }
        }

        static void HandleInitialize(object? responseId, StreamWriter logWriter)
        {
            var response = new
            {
                jsonrpc = "2.0",
                id = responseId,
                result = new
                {
                    protocolVersion = "2024-11-05",
                    serverInfo = new { name = "TonyMcpServer", version = "1.0" },
                    capabilities = new { tools = new { } }
                }
            };
            SendResponse(response, "initialize", logWriter);
        }

        static void HandleToolsList(object? responseId, StreamWriter logWriter)
        {
            var response = new
            {
                jsonrpc = "2.0",
                id = responseId,
                result = new
                {
                    tools = new object[]
                    {
                    new {
                        name = "saludar",
                        description = "Devuelve un saludo personalizado",
                        inputSchema = new {
                            type = "object",
                            properties = new {
                                nombre = new { type = "string" }
                            },
                            required = new[] { "nombre" }
                        }
                    }
                    }
                }
            };
            SendResponse(response, "tools/list", logWriter);
        }

        static void HandleToolsCall(JsonElement request, object? responseId, StreamWriter logWriter)
        {
            try
            {
                var toolName = request.GetProperty("params").GetProperty("name").GetString();
                Log(logWriter, $"Ejecutando herramienta: {toolName}");

                if (toolName == "saludar")
                {
                    var args = request.GetProperty("params").GetProperty("arguments");
                    var nombre = args.GetProperty("nombre").GetString();

                    var response = new
                    {
                        jsonrpc = "2.0",
                        id = responseId,
                        result = new
                        {
                            content = new[]
                            {
                            new { type = "text", text = $"Hola {nombre}, saludo desde TonyMcpServer 👋" }
                        }
                        }
                    };
                    SendResponse(response, "tools/call success", logWriter);
                }
                else
                {
                    SendErrorResponse(responseId, -32601, $"Tool no soportada: {toolName}", logWriter);
                }
            }
            catch (Exception ex)
            {
                Log(logWriter, $"Error en tools/call: {ex.Message}");
                SendErrorResponse(responseId, -32603, ex.Message, logWriter);
            }
        }

        static void HandleUnsupportedMethod(string method, object? responseId, StreamWriter logWriter)
        {
            SendErrorResponse(responseId, -32601, $"Método no soportado: {method}", logWriter);
        }

        static void SendResponse(object response, string context, StreamWriter logWriter)
        {
            try
            {
                var responseJson = JsonSerializer.Serialize(response);
                Console.WriteLine(responseJson);
                Console.Out.Flush(); // Forzar flush del stdout
                Log(logWriter, $"RESPONSE {context}: {responseJson}");
            }
            catch (Exception ex)
            {
                Log(logWriter, $"ERROR enviando respuesta {context}: {ex.Message}");
            }
        }

        static void SendErrorResponse(object? responseId, int code, string message, StreamWriter logWriter)
        {
            if (responseId != null)
            {
                var error = new
                {
                    jsonrpc = "2.0",
                    id = responseId,
                    error = new { code, message }
                };
                SendResponse(error, $"error {code}", logWriter);
            }
            else
            {
                Log(logWriter, $"Error en notificación (sin ID): {code} - {message}");
            }
        }

        static void Log(StreamWriter writer, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            writer.WriteLine($"[{timestamp}] {message}");
        }
    }

}