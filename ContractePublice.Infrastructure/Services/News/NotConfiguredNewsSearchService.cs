namespace ContractePublice.Infrastructure.Services.News;

public class NotConfiguredNewsSearchService : INewsSearchService
{
    public Task<NewsSearchResult> SearchAsync(string query, CancellationToken ct) =>
        Task.FromResult(new NewsSearchResult(Configured: false, Query: query, Articles: new List<NewsArticle>()));
}
