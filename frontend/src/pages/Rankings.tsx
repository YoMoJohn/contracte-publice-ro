import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, type RankingStat } from "../api";
import { formatMoney } from "../format";

export default function Rankings() {
  const [tab, setTab] = useState<"suppliers" | "authorities">("suppliers");
  const [data, setData] = useState<RankingStat[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setData(null);
    const fetcher = tab === "suppliers" ? api.topSuppliers(30) : api.topAuthorities(30);
    fetcher.then(setData).catch((e) => setError(e.message));
  }, [tab]);

  const max = data && data.length > 0 ? data[0].totalValue : 1;

  return (
    <div className="page-rankings">
      <h1>Clasamente</h1>
      <p className="page-intro">
        Cine incaseaza cei mai multi bani publici si cine ii cheltuie. Clasamentele se bazeaza pe suma valorilor
        atribuite ale contractelor din baza de date.
      </p>

      <div className="tab-toggle">
        <button className={tab === "suppliers" ? "active" : ""} onClick={() => setTab("suppliers")}>
          Top furnizori
        </button>
        <button className={tab === "authorities" ? "active" : ""} onClick={() => setTab("authorities")}>
          Top autoritati contractante
        </button>
      </div>

      {error && <div className="notice notice-error">Eroare: {error}</div>}

      <table className="data-table">
        <thead>
          <tr>
            <th>#</th>
            <th>{tab === "suppliers" ? "Furnizor" : "Autoritate"}</th>
            <th>Contracte</th>
            <th>Valoare totala</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {data === null &&
            !error &&
            Array.from({ length: 10 }).map((_, i) => (
              <tr key={i}><td colSpan={5}><div className="skeleton skeleton-line" /></td></tr>
            ))}
          {data?.map((item, i) => {
            const name = item.supplier ?? item.authority ?? "necunoscut";
            return (
              <tr key={name + i}>
                <td className="rank-cell">{i + 1}</td>
                <td>
                  <Link to={`/cauta?${tab === "suppliers" ? "supplier" : "authority"}=${encodeURIComponent(name)}`}>
                    {name}
                  </Link>
                </td>
                <td>{item.count.toLocaleString("ro-RO")}</td>
                <td>{formatMoney(item.totalValue)}</td>
                <td className="bar-cell">
                  <div className="bar-track">
                    <div className="bar-fill" style={{ width: `${(item.totalValue / max) * 100}%` }} />
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
