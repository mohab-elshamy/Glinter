import { DollarSign } from "lucide-react";

interface FiltersPanelProps {
  maxPrice?: number;
  onMaxPriceChange: (value?: number) => void;
  minRating: number;
  onMinRatingChange: (value: number) => void;
}

const FiltersPanel = ({
  maxPrice,
  onMaxPriceChange,
  minRating,
  onMinRatingChange,
}: FiltersPanelProps) => (
  <div className="liquid-glass flex flex-col gap-4 rounded-3xl p-4">
    <div>
      <h2 className="text-xl font-bold glow-text">Where to Stay</h2>
      <p className="text-[10px] text-gray-400">Filters use live backend fields</p>
    </div>
    <div className="rounded-xl border border-brand-glassBorder bg-brand-dark/50 p-2.5">
      <div className="mb-1 flex items-center justify-between">
        <span className="flex items-center gap-2 text-xs font-medium"><DollarSign className="h-3.5 w-3.5" /> Max price</span>
        <span className="text-[10px] font-medium text-brand-gold">
          {maxPrice == null ? "Any price" : `$${maxPrice}/night`}
        </span>
      </div>
      <input
        type="range"
        min={0}
        max={5000}
        step={50}
        value={maxPrice ?? 5000}
        onChange={(event) => {
          const value = Number(event.target.value);
          onMaxPriceChange(value >= 5000 ? undefined : value);
        }}
        className="w-full"
      />
    </div>
    <div>
      <p className="mb-1.5 text-[10px] text-gray-400">Minimum rating</p>
      <div className="flex gap-1.5">
        {[0, 4, 4.5].map((rating) => (
          <button
            key={rating}
            onClick={() => onMinRatingChange(rating)}
            className={`flex-1 rounded-full py-1 text-[10px] font-semibold ${minRating === rating ? "bg-brand-gold text-brand-dark" : "glass-input"}`}
          >
            {rating === 0 ? "Any" : `${rating}+`}
          </button>
        ))}
      </div>
    </div>
  </div>
);

export default FiltersPanel;
