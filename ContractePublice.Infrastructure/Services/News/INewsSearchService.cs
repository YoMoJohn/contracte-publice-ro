namespace ContractePublice.Infrastructure.Services.News;

public record NewsArticle(string Title, string Url, string? Source, string? PublishedAt);

public record NewsSearchResult(bool Configured, string Query, List<NewsArticle> Articles);

// Interfata pentru cautare de stiri legate de un contract (dupa numele autoritatii/furnizorului).
// Implementarea reala (Bing News API, Google Custom Search etc.) se conecteaza aici cand exista
// o cheie API disponibila — pana atunci, NotConfiguredNewsSearchService raspunde onest ca
// functia nu e inca activa, in loc sa inventeze rezultate.
public interface INewsSearchService
{
    Task<NewsSearchResult> SearchAsync(string query, CancellationToken ct);
}
