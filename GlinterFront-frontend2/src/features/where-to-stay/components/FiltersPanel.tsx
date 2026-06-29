import { Shield, DollarSign, Armchair, Thermometer } from "lucide-react";
import type { HeatmapLayer } from "../types";

interface FiltersPanelProps {
  maxPrice: number;
  onMaxPriceChange: (value: number) => void;
  minSafety: number;
  onMinSafetyChange: (value: number) => void;
  minComfort: number;
  onMinComfortChange: (value: number) => void;
  minRating: number;
  onMinRatingChange: (value: number) => void;
  backgroundLayer: HeatmapLayer | "none";
  onBackgroundLayerChange: (layer: HeatmapLayer | "none") => void;
}

const FiltersPanel = ({
  maxPrice,
  onMaxPriceChange,
  minSafety,
  onMinSafetyChange,
  minComfort,
  onMinComfortChange,
  minRating,
  onMinRatingChange,
  backgroundLayer,
  onBackgroundLayerChange,
}: FiltersPanelProps) => {
  return (
    <div className="liquid-glass rounded-3xl p-4 flex flex-col gap-3">
      <div>
        <h2 className="text-xl font-bold glow-text">Where to Stay</h2>
        <p className="text-[10px] text-gray-400">Strict filters — only matching dots shown</p>
      </div>

      <div className="space-y-2">
        {/* Price slider */}
        <div className="w-full">
          <div className="w-full flex items-center gap-2.5 bg-brand-dark/50 backdrop-blur-sm border border-brand-glassBorder p-2.5 rounded-xl">
            <div className="w-7 h-7 rounded-lg bg-blue-500/20 text-blue-400 flex items-center justify-center shrink-0">
              <DollarSign className="w-3.5 h-3.5" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex justify-between items-center mb-0.5">
                <span className="font-medium text-xs">Max Price</span>
                <span className="text-[10px] text-brand-gold font-medium">${maxPrice}/night</span>
              </div>
              <input
                type="range"
                min={20}
                max={200}
                value={maxPrice}
                onChange={(e) => onMaxPriceChange(parseInt(e.target.value))}
                className="w-full"
              />
            </div>
          </div>
        </div>

        {/* Safety slider */}
        <div className="w-full">
          <div className="w-full flex items-center gap-2.5 bg-brand-dark/50 backdrop-blur-sm border border-brand-glassBorder p-2.5 rounded-xl">
            <div className="w-7 h-7 rounded-lg bg-brand-purple/20 text-brand-purple flex items-center justify-center shrink-0">
              <Shield className="w-3.5 h-3.5" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex justify-between items-center mb-0.5">
                <span className="font-medium text-xs">Min Safety</span>
                <span className="text-[10px] text-brand-gold font-medium">{minSafety > 0 ? `${minSafety}%` : "Any"}</span>
              </div>
              <input
                type="range"
                min={0}
                max={100}
                step={5}
                value={minSafety}
                onChange={(e) => onMinSafetyChange(parseInt(e.target.value))}
                className="w-full"
              />
            </div>
          </div>
        </div>

        {/* Comfort slider */}
        <div className="w-full">
          <div className="w-full flex items-center gap-2.5 bg-brand-dark/50 backdrop-blur-sm border border-brand-glassBorder p-2.5 rounded-xl">
            <div className="w-7 h-7 rounded-lg bg-brand-teal/20 text-brand-teal flex items-center justify-center shrink-0">
              <Armchair className="w-3.5 h-3.5" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex justify-between items-center mb-0.5">
                <span className="font-medium text-xs">Min Comfort</span>
                <span className="text-[10px] text-brand-gold font-medium">{minComfort > 0 ? `${minComfort}%` : "Any"}</span>
              </div>
              <input
                type="range"
                min={0}
                max={100}
                step={5}
                value={minComfort}
                onChange={(e) => onMinComfortChange(parseInt(e.target.value))}
                className="w-full"
              />
            </div>
          </div>
        </div>
      </div>

      <hr className="border-white/5" />

      {/* Background heatmap selector */}
      <div>
        <p className="text-[10px] text-gray-400 mb-1.5 flex items-center gap-1">
          <Thermometer className="w-3 h-3" /> Background heatmap
        </p>
        <div className="flex gap-1.5">
          {(["safety", "comfort", "none"] as const).map((layer) => (
            <button
              key={layer}
              onClick={() => onBackgroundLayerChange(layer)}
              className={`flex-1 py-1 rounded-full text-[10px] font-semibold transition capitalize ${
                backgroundLayer === layer
                  ? "bg-brand-purple text-white"
                  : "glass-input hover:bg-brand-glass"
              }`}
            >
              {layer}
            </button>
          ))}
        </div>
      </div>

      <hr className="border-white/5" />

      {/* Min rating */}
      <div>
        <p className="text-[10px] text-gray-400 mb-1.5">Min rating</p>
        <div className="flex gap-1.5">
          {[0, 4.0, 4.5].map((r) => (
            <button
              key={r}
              onClick={() => onMinRatingChange(r)}
              className={`flex-1 py-1 rounded-full text-[10px] font-semibold transition ${
                minRating === r
                  ? "bg-brand-gold text-brand-dark"
                  : "glass-input hover:bg-brand-glass"
              }`}
            >
              {r === 0 ? "Any" : `${r}+`}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};

export default FiltersPanel;
