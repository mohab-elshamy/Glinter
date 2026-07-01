import { useState } from "react";
import { FileJson, Upload } from "lucide-react";
import { toast } from "sonner";
import { experiencesApi } from "@/shared/services/api-experiences";
import type {
  ExperienceCategory,
  ImportExperiencesResult,
} from "@/shared/types/api";

const categories: ExperienceCategory[] = [
  "Historical",
  "Nature",
  "Shopping",
  "Nightlife",
  "Dining",
];

const ExperienceImportAdminPanel = () => {
  const [category, setCategory] = useState<ExperienceCategory>("Historical");
  const [file, setFile] = useState<File>();
  const [result, setResult] = useState<ImportExperiencesResult>();
  const [importing, setImporting] = useState(false);

  const importFile = async () => {
    if (!file) return;
    setImporting(true);
    try {
      const response = await experiencesApi.importExperiences(category, file);
      setResult(response);
      toast.success("Experience import completed.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Could not import experiences.");
    } finally {
      setImporting(false);
    }
  };

  return (
    <section className="card-glass p-6">
      <div className="mb-5 flex items-start gap-3">
        <FileJson className="mt-0.5 h-6 w-6 text-accent" />
        <div>
          <h2 className="font-semibold">Third-party experience import</h2>
          <p className="mt-1 text-xs text-muted-foreground">
            Choose the category and upload a JSON array matching the backend import format.
          </p>
        </div>
      </div>
      <label className="mb-4 block text-xs">
        Experience category
        <select
          value={category}
          onChange={(event) => setCategory(event.target.value as ExperienceCategory)}
          className="input-glass mt-1 w-full"
        >
          {categories.map((item) => <option key={item}>{item}</option>)}
        </select>
      </label>
      <label className="flex cursor-pointer items-center justify-center rounded-xl border border-dashed border-border p-8 text-center">
        <span>
          <Upload className="mx-auto mb-2 h-5 w-5 text-accent" />
          <strong className="block text-sm">{file?.name || "Choose an experiences JSON file"}</strong>
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
        {importing ? "Importing…" : "Import experiences"}
      </button>
      {result && (
        <div className="mt-5 grid grid-cols-3 gap-3">
          <Metric label="Created" value={result.created} />
          <Metric label="Updated" value={result.updated} />
          <Metric label="Skipped" value={result.skipped} />
        </div>
      )}
      <p className="mt-5 text-xs text-muted-foreground">
        The raw LLM-review payload remains an internal, admin-only endpoint. It does not generate a summary or perform moderation, so a standalone UI would expose implementation data without a complete workflow.
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

export default ExperienceImportAdminPanel;
