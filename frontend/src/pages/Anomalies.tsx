import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { api, type AnomalyItem } from "../api";
import { formatDate, flagTypeLabel, flagTypeExplanation } from "../format";
import SeverityBadge from "../components/SeverityBadge";

const PAGE_SIZE = 20;

const FLAG_TYPES = ["ValueOverrun", "PossibleSplitting", "SupplierDominance"];

export default function Anomalies() {
  const [params, setParams] = useSearchParams();
  const [result, setResult] = useState<{ items: AnomalyItem[]; total: number } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const flagType = params.get("tip") ?? "";
  const page = Number(params.get("page") ?? "1");

  useEffect(() => {
    api
      .anomalies({ flagType: flagType || undefined, page, pageSize: PAGE_SIZE })
      .then(setResult)
      .catch((e) => setError(e.message));
  }, [flagType, page]);

  function selectType(t: string) {
    const next = new URLSearchParams();
    if (t) next.set("tip", t);
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
    <div className="page-anomalies">
      <h1>Anomalii detectate</h1>
      <p className="page-intro">
        Tipare statistice neobisnuite gasite automat in datele de achizitii publice. O anomalie nu inseamna
        automat frauda — inseamna ca merita o verificare suplimentara.
      </p>

      <div className="tab-toggle">
        <button className={flagType === "" ? "active" : ""} onClick={() => selectType("")}>
          Toate
        </button>
        {FLAG_TYPES.map((t) => (
          <button key={t} className={flagType === t ? "active" : ""} onClick={() => selectType(t)}>
            {flagTypeLabel(t)}
          </button>
        ))}
      </div>

      {flagType && <p className="flag-explanation">{flagTypeExplanation(flagType)}</p>}

      {error && <div className="notice notice-error">Eroare: {error}</div>}

      <ul className="plain-list">
        {result === null && !error && Array.from({ length: 8 }).map((_, i) => <li key={i} className="skeleton skeleton-line" />)}
        {result?.items.map((a) => (
          <li key={a.id} className="anomaly-item anomaly-item-wide">
            <div>
              <SeverityBadge severity={a.severity} /> <strong>{flagTypeLabel(a.flagType)}</strong>
              <span className="dim-text anomaly-date"> &middot; {formatDate(a.detectedAt)}</span>
            </div>
            <p>{a.description.replace(/^\[dedupe:[^\]]+\]\s*/, "")}</p>
            {(a.authority || a.contract) && <div className="dim-text">{a.authority ?? a.contract}</div>}
          </li>
        ))}
        {result && result.items.length === 0 && <li className="notice">Nicio anomalie de acest tip.</li>}
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
