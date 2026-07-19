import { Routes, Route } from "react-router-dom";
import Layout from "./components/Layout";
import Home from "./pages/Home";
import Search from "./pages/Search";
import Rankings from "./pages/Rankings";
import Anomalies from "./pages/Anomalies";
import ContractDetail from "./pages/ContractDetail";

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Home />} />
        <Route path="cauta" element={<Search />} />
        <Route path="clasamente" element={<Rankings />} />
        <Route path="anomalii" element={<Anomalies />} />
        <Route path="contract/:id" element={<ContractDetail />} />
      </Route>
    </Routes>
  );
}
