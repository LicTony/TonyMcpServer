// See https://aka.ms/new-console-template for more information
using System.IO;
using System.Text.Json;
using System;

namespace TonyMcpServer
{
    public static class Program
    {
        private static bool loggingEnabled = false;
        private static string settingsFile = Path.Combine(Directory.GetCurrentDirectory(), "log_settings.txt");
        private static StreamWriter logWriter = StreamWriter.Null;

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            // Cargar estado del log desde el archivo
            if (File.Exists(settingsFile) && File.ReadAllText(settingsFile) == "enabled")
            {
                loggingEnabled = true;
            }

            // Configurar logging a archivo
            if (loggingEnabled)
            {
                string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "mcp_debug.log");
                logWriter = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
            }

            Log("=== SERVIDOR MCP INICIADO ===");

            try
            {
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line == null)
                    {
                        Log("ReadLine devolvió null - fin de stream");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        Log("Línea vacía recibida, continuando...");
                        continue;
                    }

                    Log($"REQUEST: {line}");

                    ProcessRequest(line);
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR FATAL EN MAIN: {ex}");
            }
            finally
            {
                Log("=== SERVIDOR MCP TERMINADO ===");
                logWriter.Close();
            }
        }

        static void ProcessRequest(string line)
        {
            object? responseId = null;
            try
            {
                Log("Iniciando deserialización JSON...");
                var request = JsonSerializer.Deserialize<JsonElement>(line);
                Log("JSON deserializado correctamente");

                if (request.TryGetProperty("id", out var idProp))
                {
                    responseId = idProp.ValueKind switch
                    {
                        JsonValueKind.Number => idProp.GetInt32(),
                        JsonValueKind.String => idProp.GetString(),
                        _ => ""
                    };
                    Log($"ID extraído: {responseId} (tipo: {idProp.ValueKind})");
                }
                else
                {
                    Log("Request sin ID (notificación)");
                    responseId = "";
                }

                if (!request.TryGetProperty("method", out var methodProp) || methodProp.GetString() is not string method)
                {
                    Log("ERROR: Request sin propiedad 'method' válida");
                    return;
                }

                Log($"Método extraído: '{method}'");

                switch (method)
                {
                    case "initialize":
                        Log("Procesando método 'initialize'");
                        HandleInitialize(responseId);
                        break;
                    case "notifications/initialized":
                        Log("Recibida notificación 'initialized' - no responder");
                        break;
                    case "tools/list":
                        Log("Procesando método 'tools/list'");
                        HandleToolsList(responseId);
                        break;
                    case "tools/call":
                        Log("Procesando método 'tools/call'");
                        HandleToolsCall(request, responseId);
                        break;
                    default:
                        Log($"Método no soportado: '{method}'");
                        HandleUnsupportedMethod(method, responseId);
                        break;
                }
            }
            catch (JsonException ex)
            {
                Log($"ERROR JSON: {ex.Message}");
                SendErrorResponse(responseId, -32700, "Parse error");
            }
            catch (Exception ex)
            {
                Log($"EXCEPCIÓN EN ProcessRequest: {ex}");
                SendErrorResponse(responseId, -32603, ex.Message);
            }
        }

        static void HandleInitialize(object? responseId)
        {
            var response = new { jsonrpc = "2.0", id = responseId, result = new { protocolVersion = "2024-11-05", serverInfo = new { name = "TonyMcpServer", version = "1.0" }, capabilities = new { tools = new { } } } };
            SendResponse(response, "initialize");
        }

        static void HandleToolsList(object? responseId)
        {
            var tools = new object[]
            {
                new { name = "saludar", description = "Devuelve un saludo personalizado", inputSchema = new { type = "object", properties = new { nombre = new { type = "string" } }, required = new[] { "nombre" } } },
                new { name = "get-pre-fijo-archivo-yyyymmddhhmmss", description = "Retorna la fecha y hora actual en formato yyyyMMdd_HHmmss", inputSchema = new { type = "object", properties = new {}, required = Array.Empty<string>() } },
                new { name = "activar_log", description = "Activa el logging del servidor MCP Tony y guarda el estado en log_settings.txt.", inputSchema = new { type = "object", properties = new {}, required = Array.Empty<string>() } },
                new { name = "desactivar_log", description = "Desactiva el logging del servidor MCP Tony y guarda el estado en log_settings.txt.", inputSchema = new { type = "object", properties = new {}, required = Array.Empty<string>() } }
            };
            var response = new { jsonrpc = "2.0", id = responseId, result = new { tools } };
            SendResponse(response, "tools/list");
        }

        static void HandleToolsCall(JsonElement request, object? responseId)
        {
            try
            {
                var toolName = request.GetProperty("params").GetProperty("name").GetString();
                Log($"Ejecutando herramienta: {toolName}");

                string responseText;
                switch (toolName)
                {
                    case "saludar":
                        var nombre = request.GetProperty("params").GetProperty("arguments").GetProperty("nombre").GetString();
                        responseText = $"Hola {nombre}, saludo desde TonyMcpServer 👋";
                        break;
                    case "get-pre-fijo-archivo-yyyymmddhhmmss":
                        responseText = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        break;
                    case "activar_log":
                        File.WriteAllText(settingsFile, "enabled");
                        loggingEnabled = true;
                        if (logWriter == StreamWriter.Null)
                        {
                            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "mcp_debug.log");
                            logWriter = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
                        }
                        responseText = "Logging activado. Los logs se guardarán en mcp_debug.log.";
                        Log("--- LOGGING ACTIVADO ---");
                        break;
                    case "desactivar_log":
                        Log("--- DESACTIVANDO LOGGING ---");
                        File.WriteAllText(settingsFile, "disabled");
                        loggingEnabled = false;
                        logWriter.Close();
                        logWriter = StreamWriter.Null;
                        responseText = "Logging desactivado.";
                        break;
                    default:
                        SendErrorResponse(responseId, -32601, $"Tool no soportada: {toolName}");
                        return;
                }

                var response = new { jsonrpc = "2.0", id = responseId, result = new { content = new[] { new { type = "text", text = responseText } } } };
                SendResponse(response, "tools/call success");
            }
            catch (Exception ex)
            {
                Log($"Error en tools/call: {ex.Message}");
                SendErrorResponse(responseId, -32603, ex.Message);
            }
        }

        static void HandleUnsupportedMethod(string method, object? responseId)
        {
            SendErrorResponse(responseId, -32601, $"Método no soportado: {method}");
        }

        static void SendResponse(object response, string context)
        {
            try
            {
                var responseJson = JsonSerializer.Serialize(response);
                Console.WriteLine(responseJson);
                Console.Out.Flush();
                Log($"RESPONSE {context}: {responseJson}");
            }
            catch (Exception ex)
            {
                Log($"ERROR enviando respuesta {context}: {ex.Message}");
            }
        }

        static void SendErrorResponse(object? responseId, int code, string message)
        {
            if (responseId != null)
            {
                var error = new { jsonrpc = "2.0", id = responseId, error = new { code, message } };
                SendResponse(error, $"error {code}");
            }
            else
            {
                Log($"Error en notificación (sin ID): {code} - {message}");
            }
        }

        static void Log(string message)
        {
            if (!loggingEnabled) return;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            logWriter.WriteLine($"[{timestamp}] {message}");
        }
    }
}