import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, type ContractDetail as ContractDetailType, type NewsSearchResult } from "../api";
import { formatMoney, formatDate, contractTypeLabel, awardProcedureLabel, flagTypeLabel } from "../format";
import SeverityBadge from "../components/SeverityBadge";

export default function ContractDetail() {
  const { id } = useParams();
  const [contract, setContract] = useState<ContractDetailType | null>(null);
  const [news, setNews] = useState<NewsSearchResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setContract(null);
    setNews(null);
    setError(null);
    api.getContract(Number(id)).then(setContract).catch((e) => setError(e.message));
    api.getContractNews(Number(id)).then(setNews).catch(() => setNews(null));
  }, [id]);

  if (error) return <div className="notice notice-error">Nu am gasit acest contract: {error}</div>;
  if (!contract) return <div className="skeleton skeleton-line" style={{ height: 400 }} />;

  const overrun =
    contract.estimatedValue > 0 ? ((contract.awardedValue / contract.estimatedValue - 1) * 100).toFixed(0) : null;
  const authorityShare =
    contract.authority.totalValue > 0 ? ((contract.awardedValue / contract.authority.totalValue) * 100).toFixed(2) : null;
  const supplierShare =
    contract.supplier && contract.supplier.totalValue > 0
      ? ((contract.awardedValue / contract.supplier.totalValue) * 100).toFixed(2)
      : null;

  return (
    <article className="page-contract-detail">
      <Link to="/cauta" className="back-link">
        &larr; Inapoi la cautare
      </Link>
      <h1>{contract.title}</h1>
      <div className="detail-tags">
        <span className="tag">{contractTypeLabel(contract.contractType)}</span>
        <span className="tag">{awardProcedureLabel(contract.awardProcedure)}</span>
        <span className="tag">{contract.county || "judet necunoscut"}</span>
        <span className="dim-text">SEAP: {contract.seapId}</span>
      </div>

      {contract.anomalies.length > 0 && (
        <div className="anomaly-callout">
          <strong>{contract.anomalies.length} anomalie(i) detectata(e) pe acest contract</strong>
          <ul>
            {contract.anomalies.map((a, i) => (
              <li key={i}>
                <SeverityBadge severity={a.severity} /> <strong>{flagTypeLabel(a.flagType)}</strong> &mdash;{" "}
                {a.description.replace(/^\[dedupe:[^\]]+\]\s*/, "")}
              </li>
            ))}
          </ul>
        </div>
      )}

      <table className="fact-table">
        <tbody>
          <tr>
            <th>Valoare atribuita</th>
            <td className="fact-value">{formatMoney(contract.awardedValue, contract.currency)}</td>
          </tr>
          <tr>
            <th>Valoare estimata</th>
            <td>
              {formatMoney(contract.estimatedValue, contract.currency)}
              {overrun && Number(overrun) > 0 && <span className="warn-text"> (+{overrun}% fata de estimare)</span>}
            </td>
          </tr>
          {(contract.minValue || contract.maxValue) && (
            <tr>
              <th>Interval valoare contract</th>
              <td>
                {contract.minValue ? formatMoney(contract.minValue, contract.currency) : "—"}
                {" – "}
                {contract.maxValue ? formatMoney(contract.maxValue, contract.currency) : "—"}
              </td>
            </tr>
          )}
          <tr>
            <th>Tip achizitie</th>
            <td>{contractTypeLabel(contract.contractType)} &middot; {awardProcedureLabel(contract.awardProcedure)}</td>
          </tr>
          <tr>
            <th>Cod CPV</th>
            <td>{contract.cpvCode || "—"}</td>
          </tr>
          {contract.contractNumber && (
            <tr>
              <th>Numar contract</th>
              <td>{contract.contractNumber}</td>
            </tr>
          )}
          <tr>
            <th>Data publicarii</th>
            <td>{formatDate(contract.publishedAt)}</td>
          </tr>
          <tr>
            <th>Data finalizarii</th>
            <td>{formatDate(contract.awardedAt)}</td>
          </tr>
          {contract.euFunded !== null && (
            <tr>
              <th>Finantare</th>
              <td>
                {contract.euFunded ? "Cu fonduri europene" : "Fara fonduri europene"}
                {contract.fundingType && <span className="dim-text"> &middot; {contract.fundingType}</span>}
              </td>
            </tr>
          )}
          <tr>
            <th>Sursa raport</th>
            <td className="dim-text">
              {contract.reportSource === "Contract" ? "Raport Contracte (SICAP)" : "Raport Achizitii Directe"}
            </td>
          </tr>
        </tbody>
      </table>

      {contract.cpvDescription && (
        <div className="detail-block">
          <h2 className="section-title">Descriere achizitie</h2>
          <p className="detail-description">{contract.cpvDescription}</p>
        </div>
      )}

      <div className="detail-block">
        <h2 className="section-title">Autoritatea contractanta</h2>
        <table className="fact-table">
          <tbody>
            <tr>
              <th>Denumire</th>
              <td>
                <Link to={`/cauta?authority=${encodeURIComponent(contract.authority.name)}`}>
                  {contract.authority.name}
                </Link>
              </td>
            </tr>
            <tr>
              <th>CUI</th>
              <td>{contract.authority.cui || "—"}</td>
            </tr>
            <tr>
              <th>Judet</th>
              <td>{contract.authority.county || "—"}</td>
            </tr>
            <tr>
              <th>Total contracte in baza de date</th>
              <td>
                {contract.authority.totalContracts.toLocaleString("ro-RO")} contracte, in valoare de{" "}
                {formatMoney(contract.authority.totalValue)}
                {authorityShare && (
                  <span className="dim-text"> (acest contract reprezinta {authorityShare}% din total)</span>
                )}
              </td>
            </tr>
          </tbody>
        </table>
        {contract.authority.otherContracts.length > 0 && (
          <>
            <div className="dim-text detail-sublabel">Alte contracte mari ale aceleiasi autoritati:</div>
            <ul className="plain-list plain-list-compact">
              {contract.authority.otherContracts.map((c) => (
                <li key={c.id}>
                  <Link to={`/contract/${c.id}`}>{c.title}</Link>
                  <span className="dim-text"> &middot; {formatMoney(c.awardedValue, c.currency)} &middot; {formatDate(c.publishedAt)}</span>
                </li>
              ))}
            </ul>
          </>
        )}
      </div>

      {contract.supplier ? (
        <div className="detail-block">
          <h2 className="section-title">Furnizorul castigator</h2>
          <table className="fact-table">
            <tbody>
              <tr>
                <th>Denumire</th>
                <td>
                  <Link to={`/cauta?supplier=${encodeURIComponent(contract.supplier.name)}`}>
                    {contract.supplier.name}
                  </Link>
                </td>
              </tr>
              <tr>
                <th>CUI</th>
                <td>{contract.supplier.cui || "—"}</td>
              </tr>
              <tr>
                <th>Judet</th>
                <td>{contract.supplier.county || "—"}</td>
              </tr>
              <tr>
                <th>Total contracte castigate in baza de date</th>
                <td>
                  {contract.supplier.totalContracts.toLocaleString("ro-RO")} contracte, in valoare de{" "}
                  {formatMoney(contract.supplier.totalValue)}
                  {supplierShare && (
                    <span className="dim-text"> (acest contract reprezinta {supplierShare}% din total)</span>
                  )}
                </td>
              </tr>
            </tbody>
          </table>
          {contract.supplier.otherContracts.length > 0 && (
            <>
              <div className="dim-text detail-sublabel">Alte contracte mari castigate de acelasi furnizor:</div>
              <ul className="plain-list plain-list-compact">
                {contract.supplier.otherContracts.map((c) => (
                  <li key={c.id}>
                    <Link to={`/contract/${c.id}`}>{c.title}</Link>
                    <span className="dim-text"> &middot; {formatMoney(c.awardedValue, c.currency)} &middot; {formatDate(c.publishedAt)}</span>
                  </li>
                ))}
              </ul>
            </>
          )}
        </div>
      ) : (
        <div className="detail-block dim-text">Furnizor nespecificat in sursa de date.</div>
      )}

      <div className="detail-block">
        <h2 className="section-title">Stiri legate de acest contract</h2>
        {news === null && <div className="dim-text">Se cauta...</div>}
        {news && !news.configured && (
          <div className="dim-text">
            Cautarea automata de stiri nu este inca activata pe acest site. Cand va fi disponibila,
            aici vor aparea articole reale care mentioneaza autoritatea sau furnizorul de mai sus.
          </div>
        )}
        {news && news.configured && news.articles.length === 0 && (
          <div className="dim-text">Nu am gasit stiri legate de acest contract.</div>
        )}
        {news && news.configured && news.articles.length > 0 && (
          <ul className="plain-list plain-list-compact">
            {news.articles.map((a, i) => (
              <li key={i}>
                <a href={a.url} target="_blank" rel="noopener noreferrer">
                  {a.title}
                </a>
                {a.source && <span className="dim-text"> &middot; {a.source}</span>}
              </li>
            ))}
          </ul>
        )}
      </div>
    </article>
  );
}
