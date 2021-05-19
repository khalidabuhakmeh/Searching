## Using LunrCore To Search For Entities

### Getting Started

You'll need **[.NET 5](https://dot.net)**.

```console
> dotnet run
```

You'll see the `us_cities.csv` being indexed, and then you'll be able to search for a U.S. city. Try `harrisburg`, my hometown. You should see the following output.

```console
Search : harrisburg
          🔍 Search Results for "harrisburg"           
┌────────────┬──────────┬───────┬────────────┬────────┐
│ name       │ county   │ state │ alias      │ score  │
├────────────┼──────────┼───────┼────────────┼────────┤
│ Harrisburg │ DAUPHIN  │ PA    │ Harrisburg │ 15.955 │
│ Harrisburg │ CABARRUS │ NC    │ Harrisburg │ 15.955 │
│ Harrisburg │ FRANKLIN │ OH    │ Harrisburg │ 15.955 │
│ Harrisburg │ LINCOLN  │ SD    │ Harrisburg │ 15.955 │
│ Harrisburg │ SALINE   │ IL    │ Harrisburg │ 15.955 │
│ Harrisburg │ BOONE    │ MO    │ Harrisburg │ 15.955 │
│ Harrisburg │ BANNER   │ NE    │ Harrisburg │ 15.955 │
│ Harrisburg │ POINSETT │ AR    │ Harrisburg │ 15.955 │
│ Harrisburg │ LINN     │ OR    │ Harrisburg │ 15.955 │
│ Lowville   │ LEWIS    │ NY    │ Harrisburg │ 8.170  │
└────────────┴──────────┴───────┴────────────┴────────┘
Search : 

```

## The Code

This code uses the following packages:

- CsvHelper
- LunrCore
- Spectre.Console
- System.Linq.Async

Look at [Program.cs](./Program.cs) to see how it is all implemented.

## License

MIT (don't sue me)