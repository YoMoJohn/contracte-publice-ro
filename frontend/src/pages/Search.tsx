import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api, type ContractSummary } from "../api";
import { formatMoney, formatDate, contractTypeLabel } from "../format";

const PAGE_SIZE = 20;

export default function Search() {
  const [params, setParams] = useSearchParams();
  const [searchInput, setSearchInput] = useState(params.get("search") ?? "");
  const [countyInput, setCountyInput] = useState(params.get("county") ?? "");
  const [result, setResult] = useState<{ items: ContractSummary[]; total: number } | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const search = params.get("search") ?? "";
  const county = params.get("county") ?? "";
  const authority = params.get("authority") ?? "";
  const supplier = params.get("supplier") ?? "";
  const sort = (params.get("sort") as "date" | "value") ?? "date";
  const page = Number(params.get("page") ?? "1");

  useEffect(() => {
    setLoading(true);
    api
      .searchContracts({ search, county, authority, supplier, sort, page, pageSize: PAGE_SIZE })
      .then((r) => setResult(r))
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [search, county, authority, supplier, sort, page]);

  function applyFilters(e: React.FormEvent) {
    e.preventDefault();
    const next = new URLSearchParams();
    if (searchInput) next.set("search", searchInput);
    if (countyInput) next.set("county", countyInput);
    next.set("sort", sort);
    next.set("page", "1");
    setParams(next);
  }

  function changeSort(newSort: "date" | "value") {
    const next = new URLSearchParams(params);
    next.set("sort", newSort);
    next.set("page", "1");
    setParams(next);
  }

  function goToPage(p: number) {
    const next = new URLSearchParams(params);
    next.set("page", String(p));
    setParams(next);
  }

  const totalPages = result ? Math.max(1, Math.ceil(result.total / PAGE_SIZE)) : 1;

  return (
    <div className="page-search">
      <h1>Cauta contracte publice</h1>
      {(authority || supplier) && (
        <div className="filter-banner">
          Filtrat dupa {authority ? `autoritatea "${authority}"` : `furnizorul "${supplier}"`}
          {" "}
          <Link to="/cauta">(elimina filtrul)</Link>
        </div>
      )}
      <form className="search-form" onSubmit={applyFilters}>
        <input
          type="text"
          placeholder="Cuvant cheie (ex: reagenti, asfaltare, calculatoare...)"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
        />
        <input
          type="text"
          placeholder="Judet (ex: Cluj)"
          value={countyInput}
          onChange={(e) => setCountyInput(e.target.value)}
        />
        <button type="submit">Cauta</button>
      </form>

      <div className="search-toolbar">
        <div className="sort-toggle">
          <span>Sorteaza dupa:</span>
          <button className={sort === "date" ? "active" : ""} onClick={() => changeSort("date")}>
            Cele mai recente
          </button>
          <button className={sort === "value" ? "active" : ""} onClick={() => changeSort("value")}>
            Valoare
          </button>
        </div>
        {result && <div className="result-count">{result.total.toLocaleString("ro-RO")} contracte gasite</div>}
      </div>

      {error && <div className="notice notice-error">Eroare: {error}</div>}

      <ul className="contract-list">
        {loading && !result && Array.from({ length: 6 }).map((_, i) => <li key={i} className="skeleton skeleton-line" />)}
        {result?.items.map((c) => (
          <li key={c.id} className="contract-row">
            <Link to={`/contract/${c.id}`} className="contract-row-title">
              {c.title}
            </Link>
            <div className="contract-row-meta">
              <span>{c.authority}</span>
              {c.supplier && <span>&rarr; {c.supplier}</span>}
              <span>{c.county || "judet necunoscut"}</span>
              <span>{formatDate(c.publishedAt)}</span>
              <span className="tag">{contractTypeLabel(c.contractType)}</span>
            </div>
            <div className="contract-row-value">{formatMoney(c.awardedValue, c.currency)}</div>
          </li>
        ))}
        {result && result.items.length === 0 && <li className="notice">Niciun contract nu corespunde cautarii.</li>}
      </ul>

      {result && totalPages > 1 && (
        <div className="pagination">
          <button disabled={page <= 1} onClick={() => goToPage(page - 1)}>
            &larr; Anterior
          </button>
          <span>
            Pagina {page} din {totalPages}
          </span>
          <button disabled={page >= totalPages} onClick={() => goToPage(page + 1)}>
            Urmator &rarr;
          </button>
        </div>
      )}
    </div>
  );
}
