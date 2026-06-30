import { useCallback, useEffect, useState } from "react";
import { FileUp, MapPinned, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import RegionCascadeSelect from "@/components/RegionCascadeSelect";
import { regionsApi } from "@/shared/services/api-regions";
import type {
  CountryDto,
  DistrictDto,
  GovernorateDto,
  NeighbourhoodDto,
  RegionHierarchyGids,
  RegionLevel,
} from "@/shared/types/regions";

type RegionItem = CountryDto | GovernorateDto | DistrictDto | NeighbourhoodDto;

const levelLabels: Record<RegionLevel, string> = {
  adm0: "Countries (ADM0)",
  adm1: "Governorates (ADM1)",
  adm2: "Districts (ADM2)",
  adm3: "Neighbourhoods (ADM3)",
};

const message = (error: unknown) =>
  error instanceof Error ? error.message : "The region request failed.";

const RegionsAdminPanel = () => {
  const [level, setLevel] = useState<RegionLevel>("adm0");
  const [selection, setSelection] = useState<RegionHierarchyGids>({});
  const [items, setItems] = useState<RegionItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<RegionItem>();
  const [showForm, setShowForm] = useState(false);
  const [saving, setSaving] = useState(false);
  const [nameEn, setNameEn] = useState("");
  const [nameAr, setNameAr] = useState("");
  const [pcode, setPcode] = useState("");
  const [imageUrl, setImageUrl] = useState("");
  const [flagUrl, setFlagUrl] = useState("");
  const [importing, setImporting] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const loaded = level === "adm0"
        ? await regionsApi.getCountries()
        : level === "adm1"
          ? await regionsApi.getGovernorates(selection.adm0Gid)
          : level === "adm2"
            ? await regionsApi.getDistricts(selection.adm1Gid)
            : await regionsApi.getNeighbourhoods(selection.adm2Gid);
      setItems(loaded);
    } catch (error) {
      toast.error(message(error));
    } finally {
      setLoading(false);
    }
  }, [level, selection.adm0Gid, selection.adm1Gid, selection.adm2Gid]);

  useEffect(() => {
    void load();
  }, [load]);

  const openCreate = () => {
    setEditing(undefined);
    setNameEn("");
    setNameAr("");
    setPcode("");
    setImageUrl("");
    setFlagUrl("");
    setShowForm(true);
  };

  const openEdit = (item: RegionItem) => {
    setEditing(item);
    setNameEn(item.nameEn ?? "");
    setNameAr(item.nameAr ?? "");
    setPcode(item.pcode);
    setImageUrl(item.imageUrl ?? "");
    setFlagUrl("flagUrl" in item ? item.flagUrl ?? "" : "");
    setShowForm(true);
  };

  const save = async () => {
    if (!nameEn.trim() || (!editing && !pcode.trim())) {
      toast.error("English name and pcode are required.");
      return;
    }
    setSaving(true);
    const common = {
      nameEn: nameEn.trim(),
      nameAr: nameAr.trim() || undefined,
      imageUrl: imageUrl.trim() || undefined,
    };
    try {
      if (editing) {
        if (level === "adm0") {
          await regionsApi.updateCountry(editing.gid, { ...common, flagUrl: flagUrl.trim() || undefined });
        } else if (level === "adm1") {
          await regionsApi.updateGovernorate(editing.gid, common);
        } else if (level === "adm2") {
          await regionsApi.updateDistrict(editing.gid, common);
        } else {
          await regionsApi.updateNeighbourhood(editing.gid, common);
        }
      } else if (level === "adm0") {
        await regionsApi.createCountry({ ...common, pcode: pcode.trim(), flagUrl: flagUrl.trim() || undefined });
      } else if (level === "adm1" && selection.adm0Gid) {
        await regionsApi.createGovernorate({ ...common, pcode: pcode.trim(), adm0Gid: selection.adm0Gid });
      } else if (level === "adm2" && selection.adm1Gid) {
        await regionsApi.createDistrict({ ...common, pcode: pcode.trim(), adm1Gid: selection.adm1Gid });
      } else if (level === "adm3" && selection.adm2Gid) {
        await regionsApi.createNeighbourhood({ ...common, pcode: pcode.trim(), adm2Gid: selection.adm2Gid });
      } else {
        throw new Error("Select the parent region before creating this level.");
      }
      setShowForm(false);
      toast.success(editing ? "Region updated." : "Region created.");
      await load();
    } catch (error) {
      toast.error(message(error));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (item: RegionItem) => {
    if (!window.confirm(`Delete ${item.nameEn || item.nameAr || item.pcode}?`)) return;
    try {
      if (level === "adm0") await regionsApi.deleteCountry(item.gid);
      else if (level === "adm1") await regionsApi.deleteGovernorate(item.gid);
      else if (level === "adm2") await regionsApi.deleteDistrict(item.gid);
      else await regionsApi.deleteNeighbourhood(item.gid);
      toast.success("Region deleted.");
      await load();
    } catch (error) {
      toast.error(message(error));
    }
  };

  const importFile = async (file?: File) => {
    if (!file) return;
    setImporting(true);
    try {
      const result = await regionsApi.importGeoJson(level, file);
      toast.success(`${result.layer}: ${result.inserted} inserted, ${result.updated} updated.`);
      await load();
    } catch (error) {
      toast.error(message(error));
    } finally {
      setImporting(false);
    }
  };

  const importLocal = async () => {
    setImporting(true);
    try {
      const results = await regionsApi.importAllLocal();
      toast.success(`Imported ${results.length} local region layers.`);
      await load();
    } catch (error) {
      toast.error(message(error));
    } finally {
      setImporting(false);
    }
  };

  return (
    <div className="space-y-5">
      <div className="card-glass space-y-4 p-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="flex items-center gap-2 font-semibold"><MapPinned className="h-4 w-4 text-accent" /> Region hierarchy</h2>
            <p className="text-xs text-muted-foreground">Manage records or import validated GeoJSON in ADM order.</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <label className="cursor-pointer rounded-lg border border-border px-3 py-2 text-xs">
              <FileUp className="mr-1 inline h-3.5 w-3.5" /> {importing ? "Importing…" : `Import ${level.toUpperCase()}`}
              <input
                type="file"
                accept=".geojson,application/geo+json,application/json"
                className="hidden"
                disabled={importing}
                onChange={(event) => void importFile(event.target.files?.[0])}
              />
            </label>
            <button disabled={importing} onClick={() => void importLocal()} className="rounded-lg border border-border px-3 py-2 text-xs disabled:opacity-50">
              Import bundled files
            </button>
            <button onClick={openCreate} className="btn-accent rounded-lg px-3 py-2 text-xs"><Plus className="mr-1 inline h-3.5 w-3.5" /> Add region</button>
          </div>
        </div>

        <select
          value={level}
          onChange={(event) => {
            setLevel(event.target.value as RegionLevel);
            setShowForm(false);
          }}
          className="rounded-lg border border-border bg-secondary px-3 py-2 text-sm"
        >
          {(Object.keys(levelLabels) as RegionLevel[]).map((value) => (
            <option key={value} value={value}>{levelLabels[value]}</option>
          ))}
        </select>

        {level !== "adm0" && (
          <RegionCascadeSelect
            value={selection}
            onChange={setSelection}
            label="Parent filter"
            includeNeighbourhood={false}
          />
        )}
      </div>

      {showForm && (
        <div className="card-glass grid gap-3 p-5 sm:grid-cols-2">
          <h3 className="font-semibold sm:col-span-2">{editing ? "Edit" : "Create"} {level.toUpperCase()}</h3>
          <label className="text-xs">English name<input value={nameEn} onChange={(event) => setNameEn(event.target.value)} className="input-glass mt-1 w-full" /></label>
          <label className="text-xs">Arabic name<input value={nameAr} onChange={(event) => setNameAr(event.target.value)} className="input-glass mt-1 w-full" /></label>
          <label className="text-xs">Pcode<input disabled={!!editing} value={pcode} onChange={(event) => setPcode(event.target.value)} className="input-glass mt-1 w-full disabled:opacity-50" /></label>
          <label className="text-xs">Image URL<input value={imageUrl} onChange={(event) => setImageUrl(event.target.value)} className="input-glass mt-1 w-full" /></label>
          {level === "adm0" && <label className="text-xs">Flag URL<input value={flagUrl} onChange={(event) => setFlagUrl(event.target.value)} className="input-glass mt-1 w-full" /></label>}
          <div className="flex gap-2 sm:col-span-2">
            <button disabled={saving} onClick={() => void save()} className="btn-accent rounded-lg px-4 py-2 text-xs disabled:opacity-50">{saving ? "Saving…" : "Save"}</button>
            <button onClick={() => setShowForm(false)} className="rounded-lg border border-border px-4 py-2 text-xs">Cancel</button>
          </div>
        </div>
      )}

      <div className="card-glass overflow-hidden">
        {loading ? (
          <p className="p-8 text-center text-sm text-muted-foreground">Loading regions…</p>
        ) : items.length === 0 ? (
          <p className="p-8 text-center text-sm text-muted-foreground">No regions found for this level and parent.</p>
        ) : (
          <div className="max-h-[32rem] divide-y divide-border overflow-y-auto">
            {items.map((item) => (
              <div key={item.gid} className="flex items-center justify-between gap-3 p-4">
                <div>
                  <p className="text-sm font-medium">{item.nameEn || item.nameAr || "Unnamed region"}</p>
                  <p className="text-xs text-muted-foreground">GID {item.gid} · {item.pcode}{item.nameAr ? ` · ${item.nameAr}` : ""}</p>
                </div>
                <div className="flex gap-1">
                  <button onClick={() => openEdit(item)} className="rounded p-2 hover:bg-secondary" title="Edit"><Pencil className="h-4 w-4" /></button>
                  <button onClick={() => void remove(item)} className="rounded p-2 text-destructive hover:bg-secondary" title="Delete"><Trash2 className="h-4 w-4" /></button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default RegionsAdminPanel;
