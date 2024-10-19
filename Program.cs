using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Xml.Linq;

class Program
{
    static void Main(string[] args)
    {
        var rootCommand = new RootCommand
        {
            new Option<string[]>(
                "--project-locations",
                "Paths to the project files to process")
        };

        rootCommand.Description = "Automate moving NuGet package references to Directory.Packages.props";

        rootCommand.Handler = CommandHandler.Create<string[]>((projectLocations) =>
        {
            if (projectLocations == null || projectLocations.Length == 0)
            {
                Console.WriteLine("Please provide at least one project location.");
                return;
            }

            var propsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Directory.Packages.props");
            var propsFile = new XDocument(
                new XElement("Project",
                    new XElement("PropertyGroup",
                        new XElement("ManagePackageVersionsCentrally", "true")),
                    new XElement("ItemGroup")
                )
            );

            foreach (var projectLocation in projectLocations)
            {
                if (!File.Exists(projectLocation))
                {
                    Console.WriteLine($"Project file not found: {projectLocation}");
                    continue;
                }

                var projectDoc = XDocument.Load(projectLocation);
                var packageReferences = projectDoc.Descendants("PackageReference").ToList();

                foreach (var packageReference in packageReferences)
                {
                    var include = packageReference.Attribute("Include")?.Value;
                    var version = packageReference.Attribute("Version")?.Value;

                    if (!string.IsNullOrEmpty(include) && !string.IsNullOrEmpty(version))
                    {
                        propsFile.Root?.Element("ItemGroup")?.Add(
                            new XElement("PackageVersion",
                                new XAttribute("Include", include),
                                new XAttribute("Version", version))
                        );

                        packageReference.Attribute("Version")?.Remove();
                    }
                }

                projectDoc.Save(projectLocation);
            }

            propsFile.Save(propsFilePath);
            Console.WriteLine("Package references have been moved to Directory.Packages.props");
        });

        rootCommand.InvokeAsync(args).Wait();
    }
}
