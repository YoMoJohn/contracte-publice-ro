export function formatMoney(value: number, currency = "RON"): string {
  return new Intl.NumberFormat("ro-RO", { maximumFractionDigits: 0 }).format(value) + " " + currency;
}

export function formatMoneyShort(value: number, currency = "RON"): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1).replace(".0", "")} mil. ${currency}`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(0)} mii ${currency}`;
  return formatMoney(value, currency);
}

export function formatDate(value: string | null): string {
  if (!value) return "—";
  return new Intl.DateTimeFormat("ro-RO", { day: "2-digit", month: "long", year: "numeric" }).format(new Date(value));
}

const CONTRACT_TYPES: Record<number, string> = {
  0: "Lucrari",
  1: "Servicii",
  2: "Produse",
};

const AWARD_PROCEDURES: Record<number, string> = {
  0: "Licitatie deschisa",
  1: "Licitatie restransa",
  2: "Procedura simplificata",
  3: "Achizitie directa",
  4: "Negociere",
  5: "Alta procedura",
};

export function contractTypeLabel(v: number): string {
  return CONTRACT_TYPES[v] ?? "necunoscut";
}

export function awardProcedureLabel(v: number): string {
  return AWARD_PROCEDURES[v] ?? "necunoscuta";
}

const FLAG_LABELS: Record<string, string> = {
  ValueOverrun: "Depasire de pret",
  PossibleSplitting: "Posibila fragmentare a contractului",
  SupplierDominance: "Concentrare mare la un singur furnizor",
};

const FLAG_EXPLANATIONS: Record<string, string> = {
  ValueOverrun: "Suma platita efectiv a depasit cu mult estimarea initiala a autoritatii.",
  PossibleSplitting:
    "Aceeasi autoritate a impartit achizitii catre acelasi furnizor in transe mici, posibil pentru a evita o licitatie publica obligatorie peste un anumit prag valoric.",
  SupplierDominance:
    "Un singur furnizor ia o parte foarte mare din banii cheltuiti de aceasta autoritate — semn posibil de lipsa de concurenta.",
};

export function flagTypeLabel(v: string): string {
  return FLAG_LABELS[v] ?? v;
}

export function flagTypeExplanation(v: string): string {
  return FLAG_EXPLANATIONS[v] ?? "";
}

const SEVERITY_LABELS: Record<string, string> = {
  Critical: "CRITIC",
  Warning: "ATENTIE",
  Info: "INFO",
};

export function severityLabel(v: string): string {
  return SEVERITY_LABELS[v] ?? v.toUpperCase();
}
