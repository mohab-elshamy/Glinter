import { useState } from "react";
import { FileJson, Upload } from "lucide-react";
import { toast } from "sonner";
import { staysApi } from "@/shared/services/api-stays";
import type { ImportStaysResult } from "@/shared/types/api";

const StayImportAdminPanel = () => {
  const [file, setFile] = useState<File>();
  const [result, setResult] = useState<ImportStaysResult>();
  const [importing, setImporting] = useState(false);

  const importFile = async () => {
    if (!file) return;
    setImporting(true);
    try {
      const response = await staysApi.importStays(file);
      setResult(response);
      toast.success("Stay import completed.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not import stays.");
    } finally {
      setImporting(false);
    }
  };

  return (
    <section className="card-glass p-6">
      <div className="mb-5 flex items-start gap-3">
        <FileJson className="mt-0.5 h-6 w-6 text-accent" />
        <div>
          <h2 className="font-semibold">Third-party stay import</h2>
          <p className="mt-1 text-xs text-muted-foreground">
            Upload a JSON array using the backend import contract. Existing records are matched and updated where possible.
          </p>
        </div>
      </div>
      <label className="flex cursor-pointer items-center justify-center rounded-xl border border-dashed border-border p-8 text-center">
        <span>
          <Upload className="mx-auto mb-2 h-5 w-5 text-accent" />
          <strong className="block text-sm">{file?.name || "Choose a stays JSON file"}</strong>
          <small className="text-muted-foreground">JSON arrays only</small>
        </span>
        <input
          type="file"
          accept="application/json,.json"
          className="sr-only"
          onChange={(event) => {
            setFile(event.target.files?.[0]);
            setResult(undefined);
          }}
        />
      </label>
      <button
        type="button"
        disabled={!file || importing}
        onClick={() => void importFile()}
        className="btn-accent mt-4 rounded-lg px-5 py-2 text-sm disabled:opacity-50"
      >
        {importing ? "Importing…" : "Import stays"}
      </button>
      {result && (
        <div className="mt-5 grid grid-cols-3 gap-3">
          <Metric label="Created" value={result.created} />
          <Metric label="Updated" value={result.updated} />
          <Metric label="Skipped" value={result.skipped} />
        </div>
      )}
      <p className="mt-5 text-xs text-muted-foreground">
        The LLM-review endpoint remains an internal, admin-protected machine payload because it does not perform moderation or generate a review summary by itself.
      </p>
    </section>
  );
};

const Metric = ({ label, value }: { label: string; value: number }) => (
  <div className="rounded-xl bg-secondary/40 p-4 text-center">
    <strong className="block text-2xl">{value}</strong>
    <span className="text-xs text-muted-foreground">{label}</span>
  </div>
);

export default StayImportAdminPanel;
