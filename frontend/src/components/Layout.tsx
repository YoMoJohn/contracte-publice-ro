import { NavLink, Outlet } from "react-router-dom";

export default function Layout() {
  return (
    <div className="app-shell">
      <header className="site-header">
        <div className="site-header-inner">
          <NavLink to="/" className="brand">
            Banii Publici
          </NavLink>
          <nav className="main-nav">
            <NavLink to="/" end>Prima pagina</NavLink>
            <NavLink to="/cauta">Cauta contracte</NavLink>
            <NavLink to="/clasamente">Clasamente</NavLink>
            <NavLink to="/anomalii">Anomalii</NavLink>
          </nav>
        </div>
      </header>
      <main className="site-main">
        <Outlet />
      </main>
      <footer className="site-footer">
        <p>
          Date publice, agregate din rapoartele de achizitii directe publicate pe data.gov.ro.
          Anomaliile marcate sunt tipare statistice care merita verificate, nu acuzatii.
        </p>
      </footer>
    </div>
  );
}
