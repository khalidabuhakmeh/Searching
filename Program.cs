using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Lunr;
using Spectre.Console;

var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = "|",
    HasHeaderRecord = true,
    MissingFieldFound = null
};
using var reader = new StreamReader("us_cities.csv");
using var csv = new CsvHelper.CsvReader(reader, config);

// our database
var cities = new Dictionary<string, City>();

// let's build our search index
Lunr.Index index = null;
var progress = AnsiConsole
    .Progress()
    .Columns(
        new SpinnerColumn(),
        new TaskDescriptionColumn(),
        new ElapsedTimeColumn()
    )
    .AutoClear(true);

const string indexName = "local.index.json";
await progress.StartAsync(async ctx =>
{
    var loadCities = ctx.AddTask("[green]loading cities...[/]");
    loadCities.StartTask();
    
    var count = 0;
    await foreach (var city in csv.GetRecordsAsync<City>())
    {
        // assumes each row is unique
        city.Id = $"{++count}";
        // add to our database
        cities.Add(city.Id, city);
    }

    loadCities.Description = $"✅ {loadCities.Description}";
    loadCities.StopTask();

    if (File.Exists(indexName))
    {
        var load = ctx.AddTask("[green]loading index from disk...[/]");
        load.StartTask();
        var json = await File.ReadAllTextAsync(indexName);
        index = Lunr.Index.LoadFromJson(json);
        
        load.Description = $"✅ {load.Description}";
        load.StopTask();
    }
    else
    {
        var build = ctx.AddTask("[green]building index...[/]");
        build.StartTask();
        
        index = await Lunr.Index.Build(async builder =>
        {
            foreach (var field in City.Fields)
                builder.AddField(field);

            foreach (var (id, city) in cities)
            {
                await builder.Add(city.ToDocument());
            }
        });

        await using var file = File.OpenWrite(indexName);
        await index.SaveToJsonStream(file);
        build.Description = $"✅ {build.Description}";
        build.StopTask();
    }
});
var running = true;
Console.CancelKeyPress += (_, _) => running = false;
while (running)
{
    Console.Write("Search : ");
    var search = Console.ReadLine();

    var table = new Table()
        .Title($":magnifying_glass_tilted_left: Search Results for \"{search}\"")
        .BorderStyle(new Style(foreground: Color.NavajoWhite1, decoration: Decoration.Italic))
        .AddColumn("name")
        .AddColumn("county")
        .AddColumn("state")
        .AddColumn("alias")
        .AddColumn("score");

    await foreach (var result in index.Search(search ?? string.Empty).Take(10))
    {
        var city = cities[result.DocumentReference];
        table.AddRow(
            city.Name,
            city.County,
            city.StateAbbreviation,
            city.Alias,
            $"{result.Score:F3}"
        );
    }

    AnsiConsole.Render(table);
}

public class City
{
    // Headers:
    // City|State short|State full|County|City alias
    [CsvHelper.Configuration.Attributes.Ignore] public string Id { get; set; }
    [CsvHelper.Configuration.Attributes.Name("City")] public string Name { get; set; }
    [CsvHelper.Configuration.Attributes.Name("State full")] public string StateName { get; set; }
    [CsvHelper.Configuration.Attributes.Name("State short")] public string StateAbbreviation { get; set; }
    [CsvHelper.Configuration.Attributes.Name("County")] public string County { get; set; }
    [CsvHelper.Configuration.Attributes.Name("City alias")] public string Alias { get; set; }

    public Document ToDocument()
    {
        return new(new Dictionary<string, object>
        {
            {"id", Id},
            {nameof(Name), Name},
            {nameof(StateName), StateName},
            {nameof(StateAbbreviation), StateAbbreviation},
            {nameof(County), County},
            {nameof(Alias), Alias}
        });
    }

    public static IEnumerable<string> Fields => new[]
    {
        nameof(Name),
        nameof(StateName),
        nameof(StateAbbreviation),
        nameof(County),
        nameof(Alias)
    }.ToList().AsReadOnly();
}