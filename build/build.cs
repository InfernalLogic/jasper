using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using IntegrationTests;
using Npgsql;
using static System.Globalization.CultureInfo;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    internal class Build
    {
        private const string BUILD_VERSION = "6.0.0";
        private const string GITHUB_REPO = "https://github.com/jasperfx/jasper.git";

        private const string Forwards = @"
getting_started
documentation/
documentation/ioc/
documentation/ioc/concepts/
documentation/ioc/bootstrapping/
documentation/ioc/aspnetcore/
documentation/ioc/blazor/
documentation/ioc/registration/
documentation/ioc/registration/registry-dsl/
documentation/ioc/registration/inline-dependencies/
documentation/ioc/registration/auto-registration-and-conventions/
documentation/ioc/registration/configured-instance/
documentation/ioc/registration/policies/
documentation/ioc/registration/policies/
documentation/ioc/registration/changing-configuration-at-runtime/
documentation/ioc/registration/constructor-selection/
documentation/ioc/registration/existing-objects/
documentation/ioc/registration/attributes/
documentation/ioc/lifetime/
documentation/ioc/resolving/
documentation/ioc/resolving/get-a-service-by-plugintype/
documentation/ioc/resolving/get-a-service-by-plugin-type-and-name/
documentation/ioc/resolving/get-all-services-by-plugin-type/
documentation/ioc/resolving/try-getting-an-optional-service-by-plugin-type/
documentation/ioc/resolving/try-geting-an-optional-service-by-plugin-type-and-name/
documentation/ioc/resolving/requesting-a-concrete-type/
documentation/ioc/generics/
documentation/ioc/decorators/
documentation/ioc/diagnostics/
documentation/ioc/diagnostics/whatdoihave/
documentation/ioc/diagnostics/validating-container-configuration/
documentation/ioc/diagnostics/environment-tests/
documentation/ioc/diagnostics/build-plans/
documentation/ioc/diagnostics/using-the-container-model/
documentation/ioc/diagnostics/type-scanning/
documentation/ioc/auto-wiring/
documentation/ioc/lambdas/
documentation/ioc/injecting-at-runtime/
documentation/ioc/disposing/
documentation/ioc/lazy-resolution/
documentation/ioc/nested-containers/
documentation/ioc/setter-injection/
documentation/ioc/working-with-enumerable-types/
documentation/compilation/
documentation/compilation/assemblygenerator/
documentation/compilation/sourcewriter/
documentation/compilation/frames/
documentation/compilation/frames/variables/
documentation/compilation/frames/frame/
documentation/compilation/frames/extension-methods/
documentation/compilation/frames/injected-fields/
";

        private static void Main(string[] args)
        {

            Target("default", DependsOn("test", "commands"));

            Target("restore", () =>
            {
                Run("dotnet", "restore Jasper.sln");
            });

            Target("compile",  DependsOn("restore"),() =>
            {
                Run("dotnet",
                    $"build Jasper.sln --no-restore");
            });

            Target("test", DependsOn("compile"),() =>
            {
                RunTests("Jasper.Testing");
                //RunTests("Jasper.Http.Testing");
            });

            Target("full", DependsOn("test", "test-persistence", "storyteller", "test-tcp", "test-rabbitmq", "test-pulsar"));

            Target("test-persistence", DependsOn("docker-up", "compile"), () =>
            {
                RunTests("Jasper.Persistence.Testing");
            });

            Target("test-rabbitmq", DependsOn("docker-up", "compile"), () =>
            {
                RunTests("Jasper.RabbitMQ.Tests");
            });

            Target("test-pulsar", DependsOn("docker-up", "compile"), () =>
            {
                RunTests("Jasper.Pulsar.Tests");
            });

            Target("test-tcp", DependsOn("compile"), () =>
            {
                RunTests("Jasper.Tcp.Tests");
            });

            Target("commands", DependsOn("compile"),() =>
            {
                var original = Directory.GetCurrentDirectory();

                /*
  Dir.chdir("src/Subscriber") do
    sh "dotnet run -- ?"
    sh "dotnet run -- export-json-schema obj/schema"
  end
                 */

                Directory.SetCurrentDirectory(Path.Combine(original, "src", "ConsoleApp"));
                RunCurrentProject("?");
                RunCurrentProject("export-json-schema obj/schema");
                RunCurrentProject("describe");


                Directory.SetCurrentDirectory(original);
            });

            Target("ci", DependsOn("default"));

            Target("install", () =>
                RunNpm("install"));

            Target("benchmark", () =>
            {
                Run("dotnet", "run -c Release", workingDirectory:"src/Benchmarks");
            });

            Target("install-mdsnippets", IgnoreIfFailed(() =>
                Run("dotnet", $"tool install -g MarkdownSnippets.Tool")
            ));

            Target("docs", DependsOn("install", "install-mdsnippets"), () => {
                // Run docs site
                RunNpm("run docs");
            });

            Target("docs-build", DependsOn("install", "install-mdsnippets"), () => {
                // Run docs site
                RunNpm("run docs-build");
            });

            Target("clear-inline-samples", () => {
                var files = Directory.GetFiles("./docs", "*.md", SearchOption.AllDirectories);
                var pattern = @"<!-- snippet:(.+)-->[\s\S]*?<!-- endSnippet -->";
                var replacePattern = $"<!-- snippet:$1-->{Environment.NewLine}<!-- endSnippet -->";
                foreach (var file in files)
                {
                    // Console.WriteLine(file);
                    var content = File.ReadAllText(file);

                    if (!content.Contains("<!-- snippet:")) {
                        continue;
                    }

                    var updatedContent = Regex.Replace(content, pattern, replacePattern);
                    File.WriteAllText(file, updatedContent);
                }
            });

            Target("publish-docs", DependsOn("docs-build"), () =>
            {
                var docTargetDir = "doc-target";
                var branchName = "gh-pages";
                Run("git", $"clone -b {branchName} {GITHUB_REPO} {InitializeDirectory(docTargetDir)}");
                // if you are not using git --global config, un-comment the block below, update and use it
                // Run("git", "config user.email user_email", docTargetDir);
                // Run("git", "config user.name user_name", docTargetDir);

                // Clean off all the files in the doc-target directory
                foreach (var file in Directory.EnumerateFiles(docTargetDir))
                {
                    if (Path.GetFileName(file) == ".nojekyll") continue;

                    File.Delete(file);
                }

                var buildDir = Path.Combine(Environment.CurrentDirectory, "docs", ".vitepress", "dist");

                CopyFilesRecursively(buildDir, docTargetDir);

                WriteForwards();

                Run("git", "add --all", docTargetDir);
                Run("git", $"commit -a -m \"Documentation Update for {BUILD_VERSION}\" --allow-empty", docTargetDir);
                Run("git", $"push origin {branchName}", docTargetDir);
            });

			Target("storyteller", DependsOn("compile"), () =>
                Run("dotnet", $"run --culture en-US", "src/StorytellerSpecs"));

            Target("open_st", DependsOn("compile"), () =>
                Run("dotnet", $"storyteller open  --culture en-US", "src/StorytellerSpecs"));


            Target("docker-up", () =>
            {
                Run("docker", "compose up -d");

                WaitForDatabaseToBeReady();

            });

            Target("pack", DependsOn("compile"), ForEach("./src/Jasper", "./src/Jasper.RabbitMQ", "./src/Jasper.Persistence.Database", "./src/Jasper.Persistence.Postgresql", "./src/Jasper.Persistence.Marten"), project =>
                Run("dotnet", $"pack {project} -o ./artifacts --configuration Release"));


            RunTargetsAndExit(args);
        }

        private static void WaitForDatabaseToBeReady()
        {
            var attempt = 0;
            while (attempt < 10)
                try
                {
                    using var conn = new NpgsqlConnection(Servers.PostgresConnectionString);
                    conn.Open();

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "select 1";
                    cmd.ExecuteReader();

                    Console.WriteLine("Postgresql is up and ready!");
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(250);
                    attempt++;
                }
        }

        private static void WriteForwards()
        {
            var reader = new StringReader(Forwards);
            var url = reader.ReadLine();
            while (url != null)
            {
                if (!string.IsNullOrEmpty(url))
                {
                    WriteForwardingHtml(url, "lamar/guide");
                }

                url = reader.ReadLine();
            }
        }

        private static void WriteForwardingHtml(string from, string to)
        {
            var parts = from.TrimEnd('/').Split('/');

            var directory = "doc-target";
            for (var i = 0; i < parts.Length - 1; i++)
            {
                directory = Path.Combine(directory, parts[i]);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }

            var html = $@"<head>
      <meta http-equiv='refresh' content='5; URL=https://jasperfx.github.io/lamar/guide/' />
  </head>
  <body>
      <p>If you are not redirected in five seconds, <a href='https://jasperfx.github.io/lamar/guide/'>click here</a>.</p>
</body>
".Replace("'", "\"");

            var destination = Path.Combine(directory, parts.Last() + ".html");
            File.WriteAllText(destination, html);
        }


        private static void RunCurrentProject(string args)
        {
            Run("dotnet", $"run  --framework net6.0 --no-build --no-restore -- {args}");
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        private static string InitializeDirectory(string path)
        {
            EnsureDirectoriesDeleted(path);
            Directory.CreateDirectory(path);
            return path;
        }

        private static void EnsureDirectoriesDeleted(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    var dir = new DirectoryInfo(path);
                    DeleteDirectory(dir);
                }
            }
        }

        private static void DeleteDirectory(DirectoryInfo baseDir)
        {
            baseDir.Attributes = FileAttributes.Normal;
            foreach (var childDir in baseDir.GetDirectories())
                DeleteDirectory(childDir);

            foreach (var file in baseDir.GetFiles())
                file.IsReadOnly = false;

            baseDir.Delete(true);
        }

        private static void RunNpm(string args) =>
            Run("npm", args, windowsName: "cmd.exe", windowsArgs: $"/c npm {args}");

        private static void RunTests(string projectName, string directoryName = "src")
        {
            Run("dotnet", $"test -f net6.0 --no-build --no-restore {directoryName}/{projectName}/{projectName}.csproj");
        }

        private static string GetEnvironmentVariable(string variableName)
        {
            var val = Environment.GetEnvironmentVariable(variableName);

            // Azure devops converts environment variable to upper case and dot to underscore
            // https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=azure-devops&tabs=yaml%2Cbatch
            // Attempt to fetch variable by updating it
            if (string.IsNullOrEmpty(val))
            {
                val = Environment.GetEnvironmentVariable(variableName.ToUpper().Replace(".", "_"));
            }

            Console.WriteLine(val);

            return val;
        }

        private static string GetFramework()
        {
            var frameworkName = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;
            var version = float.Parse(frameworkName.Split('=')[1].Replace("v",""), InvariantCulture.NumberFormat);

            return version < 5.0 ? $"netcoreapp{version.ToString("N1", InvariantCulture.NumberFormat)}" : $"net{version.ToString("N1", InvariantCulture.NumberFormat)}";
        }

        private static Action IgnoreIfFailed(Action action)
        {
            return () =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            };
        }
    }
}
