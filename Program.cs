using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string apiUrl = "https://api.breakingbadquotes.xyz/v1/quotes/";
        string outputFilePath = "GroupedQuotes.json";

        using HttpClient client = new HttpClient();

        var allValidQuotes = new List<BreakingBadQuote>();

        try
        {
            Console.WriteLine("Fetching records from API...");

            for (int i = 0; i < 172; i++)
            {
                Console.WriteLine($"Fetching records from API attempt #{i + 1}...");

                HttpResponseMessage response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine("Validating API response...");
                var quotes = JsonSerializer.Deserialize<List<BreakingBadQuote>>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (quotes == null || quotes.Count == 0)
                {
                    Console.WriteLine("No records found in the API response.");
                    continue;
                }

                Console.WriteLine($"Fetched {quotes.Count} records. Validating data structure...");

                var validQuotes = quotes.Where(q => !string.IsNullOrWhiteSpace(q.Author) && !string.IsNullOrWhiteSpace(q.Quote)).ToList();
                var invalidQuotes = quotes.Where(q => string.IsNullOrWhiteSpace(q.Author) || string.IsNullOrWhiteSpace(q.Quote)).ToList();

                Console.WriteLine($"Valid records: {validQuotes.Count}, Invalid records: {invalidQuotes.Count}");

                if (invalidQuotes.Any())
                {
                    Console.WriteLine("Logging invalid records for review...");
                    LogInvalidQuotes(invalidQuotes);
                }

                allValidQuotes.AddRange(validQuotes);
            }

            if (allValidQuotes.Any())
            {
                Console.WriteLine("Grouping records by author...");
                var groupedQuotes = allValidQuotes
                    .GroupBy(q => q.Author)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.ToList()); // Keep the BreakingBadQuote type

                Console.WriteLine($"Records grouped into {groupedQuotes.Count} authors.");

                Console.WriteLine("Saving grouped data into a JSON file...");
                SaveGroupedQuotesToFile(groupedQuotes, outputFilePath);

                Console.WriteLine($"Data saved successfully to {outputFilePath}.");
            }
            else
            {
                Console.WriteLine("No valid quotes were fetched.");
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Network error while fetching API data: {e.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }

    private static void LogInvalidQuotes(List<BreakingBadQuote> invalidQuotes)
    {
        foreach (var quote in invalidQuotes)
        {
            Console.WriteLine($"Invalid record: {JsonSerializer.Serialize(quote)}");
        }
    }

    private static void SaveGroupedQuotesToFile(Dictionary<string, List<BreakingBadQuote>> groupedQuotes, string filePath)
    {
        if (File.Exists(filePath))
        {
            Console.WriteLine("Output file already exists. Overwriting existing file.");
        }

        string jsonContent = JsonSerializer.Serialize(groupedQuotes, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, jsonContent);
    }
}