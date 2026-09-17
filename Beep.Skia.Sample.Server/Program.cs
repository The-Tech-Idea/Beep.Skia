using System;
using System.IO;
using System.Text.Json;
using Beep.Skia.Automation;
using Beep.Skia.Model;

namespace Beep.Skia.Sample.Server
{
    /// <summary>
    /// HTTP host for the workflow execution SKU. All routing and status-code decisions live in
    /// <see cref="WorkflowRequestRouter"/>; this program is only the ASP.NET transport adapter,
    /// so the same facade can be hosted by anything else.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var options = ServerOptions.Parse(args);
            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            var service = new WorkflowExecutionService(options.MaxConcurrency);
            var api = new WorkflowExecutionApi(service);
            var router = new WorkflowRequestRouter(api);

            PublishStartupWorkflows(service, options);

            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            builder.WebHost.UseUrls(options.Url);

            var app = builder.Build();

            // Single catch-all endpoint: the tested router owns the route table and status codes.
            app.MapMethods("/{**path}", new[] { "GET", "POST", "PUT", "DELETE" }, async (HttpContext context) =>
            {
                string body = null;
                if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
                {
                    using var reader = new StreamReader(context.Request.Body);
                    body = await reader.ReadToEndAsync();
                }

                var httpRequest = new WorkflowHttpRequest(
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString.Value,
                    body);

                var response = router.Route(httpRequest);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} {context.Request.Method} {context.Request.Path}{context.Request.QueryString} -> {response.StatusCode}");
                return Results.Text(api.ToJson(response.Body), "application/json", statusCode: response.StatusCode);
            });

            app.Lifetime.ApplicationStopping.Register(() => service.Dispose());

            Console.WriteLine($"Beep.Skia workflow server listening on {options.Url}");
            Console.WriteLine($"Published workflows: {service.Workflows.Count}");
            Console.WriteLine("Press Ctrl+C to stop.");

            app.Run();
            Console.WriteLine("Stopped.");
            return 0;
        }

        private static void PublishStartupWorkflows(WorkflowExecutionService service, ServerOptions options)
        {
            if (options.Demo)
            {
                var workflow = new WorkflowDefinition("demo", "Demo Workflow");
                workflow.AddNode(new NodeDefinition(
                    "start",
                    "Start",
                    NodeType.Trigger,
                    typeof(Beep.Skia.Components.ManualTriggerNode).FullName));
                service.Publish(workflow);
                Console.WriteLine("Published demo workflow 'demo' (ManualTriggerNode).");
            }

            if (!string.IsNullOrWhiteSpace(options.PublishFile))
            {
                try
                {
                    var json = File.ReadAllText(options.PublishFile);
                    var diagram = JsonSerializer.Deserialize<Beep.Skia.Serialization.DiagramDto>(json);
                    var manager = new Beep.Skia.DrawingManager();
                    manager.LoadFromDto(diagram);
                    var workflow = manager.ToWorkflowDefinition(Path.GetFileNameWithoutExtension(options.PublishFile));
                    if (workflow.Nodes.Count == 0)
                    {
                        Console.Error.WriteLine($"'{options.PublishFile}' contains no automation nodes.");
                    }
                    else
                    {
                        service.Publish(workflow);
                        Console.WriteLine($"Published '{workflow.Name}' from {options.PublishFile} ({workflow.Nodes.Count} node(s)).");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Could not publish '{options.PublishFile}': {ex.Message}");
                }
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Beep.Skia workflow server");
            Console.WriteLine();
            Console.WriteLine("Usage: Beep.Skia.Sample.Server [options]");
            Console.WriteLine();
            Console.WriteLine("  --url <prefix>      Listen URL (default http://localhost:5199)");
            Console.WriteLine("  --concurrency <n>   Max parallel jobs (default 2)");
            Console.WriteLine("  --demo              Publish a demo workflow at startup");
            Console.WriteLine("  --publish <file>    Publish a saved diagram (skia_layout.json) at startup");
            Console.WriteLine("  --help              Show this help");
            Console.WriteLine();
            Console.WriteLine("Routes:");
            Console.WriteLine("  GET  /health");
            Console.WriteLine("  GET  /api/workflows");
            Console.WriteLine("  POST /api/workflows?name=Name            body: diagram DTO JSON");
            Console.WriteLine("  GET  /api/jobs?state=&limit=");
            Console.WriteLine("  POST /api/jobs?workflowId=&submittedBy=  body: inputs JSON object");
            Console.WriteLine("  GET  /api/jobs/{id}");
            Console.WriteLine("  POST /api/jobs/{id}/cancel|pause|resume");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  curl -X POST \"http://localhost:5199/api/jobs?workflowId=demo\" -d \"{\\\"environment\\\":\\\"ci\\\"}\"");
        }
    }

    /// <summary>Command-line options for the sample server.</summary>
    internal sealed class ServerOptions
    {
        public string Url { get; private set; } = "http://localhost:5199";
        public int MaxConcurrency { get; private set; } = 2;
        public bool Demo { get; private set; }
        public string PublishFile { get; private set; }
        public bool ShowHelp { get; private set; }

        public static ServerOptions Parse(string[] args)
        {
            var options = new ServerOptions();
            for (var i = 0; i < (args?.Length ?? 0); i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--url":
                        if (i + 1 < args.Length) options.Url = string.IsNullOrWhiteSpace(args[++i]) ? options.Url : args[i].Trim();
                        break;
                    case "--concurrency":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out var concurrency))
                            options.MaxConcurrency = Math.Max(1, concurrency);
                        break;
                    case "--demo":
                        options.Demo = true;
                        break;
                    case "--publish":
                        if (i + 1 < args.Length) options.PublishFile = args[++i];
                        break;
                    case "--help":
                    case "-h":
                    case "/?":
                        options.ShowHelp = true;
                        break;
                }
            }

            if (!options.Demo && string.IsNullOrWhiteSpace(options.PublishFile) && File.Exists("skia_layout.json"))
                options.PublishFile = "skia_layout.json";

            return options;
        }
    }
}
