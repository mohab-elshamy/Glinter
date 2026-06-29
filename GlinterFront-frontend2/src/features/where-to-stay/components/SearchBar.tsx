import { useRef } from "react";
import { Search, CalendarDays, Users, Minus, Plus } from "lucide-react";

interface SearchBarProps {
  searchQuery: string;
  onSearchQueryChange: (value: string) => void;
  checkin: string;
  onCheckinChange: (value: string) => void;
  checkout: string;
  onCheckoutChange: (value: string) => void;
  guests: number;
  onGuestsChange: (value: number) => void;
  onSearch: () => void;
}

const formatDate = (dateStr: string) => {
  if (!dateStr) return "";
  const d = new Date(dateStr + "T00:00:00");
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
};

const SearchBar = ({
  searchQuery,
  onSearchQueryChange,
  checkin,
  onCheckinChange,
  checkout,
  onCheckoutChange,
  guests,
  onGuestsChange,
  onSearch,
}: SearchBarProps) => {
  const checkinRef = useRef<HTMLInputElement>(null);
  const checkoutRef = useRef<HTMLInputElement>(null);

  const openPicker = (ref: React.RefObject<HTMLInputElement | null>) => {
    if (ref.current) {
      ref.current.showPicker();
    }
  };

  return (
    <div className="liquid-glass rounded-xl px-3 py-2 flex flex-row items-center gap-2 max-md:flex-wrap">
      <div className="flex-[2] max-md:flex-1 max-md:min-w-[200px] relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-gray-400" />
        <input
          placeholder="Search destination..."
          value={searchQuery}
          onChange={(e) => onSearchQueryChange(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && onSearch()}
          className="w-full h-9 pl-9 pr-2.5 rounded-lg glass-input placeholder-gray-400 focus:ring-0 text-xs"
        />
      </div>

      <div className="relative shrink-0 cursor-pointer" onClick={() => openPicker(checkinRef)}>
        <CalendarDays className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-gray-400 pointer-events-none" />
        <input
          ref={checkinRef}
          type="date"
          value={checkin}
          onChange={(e) => onCheckinChange(e.target.value)}
          className="absolute inset-0 opacity-0"
        />
        <div className="w-28 h-9 pl-9 pr-2.5 rounded-lg glass-input flex items-center text-xs">
          {checkin ? (
            <span className="text-white">{formatDate(checkin)}</span>
          ) : (
            <span className="text-gray-400">Check-in</span>
          )}
        </div>
      </div>

      <div className="relative shrink-0 cursor-pointer" onClick={() => openPicker(checkoutRef)}>
        <CalendarDays className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-gray-400 pointer-events-none" />
        <input
          ref={checkoutRef}
          type="date"
          value={checkout}
          onChange={(e) => onCheckoutChange(e.target.value)}
          className="absolute inset-0 opacity-0"
        />
        <div className="w-28 h-9 pl-9 pr-2.5 rounded-lg glass-input flex items-center text-xs">
          {checkout ? (
            <span className="text-white">{formatDate(checkout)}</span>
          ) : (
            <span className="text-gray-400">Check-out</span>
          )}
        </div>
      </div>

      <div className="flex items-center gap-1.5 glass-input rounded-lg px-2 h-9 shrink-0">
        <Users className="w-3.5 h-3.5 text-gray-400 shrink-0" />
        <button
          onClick={() => onGuestsChange(Math.max(1, guests - 1))}
          className="w-4 h-4 flex items-center justify-center rounded hover:bg-white/10 text-gray-400 hover:text-white transition-colors shrink-0"
        >
          <Minus className="w-2.5 h-2.5" />
        </button>
        <span className="text-xs text-white text-center tabular-nums min-w-[20px]">
          {guests}
        </span>
        <button
          onClick={() => onGuestsChange(guests + 1)}
          className="w-4 h-4 flex items-center justify-center rounded hover:bg-white/10 text-gray-400 hover:text-white transition-colors shrink-0"
        >
          <Plus className="w-2.5 h-2.5" />
        </button>
      </div>

      <button
        onClick={onSearch}
        className="h-9 px-4 rounded-lg bg-gradient-to-r from-brand-gold to-yellow-600 text-brand-dark font-semibold hover:opacity-90 transition text-xs shrink-0"
      >
        Search
      </button>
    </div>
  );
};

export default SearchBar;
