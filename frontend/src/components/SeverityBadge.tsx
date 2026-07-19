import { severityLabel } from "../format";

export default function SeverityBadge({ severity }: { severity: string }) {
  const cls =
    severity === "Critical" ? "sev sev-critical" : severity === "Warning" ? "sev sev-warning" : "sev sev-info";
  return <span className={cls}>[{severityLabel(severity)}]</span>;
}
