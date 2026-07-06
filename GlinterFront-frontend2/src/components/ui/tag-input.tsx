import { useState } from "react";
import { X } from "lucide-react";
import { formControlClass } from "./form-control";
import { normalizeTags } from "./tag-utils";

interface TagInputProps {
  value: string[];
  onChange: (value: string[]) => void;
  placeholder?: string;
  maxItems?: number;
  maxItemLength?: number;
  disabled?: boolean;
  "aria-label"?: string;
}

export const TagInput = ({
  value,
  onChange,
  placeholder,
  maxItems = 10,
  maxItemLength = 40,
  disabled,
  "aria-label": ariaLabel,
}: TagInputProps) => {
  const [draft, setDraft] = useState("");

  const commit = (pending = draft) => {
    const next = normalizeTags(
      [...value, ...pending.split(/[,;\n]/)],
      maxItems,
      maxItemLength,
    );
    onChange(next);
    setDraft("");
  };

  return (
    <div className="mt-1 space-y-2">
      {value.length > 0 && (
        <div className="flex flex-wrap gap-2" aria-label="Selected values">
          {value.map((item) => (
            <span
              key={item.toLocaleLowerCase()}
              className="inline-flex items-center gap-1 rounded-full border border-primary/25 bg-primary/10 px-3 py-1 text-xs text-foreground"
            >
              {item}
              <button
                type="button"
                aria-label={`Remove ${item}`}
                disabled={disabled}
                onClick={() => onChange(value.filter(
                  (current) => current.toLocaleLowerCase() !== item.toLocaleLowerCase(),
                ))}
                className="rounded-full p-0.5 text-muted-foreground hover:bg-destructive/15 hover:text-destructive focus:outline-none focus:ring-2 focus:ring-primary/40"
              >
                <X className="h-3 w-3" />
              </button>
            </span>
          ))}
        </div>
      )}
      <input
        aria-label={ariaLabel}
        value={draft}
        disabled={disabled || value.length >= maxItems}
        maxLength={maxItemLength}
        placeholder={value.length >= maxItems ? `Maximum ${maxItems} items` : placeholder}
        className={formControlClass}
        onChange={(event) => {
          const next = event.target.value;
          if (next.endsWith(",")) {
            commit(next.slice(0, -1));
          } else {
            setDraft(next);
          }
        }}
        onBlur={() => {
          if (draft.trim()) commit();
        }}
        onKeyDown={(event) => {
          if ((event.key === "Enter" || event.key === ",") && draft.trim()) {
            event.preventDefault();
            commit();
          } else if (event.key === "Backspace" && !draft && value.length > 0) {
            onChange(value.slice(0, -1));
          }
        }}
      />
      <p className="text-xs text-muted-foreground">
        Press Enter or comma to add. {value.length}/{maxItems}
      </p>
    </div>
  );
};
