const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5010";

export interface ContractSummary {
  id: number;
  seapId: string;
  title: string;
  cpvCode: string;
  cpvDescription: string;
  awardedValue: number;
  estimatedValue: number;
  currency: string;
  publishedAt: string;
  county: string;
  contractType: number;
  awardProcedure: number;
  reportSource: string;
  authority: string;
  supplier: string | null;
}

export interface RelatedContract {
  id: number;
  title: string;
  awardedValue: number;
  currency: string;
  publishedAt: string;
}

export interface ContractDetail extends Omit<ContractSummary, "authority" | "supplier"> {
  awardedAt: string | null;
  contractStartDate: string | null;
  contractEndDate: string | null;
  contractNumber: string | null;
  minValue: number | null;
  maxValue: number | null;
  euFunded: boolean | null;
  fundingType: string | null;
  authority: {
    id: number;
    name: string;
    cui: string;
    county: string;
    totalContracts: number;
    totalValue: number;
    otherContracts: RelatedContract[];
  };
  supplier: {
    id: number;
    name: string;
    cui: string;
    county: string;
    totalContracts: number;
    totalValue: number;
    otherContracts: RelatedContract[];
  } | null;
  anomalies: { flagType: string; severity: string; description: string }[];
}

export interface PagedResult<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

export interface AnomalyItem {
  id: number;
  flagType: string;
  severity: string;
  description: string;
  detectedAt: string;
  contract: string | null;
  authority: string | null;
}

export interface NewsArticle {
  title: string;
  url: string;
  source: string | null;
  publishedAt: string | null;
}

export interface NewsSearchResult {
  configured: boolean;
  query: string;
  articles: NewsArticle[];
}

export interface CountyStat {
  county: string;
  count: number;
  totalValue: number;
}

export interface RankingStat {
  supplier?: string;
  authority?: string;
  count: number;
  totalValue: number;
}

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${API_URL}${path}`);
  if (!res.ok) throw new Error(`Cerere eșuată (${res.status}): ${path}`);
  return res.json() as Promise<T>;
}

export const api = {
  searchContracts: (params: {
    search?: string;
    county?: string;
    cpv?: string;
    authority?: string;
    supplier?: string;
    sort?: "date" | "value";
    page?: number;
    pageSize?: number;
  }) => {
    const q = new URLSearchParams();
    if (params.search) q.set("search", params.search);
    if (params.county) q.set("county", params.county);
    if (params.cpv) q.set("cpv", params.cpv);
    if (params.authority) q.set("authority", params.authority);
    if (params.supplier) q.set("supplier", params.supplier);
    if (params.sort) q.set("sort", params.sort);
    q.set("page", String(params.page ?? 1));
    q.set("pageSize", String(params.pageSize ?? 20));
    return get<PagedResult<ContractSummary>>(`/api/contracts?${q.toString()}`);
  },
  getContract: (id: number) => get<ContractDetail>(`/api/contracts/${id}`),
  getContractNews: (id: number) => get<NewsSearchResult>(`/api/contracts/${id}/news`),
  countyStats: () => get<CountyStat[]>("/api/contracts/stats/counties"),
  topSuppliers: (top = 20) => get<RankingStat[]>(`/api/contracts/stats/top-suppliers?top=${top}`),
  topAuthorities: (top = 20) => get<RankingStat[]>(`/api/contracts/stats/top-authorities?top=${top}`),
  anomalies: (params: { flagType?: string; severity?: string; page?: number; pageSize?: number }) => {
    const q = new URLSearchParams();
    if (params.flagType) q.set("flagType", params.flagType);
    if (params.severity) q.set("severity", params.severity);
    q.set("page", String(params.page ?? 1));
    q.set("pageSize", String(params.pageSize ?? 20));
    return get<PagedResult<AnomalyItem>>(`/api/anomalies?${q.toString()}`);
  },
};
