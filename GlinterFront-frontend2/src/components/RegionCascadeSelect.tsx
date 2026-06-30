import { useEffect, useState } from "react";
import { MapPin } from "lucide-react";
import { regionsApi } from "@/shared/services/api-regions";
import type {
  CountryDto,
  DistrictDto,
  GovernorateDto,
  NeighbourhoodDto,
  RegionHierarchyGids,
} from "@/shared/types/regions";

interface RegionCascadeSelectProps {
  value: RegionHierarchyGids;
  onChange: (value: RegionHierarchyGids) => void;
  label?: string;
  includeNeighbourhood?: boolean;
  disabled?: boolean;
}

const selectClass =
  "w-full rounded-lg border border-border bg-secondary px-3 py-2 text-xs text-foreground disabled:opacity-50";

const readId = (value: string) => value ? Number(value) : undefined;

const RegionCascadeSelect = ({
  value,
  onChange,
  label = "Region",
  includeNeighbourhood = true,
  disabled = false,
}: RegionCascadeSelectProps) => {
  const [countries, setCountries] = useState<CountryDto[]>([]);
  const [governorates, setGovernorates] = useState<GovernorateDto[]>([]);
  const [districts, setDistricts] = useState<DistrictDto[]>([]);
  const [neighbourhoods, setNeighbourhoods] = useState<NeighbourhoodDto[]>([]);
  const [loadingLevel, setLoadingLevel] = useState<string>();
  const [error, setError] = useState("");

  useEffect(() => {
    setLoadingLevel("countries");
    regionsApi.getCountries()
      .then(setCountries)
      .catch((requestError: unknown) => {
        setError(requestError instanceof Error ? requestError.message : "Could not load countries.");
      })
      .finally(() => setLoadingLevel(undefined));
  }, []);

  useEffect(() => {
    if (!value.adm0Gid) {
      setGovernorates([]);
      return;
    }
    setLoadingLevel("governorates");
    setError("");
    regionsApi.getGovernorates(value.adm0Gid)
      .then(setGovernorates)
      .catch((requestError: unknown) => {
        setError(requestError instanceof Error ? requestError.message : "Could not load governorates.");
      })
      .finally(() => setLoadingLevel(undefined));
  }, [value.adm0Gid]);

  useEffect(() => {
    if (!value.adm1Gid) {
      setDistricts([]);
      return;
    }
    setLoadingLevel("districts");
    setError("");
    regionsApi.getDistricts(value.adm1Gid)
      .then(setDistricts)
      .catch((requestError: unknown) => {
        setError(requestError instanceof Error ? requestError.message : "Could not load districts.");
      })
      .finally(() => setLoadingLevel(undefined));
  }, [value.adm1Gid]);

  useEffect(() => {
    if (!value.adm2Gid || !includeNeighbourhood) {
      setNeighbourhoods([]);
      return;
    }
    setLoadingLevel("neighbourhoods");
    setError("");
    regionsApi.getNeighbourhoods(value.adm2Gid)
      .then(setNeighbourhoods)
      .catch((requestError: unknown) => {
        setError(requestError instanceof Error ? requestError.message : "Could not load neighbourhoods.");
      })
      .finally(() => setLoadingLevel(undefined));
  }, [includeNeighbourhood, value.adm2Gid]);

  return (
    <fieldset className="space-y-2" disabled={disabled}>
      <legend className="mb-1 flex items-center gap-1 text-xs font-medium">
        <MapPin className="h-3.5 w-3.5" /> {label}
      </legend>
      <div className={`grid gap-2 ${includeNeighbourhood ? "sm:grid-cols-2 xl:grid-cols-4" : "sm:grid-cols-3"}`}>
        <select
          aria-label="Country"
          className={selectClass}
          value={value.adm0Gid ?? ""}
          onChange={(event) => onChange({ adm0Gid: readId(event.target.value) })}
        >
          <option value="">{loadingLevel === "countries" ? "Loading countries…" : "All countries"}</option>
          {countries.map((item) => <option key={item.gid} value={item.gid}>{item.nameEn}</option>)}
        </select>
        <select
          aria-label="Governorate"
          className={selectClass}
          disabled={disabled || !value.adm0Gid}
          value={value.adm1Gid ?? ""}
          onChange={(event) => onChange({
            adm0Gid: value.adm0Gid,
            adm1Gid: readId(event.target.value),
          })}
        >
          <option value="">{loadingLevel === "governorates" ? "Loading governorates…" : "All governorates"}</option>
          {governorates.map((item) => <option key={item.gid} value={item.gid}>{item.nameEn}</option>)}
        </select>
        <select
          aria-label="District"
          className={selectClass}
          disabled={disabled || !value.adm1Gid}
          value={value.adm2Gid ?? ""}
          onChange={(event) => onChange({
            adm0Gid: value.adm0Gid,
            adm1Gid: value.adm1Gid,
            adm2Gid: readId(event.target.value),
          })}
        >
          <option value="">{loadingLevel === "districts" ? "Loading districts…" : "All districts"}</option>
          {districts.map((item) => <option key={item.gid} value={item.gid}>{item.nameEn}</option>)}
        </select>
        {includeNeighbourhood && (
          <select
            aria-label="Neighbourhood"
            className={selectClass}
            disabled={disabled || !value.adm2Gid}
            value={value.adm3Gid ?? ""}
            onChange={(event) => onChange({
              adm0Gid: value.adm0Gid,
              adm1Gid: value.adm1Gid,
              adm2Gid: value.adm2Gid,
              adm3Gid: readId(event.target.value),
            })}
          >
            <option value="">{loadingLevel === "neighbourhoods" ? "Loading neighbourhoods…" : "All neighbourhoods"}</option>
            {neighbourhoods.map((item) => (
              <option key={item.gid} value={item.gid}>{item.nameEn || item.nameAr || item.pcode}</option>
            ))}
          </select>
        )}
      </div>
      {error && <p role="alert" className="text-xs text-destructive">{error}</p>}
    </fieldset>
  );
};

export default RegionCascadeSelect;
