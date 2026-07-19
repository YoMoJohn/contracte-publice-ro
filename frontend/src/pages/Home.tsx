import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, type ContractSummary, type AnomalyItem, type CountyStat } from "../api";
import { formatMoney, flagTypeLabel } from "../format";
import SeverityBadge from "../components/SeverityBadge";

export default function Home() {
  const [bigContracts, setBigContracts] = useState<ContractSummary[] | null>(null);
  const [anomalies, setAnomalies] = useState<AnomalyItem[] | null>(null);
  const [counties, setCounties] = useState<CountyStat[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      api.searchContracts({ sort: "value", pageSize: 12 }),
      api.anomalies({ pageSize: 8 }),
      api.countyStats(),
    ])
      .then(([contracts, anom, cty]) => {
        setBigContracts(contracts.items);
        setAnomalies(anom.items);
        setCounties(cty.slice(0, 10));
      })
      .catch((e) => setError(e.message));
  }, []);

  const totalNational = counties?.reduce((s, c) => s + c.totalValue, 0) ?? 0;

  if (error) return <div className="notice notice-error">Nu am putut incarca datele: {error}</div>;

  return (
    <div className="page-home">
      <section className="hero">
        <h1>Banii publici, urmariti in timp real</h1>
        <p>
          Date agregate automat din achizitiile publice raportate oficial pe data.gov.ro. Fara termeni de
          contabilitate, fara interpretari — doar cifrele, sursa lor si tiparele care merita verificate.
        </p>
        {counties && (
          <p className="hero-stat">
            Total urmarit: <strong>{formatMoney(totalNational)}</strong>
          </p>
        )}
      </section>

      <div className="two-col">
        <section className="section">
          <h2 className="section-title">Cele mai mari contracte</h2>
          <ol className="headline-list">
            {bigContracts === null && !error && Array.from({ length: 8 }).map((_, i) => <li key={i} className="skeleton skeleton-line" />)}
            {bigContracts?.map((c, i) => (
              <li key={c.id} className="headline-item">
                <span className="headline-rank">{i + 1}.</span>
                <div className="headline-body">
                  <Link to={`/contract/${c.id}`} className="headline-title">
                    {c.title}
                  </Link>
                  <div className="headline-meta">
                    {c.authority} &middot; {c.county || "judet necunoscut"} &middot;{" "}
                    <span className="headline-value">{formatMoney(c.awardedValue, c.currency)}</span>
                  </div>
                </div>
              </li>
            ))}
          </ol>
          <Link to="/cauta?sort=value" className="see-all">
            Vezi toate contractele &rarr;
          </Link>
        </section>

        <section className="section">
          <h2 className="section-title">Anomalii recente</h2>
          <ul className="plain-list">
            {anomalies === null && !error && Array.from({ length: 6 }).map((_, i) => <li key={i} className="skeleton skeleton-line" />)}
            {anomalies?.map((a) => (
              <li key={a.id} className="anomaly-item">
                <SeverityBadge severity={a.severity} /> <strong>{flagTypeLabel(a.flagType)}</strong>
                <p>{a.description.replace(/^\[dedupe:[^\]]+\]\s*/, "")}</p>
                {a.authority && <div className="dim-text">{a.authority}</div>}
              </li>
            ))}
          </ul>
          <Link to="/anomalii" className="see-all">
            Vezi toate anomaliile &rarr;
          </Link>
        </section>
      </div>

      <section className="section">
        <h2 className="section-title">Cheltuieli pe judet</h2>
        <table className="data-table">
          <thead>
            <tr>
              <th>Judet</th>
              <th>Contracte</th>
              <th>Valoare totala</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {counties === null && !error && Array.from({ length: 6 }).map((_, i) => (
              <tr key={i}><td colSpan={4}><div className="skeleton skeleton-line" /></td></tr>
            ))}
            {counties?.map((c) => (
              <tr key={c.county}>
                <td>{c.county || "necunoscut"}</td>
                <td>{c.count.toLocaleString("ro-RO")}</td>
                <td>{formatMoney(c.totalValue)}</td>
                <td className="bar-cell">
                  <div className="bar-track">
                    <div className="bar-fill" style={{ width: `${totalNational ? (c.totalValue / totalNational) * 100 : 0}%` }} />
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <Link to="/clasamente" className="see-all">
          Vezi clasamente complete &rarr;
        </Link>
      </section>
    </div>
  );
}
