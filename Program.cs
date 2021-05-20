using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Lunr;
using Spectre.Console;

// our database
var cities = new Dictionary<string, City>();
var status = AnsiConsole.Status().Spinner(Spinner.Known.Earth).AutoRefresh(true);

// let's build our search index
Lunr.Index index = null;
const string indexName = "local.index.json";
await status.StartAsync("Thinking...", async ctx =>
{
    ctx.Status("[green]loading cities...[/]");
    var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
        Delimiter = "|",
        HasHeaderRecord = true,
        MissingFieldFound = null
    };
    using var reader = new StreamReader("us_cities.csv");
    using var csv = new CsvReader(reader, config);
    
    await foreach (var city in csv.GetRecordsAsync<City>().Select((city, id) => city.WithId(id)))
    {
        cities.Add(city.Id, city);
    }

    if (File.Exists(indexName))
    {
        ctx.Status("[green]loading index from disk...[/]");
        var json = await File.ReadAllTextAsync(indexName);
        index = Lunr.Index.LoadFromJson(json);
    }
    else
    {
        ctx.Status("[green]building index...[/]");
        
        index = await Lunr.Index.Build(async builder =>
        {
            foreach (var field in City.Fields)
                builder.AddField(field);

            foreach (var (_, city) in cities)
            {
                await builder.Add(city.ToDocument());
            }
        });

        await using var file = File.OpenWrite(indexName);
        await index.SaveToJsonStream(file);
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
    [Ignore] public string Id { get; set; }
    [Name("City")] public string Name { get; set; }
    [Name("State full")] public string StateName { get; set; }
    [Name("State short")] public string StateAbbreviation { get; set; }
    [Name("County")] public string County { get; set; }
    [Name("City alias")] public string Alias { get; set; }
    
    public City WithId(int id)
    {
        Id = id.ToString();
        return this;
    }

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